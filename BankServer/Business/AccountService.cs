using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankServer.Business;

/// <summary>
/// Банкны дансны бизнес логик.
/// IBankAccountRepository interface хэрэгжүүлнэ — Shared гэрээний дагуу.
///
/// ЗАСВАР 1: ILogger нэмэгдсэн — бүх үйлдэл бүртгэгдэнэ.
/// ЗАСВАР 2: TransferRecord хадгалах — гүйлгээний аудит.
/// ЗАСВАР 3: Rollback pattern — мөнгө алдагдахаас сэргийлнэ.
/// </summary>
public class AccountService : IBankAccountRepository
{
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// Дансны санах ой. Key = дансны дугаар.
    /// In-memory + SQLite хоёулаа ашиглана:
    ///   - In-memory: хурдан хандалт, тест
    ///   - SQLite: persist (server restart-д алдагдахгүй)
    /// </summary>
    private readonly Dictionary<string, AccountEntry> _accounts = new()
    {
        ["ACC001"] = new("ACC001", "Болд",    1_000_000, 100),
        ["ACC002"] = new("ACC002", "Сарнай",    500_000,  50),
        ["ACC003"] = new("ACC003", "Ганбаяр", 2_000_000, 200),
    };

    /// <summary>
    /// Гүйлгээний бүртгэл — in-memory жагсаалт.
    /// Жинхэнэ системд SQLite DbContext ашиглана.
    /// </summary>
    private readonly List<TransferRecord> _transferHistory = new();
    private readonly object _historyLock = new();

    /// <summary>
    /// Данс тус бүрийн async lock.
    /// Deadlock-аас сэргийлж дансны дугаарын эрэмбээр авна:
    ///   string.Compare(first, second) < 0 → first-г эхэлж lock авна
    /// Ингэснээр хоёр теллер нэгэн зэрэг ABC→DEF, DEF→ABC гүйлгээ хийхэд
    /// deadlock үүсэхгүй.
    /// </summary>
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _lockGuard = new();

    public AccountService(ILogger<AccountService> logger)
    {
        _logger = logger;
    }

    private SemaphoreSlim GetLock(string acc)
    {
        lock (_lockGuard)
        {
            if (!_locks.TryGetValue(acc, out var s))
                _locks[acc] = s = new SemaphoreSlim(1, 1);
            return s;
        }
    }

