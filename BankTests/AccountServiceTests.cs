using BankServer.Business;
using Xunit;

namespace BankTests;

/// <summary>
/// AccountService-ийн гүйлгээний логик,
/// алдааны тохиолдол, concurrent гүйлгээний тестүүд.
/// </summary>
public class AccountServiceTests
{
    private AccountService NewService() => new();

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
        var fromBefore = svc.GetAccount("ACC001")!.MNT;
        var toBefore = svc.GetAccount("ACC002")!.MNT;

        await svc.TransferAsync("ACC001", "ACC002", 100_000);

        Assert.Equal(fromBefore - 100_000, svc.GetAccount("ACC001")!.MNT);
        Assert.Equal(toBefore + 100_000, svc.GetAccount("ACC002")!.MNT);
    }

    /// <summary>
    /// Нэгэн зэрэг 10 гүйлгээ ирэхэд balance хоёр дахин хасагдахгүйг шалгана.
    /// Race condition байхгүйг баталгаажуулах гол тест.
    /// </summary>
    [Fact]
    public async Task Transfer_Concurrent_BalanceCorrect()
    {
        var svc = NewService();
        var before = svc.GetAccount("ACC001")!.MNT;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => svc.TransferAsync("ACC001", "ACC002", 100_000));
        var results = await Task.WhenAll(tasks);

        var after = svc.GetAccount("ACC001")!.MNT;
        var successCount = results.Count(r => r.Success);

        Assert.Equal(before - successCount * 100_000m, after);
    }

    /// <summary>Данс олдвол мэдээлэл буцаана.</summary>
    [Fact]
    public void GetAccount_ExistingAccount_ReturnsAccount()
    {
        var svc = NewService();
        var account = svc.GetAccount("ACC001");

        Assert.NotNull(account);
        Assert.Equal("ACC001", account.AccountNumber);
    }

    /// <summary>Байхгүй данс хайхад null буцаана.</summary>
    [Fact]
    public void GetAccount_NonExistent_ReturnsNull()
    {
        var svc = NewService();
        Assert.Null(svc.GetAccount("INVALID"));
    }

    /// <summary>Бүх дансны жагсаалт хоосон биш байх ёстой.</summary>
    [Fact]
    public void GetAllAccounts_ReturnsAccounts()
    {
        var svc = NewService();
        var accounts = svc.GetAllAccounts().ToList();

        Assert.NotEmpty(accounts);
        Assert.Equal(3, accounts.Count);
    }
}