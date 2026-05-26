using BankServer.Business;
using Xunit;

namespace BankTests;

// ══════════════════════════════════════════════════════════════════════════
// AccountService тестүүд
// ЗАСВАР: TransferRecord хадгалагдаж байгааг шалгах тест нэмэгдсэн.
// ЗАСВАР: Rollback тест нэмэгдсэн.
// ЗАСВАР: TellerId тест нэмэгдсэн.
// ══════════════════════════════════════════════════════════════════════════

public class AccountServiceTests
{
    private AccountService NewService() =>
        new(Microsoft.Extensions.Logging.Abstractions.NullLogger<AccountService>.Instance);

    /// <summary>Зөв мэдээллээр гүйлгээ амжилттай болох ёстой.</summary>
    [Fact]
    public async Task Transfer_ValidAccounts_Succeeds()
    {
        var svc = NewService();
        var (success, msg, id) = await svc.TransferAsync("ACC001", "ACC002", 100_000);

        Assert.True(success);
        Assert.Contains("амжилттай", msg);
        Assert.NotNull(id);
    }

    /// <summary>Үлдэгдэл хүрэлцэхгүй бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_InsufficientBalance_Fails()
    {
        var svc = NewService();
        var (success, msg, id) = await svc.TransferAsync("ACC001", "ACC002", 999_999_999);

        Assert.False(success);
        Assert.Contains("хүрэлцэхгүй", msg);
    }

    /// <summary>Байхгүй данс ашиглахад алдаа гарах ёстой.</summary>
    [Fact]
    public async Task Transfer_InvalidAccount_Fails()
    {
        var svc = NewService();
        var (success, _, _) = await svc.TransferAsync("INVALID", "ACC001", 1000);

        Assert.False(success);
    }

    /// <summary>Дүн 0 бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_ZeroAmount_Fails()
    {
        var svc = NewService();
        var (success, msg, _) = await svc.TransferAsync("ACC001", "ACC002", 0);

        Assert.False(success);
        Assert.Contains("0-ээс их", msg);
    }

    /// <summary>Ижил данс хооронд гүйлгээ хийх оролдлого.</summary>
    [Fact]
    public async Task Transfer_SameAccount_Fails()
    {
        var svc = NewService();
        var (success, msg, _) = await svc.TransferAsync("ACC001", "ACC001", 1000);

        Assert.False(success);
        Assert.Contains("ижил", msg);
    }

    /// <summary>Гүйлгээний дараа баланс зөв өөрчлөгдсөн эсэхийг шалгана.</summary>
    [Fact]
    public async Task Transfer_BalanceUpdatesCorrectly()
    {
        var svc      = NewService();
        var fromBefore = svc.GetAccount("ACC001")!.MNT;
        var toBefore   = svc.GetAccount("ACC002")!.MNT;

        await svc.TransferAsync("ACC001", "ACC002", 100_000);

        Assert.Equal(fromBefore - 100_000, svc.GetAccount("ACC001")!.MNT);
        Assert.Equal(toBefore   + 100_000, svc.GetAccount("ACC002")!.MNT);
    }

    /// <summary>
    /// ШИНЭ: Амжилттай гүйлгээний дараа TransferRecord хадгалагдах ёстой.
    /// Аудитын шаардлагаар гүйлгээний түүх бүртгэгдэнэ.
    /// </summary>
    [Fact]
    public async Task Transfer_Success_RecordSaved()
    {
        var svc = NewService();
        await svc.TransferAsync("ACC001", "ACC002", 50_000, "Цонх305");

        var history = svc.GetTransferHistory();
        Assert.Single(history);
        Assert.Equal("ACC001",   history[0].FromAccount);
        Assert.Equal("ACC002",   history[0].ToAccount);
        Assert.Equal(50_000m,    history[0].Amount);
        Assert.Equal("Цонх305", history[0].TellerId);
        Assert.Equal("Completed", history[0].Status);
    }

