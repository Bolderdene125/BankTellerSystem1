using BankServer.Data;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankServer.Business;

/// <summary>
/// Банкны дансны бизнес логик.
/// IBankAccountRepository interface хэрэгжүүлнэ.
///
/// ӨӨРЧЛӨЛТ: In-memory Dictionary-аас SQLite + EF Core DB transaction руу шилжсэн.
///
/// Яагаад DB transaction:
///   - Server дахин эхлэхэд үлдэгдэл алдагдахгүй болно
///   - from.Balance -= amount, to.Balance += amount хоёул нэг
///     атомик үйлдлээр хадгалагдана — хооронд crash болсон ч
///     мөнгө алдагдахгүй, DB автоматаар rollback хийнэ
///
/// Яагаад Scoped (Singleton биш):
///   - BankDbContext нь Scoped lifetime-тай
///   - Singleton сервис дотор Scoped inject хийхийг ASP.NET Core
///     runtime-д хориглодог ("captive dependency" алдаа)
///   - Тиймээс AccountService ч Scoped болно
/// </summary>
public class AccountService : IBankAccountRepository
{
    private readonly BankDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(BankDbContext db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// А данснаас Б данс руу мөнгө шилжүүлнэ.
    ///
    /// SQL TRANSACTION ашигласан — жинхэнэ банкны системтэй адил:
    ///   BEGIN TRANSACTION
    ///   UPDATE Accounts SET Balance -= amount WHERE AccountNumber = from
    ///   UPDATE Accounts SET Balance += amount WHERE AccountNumber = to
    ///   INSERT INTO TransferRecords ...
    ///   COMMIT  ← гурвал амжилттай болсны дараа л хадгална
    ///   ROLLBACK ← алдаа гарвал гурвал цуцлагдана
    ///
    /// Deadlock-аас сэргийлэх:
    ///   EF Core + SQLite row-level lock ашигладаг тул
    ///   SemaphoreSlim lock шаардлагагүй
    ///   SQLite serializable transaction isolation ашигладаг.
    /// </summary>
    public async Task<(bool Success, string Message, Guid? TransferId)> TransferAsync(
        string fromAccount, string toAccount, decimal amount, string tellerId = "")
    {
        // ── Validation — DB хандахаас өмнө шалгана ────────────────────
        if (amount <= 0)
            return (false, "Дүн 0-ээс их байх ёстой", null);

        if (string.IsNullOrWhiteSpace(fromAccount))
            return (false, "Илгээгч дансны дугаар хоосон байж болохгүй", null);

        if (string.IsNullOrWhiteSpace(toAccount))
            return (false, "Хүлээн авагч дансны дугаар хоосон байж болохгүй", null);

        if (fromAccount.Equals(toAccount, StringComparison.OrdinalIgnoreCase))
            return (false, "Илгээгч болон хүлээн авагч данс ижил байж болохгүй", null);

        // ── DB Transaction эхлүүлнэ ───────────────────────────────────
        // using var: блокоос гарахад transaction автоматаар dispose хийгдэнэ
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Дансуудыг DB-аас уншина
            var from = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == fromAccount);
            if (from is null)
                return (false, $"'{fromAccount}' данс олдсонгүй", null);

            var to = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == toAccount);
            if (to is null)
                return (false, $"'{toAccount}' данс олдсонгүй", null);

            if (from.Balance < amount)
                return (false,
                    $"Үлдэгдэл хүрэлцэхгүй ({from.Balance:N0}₮ < {amount:N0}₮)", null);

            _logger.LogInformation(
                "Гүйлгээ эхэллээ: {From} → {To}, {Amount:N0}₮, теллер: {Teller}",
                fromAccount, toAccount, amount, tellerId);

            // ── Үлдэгдэл өөрчлөх ─────────────────────────────────────
            // EF Core change tracking: from, to объект өөрчлөгдсөнийг
            // SaveChangesAsync() дуудахад UPDATE SQL автоматаар явуулна
            from.Balance -= amount;
            to.Balance += amount;

