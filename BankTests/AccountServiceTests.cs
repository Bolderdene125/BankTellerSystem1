using BankServer.Business;
using BankServer.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankTests;

/// <summary>
/// AccountService-ийн тестүүд.
/// InMemory database ашигладаг тул SQLite суулгах шаардлагагүй.
/// </summary>
public class AccountServiceTests
{
    /// <summary>
    /// Тест бүрт тусдаа InMemory database үүсгэнэ.
    /// Тестүүд бие биенд нөлөөлөхгүй.
    /// </summary>
    private static AccountService NewService()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BankDbContext(options);
        db.Database.EnsureCreated(); // Seed өгөгдөл нэмнэ
        return new AccountService(db);
    }

    /// <summary>Зөв мэдээллээр гүйлгээ амжилттай болох ёстой.</summary>
    [Fact]
    public async Task Transfer_ValidAccounts_Succeeds()
    {
        var svc = NewService();
        var (success, msg) = await svc.TransferAsync("ACC001", "ACC002", 100_000);

        Assert.True(success);
        Assert.Contains("амжилттай", msg);
    }

    /// <summary>Үлдэгдэл хүрэлцэхгүй бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_InsufficientBalance_Fails()
    {
        var svc = NewService();
        var (success, msg) = await svc.TransferAsync("ACC001", "ACC002", 999_999_999);

        Assert.False(success);
        Assert.Contains("хүрэлцэхгүй", msg);
    }

    /// <summary>Байхгүй данс ашиглахад алдаа гарах ёстой.</summary>
    [Fact]
    public async Task Transfer_InvalidAccount_Fails()
    {
        var svc = NewService();
        var (success, msg) = await svc.TransferAsync("INVALID", "ACC001", 1000);

        Assert.False(success);
        Assert.Contains("олдсонгүй", msg);
    }

    /// <summary>Дүн 0 бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_ZeroAmount_Fails()
    {
        var svc = NewService();
        var (success, msg) = await svc.TransferAsync("ACC001", "ACC002", 0);

        Assert.False(success);
        Assert.Contains("0-ээс их", msg);
    }

    /// <summary>Гүйлгээний дараа баланс зөв өөрчлөгдсөн эсэхийг шалгана.</summary>
    [Fact]
    public async Task Transfer_BalanceUpdatesCorrectly()
    {
        var svc = NewService();
        var fromBefore = (await svc.GetAccountAsync("ACC001"))!.MNT;
        var toBefore = (await svc.GetAccountAsync("ACC002"))!.MNT;

        await svc.TransferAsync("ACC001", "ACC002", 100_000);

        Assert.Equal(fromBefore - 100_000, (await svc.GetAccountAsync("ACC001"))!.MNT);
        Assert.Equal(toBefore + 100_000, (await svc.GetAccountAsync("ACC002"))!.MNT);
    }

    /// <summary>
    /// Нэгэн зэрэг 10 гүйлгээ ирэхэд balance давхар хасагдахгүйг шалгана.
    /// </summary>
    [Fact]
    public async Task Transfer_Concurrent_BalanceCorrect()
    {
        var svc = NewService();
        var before = (await svc.GetAccountAsync("ACC001"))!.MNT;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => svc.TransferAsync("ACC001", "ACC002", 100_000));
        var results = await Task.WhenAll(tasks);

        var after = (await svc.GetAccountAsync("ACC001"))!.MNT;
        var successCount = results.Count(r => r.Success);

        Assert.Equal(before - successCount * 100_000m, after);
    }

    /// <summary>Данс олдвол мэдээлэл буцаана.</summary>
    [Fact]
    public async Task GetAccount_ExistingAccount_ReturnsAccount()
    {
        var svc = NewService();
        var account = await svc.GetAccountAsync("ACC001");

        Assert.NotNull(account);
        Assert.Equal("ACC001", account.AccountNumber);
    }

    /// <summary>Байхгүй данс хайхад null буцаана.</summary>
    [Fact]
    public async Task GetAccount_NonExistent_ReturnsNull()
    {
        var svc = NewService();
        Assert.Null(await svc.GetAccountAsync("INVALID"));
    }

    /// <summary>Бүх дансны жагсаалт хоосон биш байх ёстой.</summary>
    [Fact]
    public async Task GetAllAccounts_ReturnsAccounts()
    {
        var svc = NewService();
        var accounts = (await svc.GetAllAccountsAsync()).ToList();

        Assert.NotEmpty(accounts);
        Assert.Equal(3, accounts.Count);
    }
}