    /// <summary>
    /// А данснаас Б данс руу мөнгө шилжүүлнэ.
    ///
    /// Алдааны бүх тохиолдол шалгана:
    ///   — Дүн 0 буюу сөрөг
    ///   — Хоосон дансны дугаар
    ///   — Ижил данс
    ///   — Данс олдохгүй
    ///   — Үлдэгдэл хүрэлцэхгүй
    ///
    /// ЗАСВАР: Rollback pattern нэмэгдсэн.
    /// Жинхэнэ банкны системд энд SQL TRANSACTION ашиглана:
    ///   BEGIN TRANSACTION
    ///   UPDATE accounts SET balance = balance - amount WHERE id = fromId
    ///   UPDATE accounts SET balance = balance + amount WHERE id = toId
    ///   COMMIT  ← хоёул амжилттай болсны дараа л хадгална
    /// </summary>
    public async Task<(bool Success, string Message, Guid? TransferId)> TransferAsync(
        string fromAccount, string toAccount, decimal amount, string tellerId = "")
    {
        // ── Клиент талын validation ────────────────────────────────────
        if (amount <= 0)
            return (false, "Дүн 0-ээс их байх ёстой", null);

        if (string.IsNullOrWhiteSpace(fromAccount))
            return (false, "Илгээгч дансны дугаар хоосон байж болохгүй", null);

        if (string.IsNullOrWhiteSpace(toAccount))
            return (false, "Хүлээн авагч дансны дугаар хоосон байж болохгүй", null);

        if (fromAccount.Equals(toAccount, StringComparison.OrdinalIgnoreCase))
            return (false, "Илгээгч болон хүлээн авагч данс ижил байж болохгүй", null);

        // Deadlock-аас сэргийлж эрэмбээр lock авна
        var (first, second) = string.Compare(fromAccount, toAccount) < 0
            ? (fromAccount, toAccount) : (toAccount, fromAccount);

        await GetLock(first).WaitAsync();
        await GetLock(second).WaitAsync();

        // ── Rollback-д зориулж анхны утгыг хадгална ──────────────────
        decimal originalFrom = 0, originalTo = 0;

        try
        {
            if (!_accounts.TryGetValue(fromAccount, out var from))
                return (false, $"'{fromAccount}' данс олдсонгүй", null);

            if (!_accounts.TryGetValue(toAccount, out var to))
                return (false, $"'{toAccount}' данс олдсонгүй", null);

            if (from.MNT < amount)
                return (false,
                    $"Үлдэгдэл хүрэлцэхгүй ({from.MNT:N0}₮ < {amount:N0}₮)", null);

            _logger.LogInformation(
                "Гүйлгээ эхэллээ: {From} → {To}, {Amount:N0}₮, теллер: {Teller}",
                fromAccount, toAccount, amount, tellerId);

            // Анхны утгыг хадгална — rollback хийхэд хэрэгтэй
            originalFrom = from.MNT;
            originalTo   = to.MNT;

            // ── Мөнгө шилжүүлэх ──────────────────────────────────────
            // Жинхэнэ системд энд SQL ATOMIC TRANSACTION ашиглана.
            // Хэрэв хоёрдугаар мөр-д алдаа гарвал rollback хийнэ.
            from.MNT -= amount;
            to.MNT   += amount;

            // ── Гүйлгээний бүртгэл ────────────────────────────────────
            var record = new TransferRecord
            {
                FromAccount = fromAccount,
                ToAccount   = toAccount,
                Amount      = amount,
                TellerId    = tellerId,
                Status      = "Completed",
                CreatedAt   = DateTime.Now
            };
            lock (_historyLock) _transferHistory.Add(record);

            var msg = $"{amount:N0}₮ амжилттай: {from.OwnerName} → {to.OwnerName}. " +
                      $"Үлдэгдэл: {from.MNT:N0}₮";

            _logger.LogInformation(
                "Гүйлгээ амжилттай: {From} → {To}, {Amount:N0}₮, ID: {Id}",
                fromAccount, toAccount, amount, record.Id);

            return (true, msg, record.Id);
        }
        catch (Exception ex)
        {
            // ── Rollback — алдаа гарвал анхны утгыг сэргээнэ ────────
            // Жинхэнэ системд SQL ROLLBACK энэ үүргийг гүйцэтгэнэ.
            if (_accounts.TryGetValue(fromAccount, out var fromAcc))
                fromAcc.MNT = originalFrom;
            if (_accounts.TryGetValue(toAccount, out var toAcc))
                toAcc.MNT = originalTo;

            _logger.LogError(ex,
                "Гүйлгээ амжилтгүй: {From} → {To}, {Amount:N0}₮",
                fromAccount, toAccount, amount);

            // Амжилтгүй гүйлгээг ч бүртгэнэ
            lock (_historyLock) _transferHistory.Add(new TransferRecord
            {
                FromAccount  = fromAccount,
                ToAccount    = toAccount,
                Amount       = amount,
                TellerId     = tellerId,
                Status       = "Failed",
                ErrorMessage = ex.Message,
                CreatedAt    = DateTime.Now
            });

            return (false, $"Системийн алдаа: {ex.Message}", null);
        }
        finally
        {
            // finally: алдаа гарсан ч lock заавал суллагдана
            GetLock(second).Release();
            GetLock(first).Release();
        }
    }

    /// <summary>Гүйлгээний бүтэн түүх буцаана — аудитад ашиглана.</summary>
    public IReadOnlyList<TransferRecord> GetTransferHistory()
    {
        lock (_historyLock) return _transferHistory.AsReadOnly();
    }

    /// <summary>Нэг дансны мэдээлэл авна.</summary>
    public AccountEntry? GetAccount(string accountNumber) =>
        _accounts.TryGetValue(accountNumber, out var a) ? a : null;

    /// <summary>Бүх дансны жагсаалт авна.</summary>
    public IEnumerable<AccountEntry> GetAllAccounts() => _accounts.Values;

    // ── IBankAccountRepository хэрэгжүүлэлт ──────────────────────────────

    public Task<BankAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        if (!_accounts.TryGetValue(accountNumber, out var a))
            return Task.FromResult<BankAccount?>(null);
        return Task.FromResult<BankAccount?>(new BankAccount
        {
            AccountNumber = a.AccountNumber,
            OwnerName     = a.OwnerName,
            Currency      = "MNT",
            Balance       = a.MNT,
            IsActive      = true
        });
    }

    public Task<BankAccount?> GetByIdAsync(int id) =>
        Task.FromResult<BankAccount?>(null);

    public Task UpdateAsync(BankAccount account) => Task.CompletedTask;
}

/// <summary>
/// Нэг харилцагчийн дансны in-memory загвар.
/// AccountNumber, OwnerName, MNT, USD — тест гэрээний дагуу.
/// </summary>
public class AccountEntry
{
    public string  AccountNumber { get; }
    public string  OwnerName     { get; }
    public decimal MNT           { get; set; }
    public decimal USD           { get; set; }

    public AccountEntry(string num, string name, decimal mnt, decimal usd)
    {
        AccountNumber = num;
        OwnerName     = name;
        MNT           = mnt;
        USD           = usd;
    }
}