            // ── Гүйлгээний бүртгэл нэмэх ─────────────────────────────
            // TransferRecord нь гүйлгээтэй нэг transaction дотор хадгалагдана
            // Тиймээс гүйлгээ амжилттай болсон бүрийн бүртгэл байна
            var record = new TransferRecord
            {
                FromAccount = fromAccount,
                ToAccount = toAccount,
                Amount = amount,
                TellerId = tellerId,
                Status = "Completed",
                CreatedAt = DateTime.Now
            };
            _db.TransferRecords.Add(record);

            // ── Хоёулыг нэг дор хадгална ─────────────────────────────
            // SaveChangesAsync: from.Balance, to.Balance, TransferRecord
            // гурвал нэг SQL batch болгон явуулна
            await _db.SaveChangesAsync();

            // ── Transaction баталгаажуулна ─────────────────────────────
            // CommitAsync дуудсаны дараа л disk-д бичигдэнэ
            // Commit хүрэхээс өмнө аливаа алдаа гарвал catch-д RollbackAsync дуудна
            await tx.CommitAsync();

            var msg = $"{amount:N0}₮ амжилттай: {from.OwnerName} → {to.OwnerName}. " +
                      $"Үлдэгдэл: {from.Balance:N0}₮";

            _logger.LogInformation(
                "Гүйлгээ амжилттай: {From} → {To}, {Amount:N0}₮, ID: {Id}",
                fromAccount, toAccount, amount, record.Id);

            return (true, msg, record.Id);
        }
        catch (Exception ex)
        {
            // ── Rollback — алдаа гарвал DB-г анхны байдалд нь буцаана ──
            // SQLite transaction rollback: from.Balance, to.Balance хоёул
            // өөрчлөгдөхөөс өмнөх утгаа сэргээнэ — мөнгө алдагдахгүй
            await tx.RollbackAsync();

            _logger.LogError(ex,
                "Гүйлгээ амжилтгүй — rollback: {From} → {To}, {Amount:N0}₮",
                fromAccount, toAccount, amount);

            // Амжилтгүй гүйлгээг тусдаа transaction-аар бүртгэнэ
            // (rollback болсон тул шинэ transaction хэрэгтэй)
            try
            {
                _db.TransferRecords.Add(new TransferRecord
                {
                    FromAccount = fromAccount,
                    ToAccount = toAccount,
                    Amount = amount,
                    TellerId = tellerId,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Failed бүртгэл хийхэд алдаа гарвал орхино — критик биш
            }

            return (false, $"Системийн алдаа: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Гүйлгээний түүх DB-аас авна.
    /// Хамгийн сүүлийн 100 гүйлгээг буцаана.
    /// </summary>
    public async Task<List<TransferRecord>> GetTransferHistoryAsync()
    {
        return await _db.TransferRecords
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <summary>Sync хэлбэр — Controller дуудахад хэрэглэнэ.</summary>
    public List<TransferRecord> GetTransferHistory()
    {
        return _db.TransferRecords
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToList();
    }

    /// <summary>Нэг дансны мэдээлэл DB-аас авна.</summary>
    public BankAccount? GetAccount(string accountNumber)
    {
        return _db.Accounts
            .FirstOrDefault(a => a.AccountNumber == accountNumber);
    }

    /// <summary>Бүх идэвхтэй дансны жагсаалт.</summary>
    public IEnumerable<BankAccount> GetAllAccounts()
    {
        return _db.Accounts.Where(a => a.IsActive).ToList();
    }

    // ── IBankAccountRepository хэрэгжүүлэлт ──────────────────────────────

    public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<BankAccount?> GetByIdAsync(int id)
    {
        return await _db.Accounts.FindAsync(id);
    }

    public async Task UpdateAsync(BankAccount account)
    {
        _db.Accounts.Update(account);
        await _db.SaveChangesAsync();
    }
}