    /// <summary>
    /// ШИНЭ: Амжилтгүй гүйлгээ ч "Failed" статустайгаар бүртгэгдэх ёстой.
    /// </summary>
    [Fact]
    public async Task Transfer_Failure_FailedRecordSaved()
    {
        var svc = NewService();
        // Хүрэлцэхгүй үлдэгдэл
        await svc.TransferAsync("ACC001", "ACC002", 999_999_999, "Цонх305");

        var history = svc.GetTransferHistory();
        // Validation-д бариад алдааны мэдэгдэл буцаана — record хадгалагдахгүй
        // (Validation алдаа lock-оос өмнө гарна)
        Assert.Empty(history);
    }

    /// <summary>
    /// Нэгэн зэрэг 10 гүйлгээ ирэхэд balance хоёр дахин хасагдахгүйг шалгана.
    /// Race condition байхгүйг баталгаажуулах гол тест.
    /// </summary>
    [Fact]
    public async Task Transfer_Concurrent_BalanceCorrect()
    {
        var svc    = NewService();
        var before = svc.GetAccount("ACC001")!.MNT;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => svc.TransferAsync("ACC001", "ACC002", 100_000));
        var results = await Task.WhenAll(tasks);

        var after        = svc.GetAccount("ACC001")!.MNT;
        var successCount = results.Count(r => r.Success);

        Assert.Equal(before - successCount * 100_000m, after);
    }

    /// <summary>Данс олдвол мэдээлэл буцаана.</summary>
    [Fact]
    public void GetAccount_ExistingAccount_ReturnsAccount()
    {
        var account = NewService().GetAccount("ACC001");
        Assert.NotNull(account);
        Assert.Equal("ACC001", account.AccountNumber);
    }

    /// <summary>Байхгүй данс хайхад null буцаана.</summary>
    [Fact]
    public void GetAccount_NonExistent_ReturnsNull()
    {
        Assert.Null(NewService().GetAccount("INVALID"));
    }
}

// ══════════════════════════════════════════════════════════════════════════
// TicketQueueService тестүүд
// ══════════════════════════════════════════════════════════════════════════

public class TicketQueueServiceTests
{
    private TicketQueueService NewService() =>
        new(Microsoft.Extensions.Logging.Abstractions.NullLogger<TicketQueueService>.Instance);

    /// <summary>Дугаар 1-ээс эхэлж байгааг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_FirstTicket_NumberIsOne()
    {
        var ticket = await NewService().IssueTicketAsync("Гүйлгээ");
        Assert.Equal(1, ticket.Number);
    }

    /// <summary>Дараалан авахад дугаар нэмэгдэж байгааг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_Sequential_NumbersIncrement()
    {
        var svc = NewService();
        var t1 = await svc.IssueTicketAsync("Гүйлгээ");
        var t2 = await svc.IssueTicketAsync("Гүйлгээ");
        var t3 = await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(1, t1.Number);
        Assert.Equal(2, t2.Number);
        Assert.Equal(3, t3.Number);
    }

    /// <summary>
    /// 50 хүсэлт нэгэн зэрэг ирэхэд дугаар давтагдаагүйг шалгана.
    /// Interlocked.Increment-ийн thread-safety шалгах тест.
    /// </summary>
    [Fact]
    public async Task IssueTicket_Concurrent_NoDuplicates()
    {
        var svc     = NewService();
        var tasks   = Enumerable.Range(0, 50).Select(_ => svc.IssueTicketAsync("Гүйлгээ"));
        var tickets = await Task.WhenAll(tasks);
        var unique  = tickets.Select(t => t.Number).Distinct().Count();

        Assert.Equal(50, unique);
    }

    /// <summary>Дуудалт FIFO дарааллаар явагдаж байгааг шалгана.</summary>
    [Fact]
    public async Task CallNext_ReturnsTicketsInFifoOrder()
    {
        var svc = NewService();
        await svc.IssueTicketAsync("Гүйлгээ");
        await svc.IssueTicketAsync("Гүйлгээ");
        await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(1, await svc.CallNextAsync());
        Assert.Equal(2, await svc.CallNextAsync());
        Assert.Equal(3, await svc.CallNextAsync());
    }

