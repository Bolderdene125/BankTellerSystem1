using BankServer.Data;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankServer.Business;

/// <summary>
/// IBankAccountRepository interface-г хэрэгжүүлнэ.
/// Гүйлгээний бизнес логик болон данс хайх үйлдлүүд энд байна.
/// </summary>
public class AccountService(BankDbContext db) : IBankAccountRepository
{
    /// <summary>
    /// async/await дотор lock{} ажиллахгүй тул SemaphoreSlim ашиглана.
    /// Нэгэн зэрэг хоёр гүйлгээ ирэхэд баланс давхар хасагдахаас сэргийлнэ.
    /// </summary>
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// А данснаас Б данс руу мөнгө шилжүүлнэ.
    /// Клиент болон сервер хоёр талд validation хийдэг — давхар шалгалт.
    /// </summary>
    public async Task<(bool Success, string Message)> TransferAsync(
        string fromAccount, string toAccount, decimal amount)
    {
        if (amount <= 0)
            return (false, "Дүн 0-ээс их байх ёстой");

        if (string.IsNullOrWhiteSpace(fromAccount))
            return (false, "Илгээгч дансны дугаар хоосон байж болохгүй");

        if (string.IsNullOrWhiteSpace(toAccount))
            return (false, "Хүлээн авагч дансны дугаар хоосон байж болохгүй");

        if (fromAccount.Equals(toAccount, StringComparison.OrdinalIgnoreCase))
            return (false, "Илгээгч болон хүлээн авагч данс ижил байж болохгүй");

        await _lock.WaitAsync();
        try
        {
            var from = await db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == fromAccount && a.IsActive);
            if (from is null)
                return (false, $"'{fromAccount}' илгээгч данс олдсонгүй");

            var to = await db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == toAccount && a.IsActive);
            if (to is null)
                return (false, $"'{toAccount}' хүлээн авагч данс олдсонгүй");

            if (from.Currency != to.Currency)
                return (false, $"Валют тохирохгүй: {from.Currency} → {to.Currency}");

            if (from.Balance < amount)
                return (false,
                    $"Үлдэгдэл хүрэлцэхгүй ({from.Balance:N0} {from.Currency} < {amount:N0})");

            from.Balance -= amount;
            to.Balance += amount;

            await db.SaveChangesAsync();

            return (true,
                $"{amount:N0} {from.Currency} амжилттай: " +
                $"{from.OwnerName} → {to.OwnerName}. Үлдэгдэл: {from.Balance:N0}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Дансны дугаараар данс хайна — тестэд ашиглагдана.
    /// AccountNumber-ын эхний хэсгээр (ACC001) хайх боломжтой.
    /// </summary>
    public async Task<AccountView?> GetAccountAsync(string accountNumber)
    {
        // ACC001 хэлбэрээр хайхад ACC001-MNT, ACC001-USD хоёуланг нэгтгэж буцаана
        var accounts = await db.Accounts
            .Where(a => a.AccountNumber.StartsWith(accountNumber) && a.IsActive)
            .ToListAsync();

        if (!accounts.Any()) return null;

        return new AccountView
        {
            AccountNumber = accountNumber,
            OwnerName = accounts.First().OwnerName,
            MNT = accounts.FirstOrDefault(a => a.Currency == "MNT")?.Balance ?? 0,
            USD = accounts.FirstOrDefault(a => a.Currency == "USD")?.Balance ?? 0
        };
    }

    /// <summary>Идэвхтэй дансны жагсаалт — AccountController дуудна.</summary>
    public async Task<IEnumerable<BankAccount>> GetAllAccountsAsync() =>
        await db.Accounts.Where(a => a.IsActive).ToListAsync();

    // ── IBankAccountRepository хэрэгжүүлэлт ─────────────────────────────

    /// <summary>Дансны дугаараар хайна — IBankAccountRepository гэрээ.</summary>
    public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber) =>
        await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

    /// <summary>ID-аар хайна — IBankAccountRepository гэрээ.</summary>
    public async Task<BankAccount?> GetByIdAsync(int id) =>
        await db.Accounts.FindAsync(id);

    /// <summary>Данс шинэчилнэ — IBankAccountRepository гэрээ.</summary>
    public async Task UpdateAsync(BankAccount account)
    {
        db.Accounts.Update(account);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Нэг харилцагчийн бүх дансны нэгтгэл — тестэд ашиглагдана.
/// ACC001-MNT болон ACC001-USD-г нэг объектод нэгтгэнэ.
/// </summary>
public class AccountView
{
    /// <summary>Дансны үндсэн дугаар (жишээ: ACC001)</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Эзэмшигчийн нэр</summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>МНТ үлдэгдэл</summary>
    public decimal MNT { get; set; }

    /// <summary>USD үлдэгдэл</summary>
    public decimal USD { get; set; }
}