using BankServer.Domain.Models;

namespace BankServer.Business;

/// <summary>
/// А данснаас Б данс руу мөнгө шилжүүлэх логик.
/// Данс тус бүрт тусдаа lock — race condition-оос хамгаална.
/// </summary>
public class AccountService
{
    /// <summary>
    /// Дансны мэдээллийн санах ой. Key = дансны дугаар.
    /// Жинхэнэ проектод SQL Server + Entity Framework орлоно.
    /// </summary>
    private readonly Dictionary<string, BankAccount> _accounts = new()
    {
        ["ACC001"] = new BankAccount { AccountNumber = "ACC001", OwnerName = "Болд", MNT = 1_000_000, USD = 100 },
        ["ACC002"] = new BankAccount { AccountNumber = "ACC002", OwnerName = "Сарнай", MNT = 500_000, USD = 50 },
        ["ACC003"] = new BankAccount { AccountNumber = "ACC003", OwnerName = "Ганбаяр", MNT = 2_000_000, USD = 200 },
    };

    /// <summary>Данс тус бүрийн lock. Нэг дансны гүйлгээ нөгөөг саатуулахгүй.</summary>
    private readonly Dictionary<string, SemaphoreSlim> _accountLocks = new();
    private readonly object _lockGuard = new();

    private SemaphoreSlim GetOrCreateLock(string accountNumber)
    {
        lock (_lockGuard)
        {
            if (!_accountLocks.TryGetValue(accountNumber, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                _accountLocks[accountNumber] = sem;
            }
            return sem;
        }
    }

    /// <summary>
    /// Мөнгө шилжүүлнэ. Deadlock-оос сэргийлж lock-уудыг
    /// дансны дугаарын string эрэмбээр авна.
    /// </summary>
    public async Task<(bool Success, string Message)> TransferAsync(
    string fromAccount, string toAccount, decimal amount)
    {
        // ЭНД НЭМНЭ
        if (amount <= 0)
            return (false, "Дүн 0-ээс их байх ёстой");

        var (first, second) = string.Compare(fromAccount, toAccount) < 0
            ? (fromAccount, toAccount)
            : (toAccount, fromAccount);

        var lock1 = GetOrCreateLock(first);
        var lock2 = GetOrCreateLock(second);

        await lock1.WaitAsync();
        await lock2.WaitAsync();
        try
        {
            if (!_accounts.TryGetValue(fromAccount, out var from))
                return (false, $"'{fromAccount}' данс олдсонгүй");

            if (!_accounts.TryGetValue(toAccount, out var to))
                return (false, $"'{toAccount}' данс олдсонгүй");

            if (from.MNT < amount)
                return (false, $"Үлдэгдэл хүрэлцэхгүй ({from.MNT:N0}₮ < {amount:N0}₮)");

            from.MNT -= amount;
            to.MNT += amount;

            return (true, $"{amount:N0}₮ амжилттай: {from.OwnerName} → {to.OwnerName}");
        }
        finally
        {
            lock2.Release();
            lock1.Release();
        }
    }

    /// <summary>Нэг дансны мэдээлэл. Байхгүй бол null.</summary>
    public BankAccount? GetAccount(string accountNumber) =>
        _accounts.TryGetValue(accountNumber, out var acc) ? acc : null;

    /// <summary>Бүх дансны жагсаалт.</summary>
    public IEnumerable<BankAccount> GetAllAccounts() => _accounts.Values;
}