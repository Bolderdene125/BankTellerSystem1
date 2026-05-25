using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;

namespace BankServer.Business;

/// <summary>
/// Банкны дансны бизнес логик.
/// IBankAccountRepository interface хэрэгжүүлнэ — Shared гэрээний дагуу.
/// In-memory хадгалалт: тест болон demo-д тохиромжтой.
/// </summary>
public class AccountService : IBankAccountRepository
{
    /// <summary>
    /// Дансны санах ой. Key = дансны дугаар.
    /// Нэг дансны загвар: AccountNumber, OwnerName, MNT, USD.
    /// </summary>
    private readonly Dictionary<string, AccountEntry> _accounts = new()
    {
        ["ACC001"] = new("ACC001", "Болд", 1_000_000, 100),
        ["ACC002"] = new("ACC002", "Сарнай", 500_000, 50),
        ["ACC003"] = new("ACC003", "Ганбаяр", 2_000_000, 200),
    };

    /// <summary>
    /// Данс тус бүрийн async lock.
    /// Deadlock-аас сэргийлж дансны дугаарын эрэмбээр авна.
    /// </summary>
    private readonly Dictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _lockGuard = new();

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
    /// Бүх алдааны тохиолдлыг шалгана:
    ///   — Дүн 0 буюу сөрөг
    ///   — Хоосон дансны дугаар
    ///   — Ижил данс
    ///   — Данс олдохгүй
    ///   — Үлдэгдэл хүрэлцэхгүй
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

        // Deadlock-аас сэргийлж эрэмбээр lock авна
        var (first, second) = string.Compare(fromAccount, toAccount) < 0
            ? (fromAccount, toAccount) : (toAccount, fromAccount);

        await GetLock(first).WaitAsync();
        await GetLock(second).WaitAsync();
        try
        {
            if (!_accounts.TryGetValue(fromAccount, out var from))
                return (false, $"'{fromAccount}' данс олдсонгүй");

            if (!_accounts.TryGetValue(toAccount, out var to))
                return (false, $"'{toAccount}' данс олдсонгүй");

            if (from.MNT < amount)
                return (false,
                    $"Үлдэгдэл хүрэлцэхгүй ({from.MNT:N0}₮ < {amount:N0}₮)");

            from.MNT -= amount;
            to.MNT += amount;

            return (true,
                $"{amount:N0}₮ амжилттай: {from.OwnerName} → {to.OwnerName}. " +
                $"Үлдэгдэл: {from.MNT:N0}₮");
        }
        finally
        {
            GetLock(second).Release();
            GetLock(first).Release();
        }
    }

    /// <summary>
    /// Нэг дансны мэдээлэл — тест sync хэлбэрээр дуудна.
    /// AccountEntry: AccountNumber, OwnerName, MNT, USD талбартай.
    /// </summary>
    public AccountEntry? GetAccount(string accountNumber) =>
        _accounts.TryGetValue(accountNumber, out var a) ? a : null;

    /// <summary>Бүх дансны жагсаалт — тест sync хэлбэрээр дуудна.</summary>
    public IEnumerable<AccountEntry> GetAllAccounts() => _accounts.Values;

    // ── IBankAccountRepository хэрэгжүүлэлт (Shared гэрээ) ──────────────

    /// <summary>Дансны дугаараар Shared.BankAccount буцаана — interface гэрээ.</summary>
    public Task<BankAccount?> GetByAccountNumberAsync(string accountNumber)
    {
        if (!_accounts.TryGetValue(accountNumber, out var a))
            return Task.FromResult<BankAccount?>(null);
        return Task.FromResult<BankAccount?>(new BankAccount
        {
            AccountNumber = a.AccountNumber,
            OwnerName = a.OwnerName,
            Currency = "MNT",
            Balance = a.MNT,
            IsActive = true
        });
    }

    /// <summary>ID-аар хайна — IBankAccountRepository гэрээ.</summary>
    public Task<BankAccount?> GetByIdAsync(int id) =>
        Task.FromResult<BankAccount?>(null);

    /// <summary>Данс шинэчилнэ — IBankAccountRepository гэрээ.</summary>
    public Task UpdateAsync(BankAccount account) => Task.CompletedTask;
}

/// <summary>
/// Нэг харилцагчийн дансны загвар.
/// AccountNumber, OwnerName, MNT, USD — тест гэрээний дагуу.
/// </summary>
public class AccountEntry
{
    public string AccountNumber { get; }
    public string OwnerName { get; }
    public decimal MNT { get; set; }
    public decimal USD { get; set; }

    public AccountEntry(string num, string name, decimal mnt, decimal usd)
    {
        AccountNumber = num;
        OwnerName = name;
        MNT = mnt;
        USD = usd;
    }
}