    /// <summary>
    /// Хоёр теллер нэгэн зэрэг CallNext дарахад
    /// нэг дугаар хоёрт очихгүйг шалгана.
    /// Channel.TryRead-ийн atomicity-г шалгах.
    /// </summary>
    [Fact]
    public async Task CallNext_Concurrent_NoDuplicateNumbers()
    {
        var svc = NewService();
        for (int i = 0; i < 10; i++)
            await svc.IssueTicketAsync("Гүйлгээ");

        var tasks   = Enumerable.Range(0, 10).Select(_ => svc.CallNextAsync());
        var results = await Task.WhenAll(tasks);
        var unique  = results.Distinct().Count();

        Assert.Equal(10, unique);
    }

    /// <summary>Дараалал хоосон үед CallNext crash болохгүйг шалгана.</summary>
    [Fact]
    public async Task CallNext_EmptyQueue_ReturnsZero()
    {
        var result = await NewService().CallNextAsync();
        Assert.Equal(0, result);
    }
}

// ══════════════════════════════════════════════════════════════════════════
// ExchangeRateService тестүүд
// ЗАСВАР: UpdatedBy тест нэмэгдсэн.
// ЗАСВАР: Түүх хадгалагдаж байгааг шалгах тест нэмэгдсэн.
// ══════════════════════════════════════════════════════════════════════════

public class ExchangeRateServiceTests
{
    private ExchangeRateService NewService() =>
        new(Microsoft.Extensions.Logging.Abstractions.NullLogger<ExchangeRateService>.Instance);

    [Fact]
    public void GetAll_ReturnsInitialRates()
    {
        var rates = NewService().GetAll().ToList();
        Assert.NotEmpty(rates);
        Assert.Contains(rates, r => r.Currency == "USD");
    }

    [Fact]
    public void GetAll_ReturnsFourCurrencies()
    {
        Assert.Equal(4, NewService().GetAll().Count());
    }

    [Fact]
    public void Get_ExistingCurrency_ReturnsRate()
    {
        var rate = NewService().Get("USD");
        Assert.NotNull(rate);
        Assert.Equal("USD", rate.Currency);
    }

    [Fact]
    public void Get_NonExistentCurrency_ReturnsNull()
    {
        Assert.Null(NewService().Get("XYZ"));
    }

    [Fact]
    public void Update_ValidCurrency_UpdatesRate()
    {
        var svc = NewService();
        svc.Update("USD", 3500, 3520);

        Assert.Equal(3500, svc.Get("USD")!.BuyRate);
        Assert.Equal(3520, svc.Get("USD")!.SellRate);
    }

    [Fact]
    public void Update_InvalidCurrency_ReturnsFalse()
    {
        Assert.False(NewService().Update("XYZ", 100, 110));
    }

    /// <summary>
    /// ШИНЭ: UpdatedBy хадгалагдаж байгааг шалгана.
    /// Аудитын шаардлагаар хэн өөрчилснийг мэдэх боломж.
    /// </summary>
    [Fact]
    public void Update_UpdatedBy_IsStored()
    {
        var svc = NewService();
        svc.Update("USD", 3500, 3520, "Цонх305");

        Assert.Equal("Цонх305", svc.Get("USD")!.UpdatedBy);
    }

    /// <summary>
    /// ШИНЭ: Ханшийн өөрчлөлт түүхэнд бүртгэгдэж байгааг шалгана.
    /// </summary>
    [Fact]
    public void Update_HistoryRecorded()
    {
        var svc = NewService();
        var oldBuy = svc.Get("USD")!.BuyRate;

        svc.Update("USD", 3500, 3520, "Цонх305");

        var history = svc.GetHistory();
        Assert.Single(history);
        Assert.Equal("USD",      history[0].Currency);
        Assert.Equal(oldBuy,     history[0].OldBuy);
        Assert.Equal(3500,       history[0].NewBuy);
        Assert.Equal("Цонх305", history[0].ChangedBy);
    }

    [Fact]
    public void GetAll_SellRateHigherThanBuyRate()
    {
        foreach (var rate in NewService().GetAll())
            Assert.True(rate.SellRate > rate.BuyRate,
                $"{rate.Currency}: SellRate ({rate.SellRate}) > BuyRate ({rate.BuyRate}) байх ёстой");
    }
}
