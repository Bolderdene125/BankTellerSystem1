using BankServer.Services;
using Xunit;

namespace BankTests;

/// <summary>
/// TicketQueueService-ийн дугаар олгох, дуудах,
/// thread-safety-г шалгах тестүүд.
/// </summary>
public class TicketQueueServiceTests
{
    /// <summary>Шинэ service тест бүрт тусдаа үүсгэнэ — тестүүд бие биенд нөлөөлөхгүй.</summary>
    private TicketQueueService NewService() => new();

    /// <summary>Дугаар 1-ээс эхэлж байгааг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_FirstTicket_NumberIsOne()
    {
        var svc = NewService();
        var ticket = await svc.IssueTicketAsync("Гүйлгээ");
        Assert.Equal(1, ticket.Number);
    }

    /// <summary>Дараалан авахад дугаар нэмэгдэж байгааг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_Sequential_NumbersIncrement()
    {
        var svc = NewService();
        var t1 = await svc.IssueTicketAsync("Гүйлгээ");
        var t2 = await svc.IssueTicketAsync("Лавлагаа");
        var t3 = await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(1, t1.Number);
        Assert.Equal(2, t2.Number);
        Assert.Equal(3, t3.Number);
    }

    /// <summary>
    /// 50 хүсэлт нэгэн зэрэг ирэхэд дугаар давтагдаагүйг шалгана.
    /// Thread-safety-г баталгаажуулах гол тест.
    /// </summary>
    [Fact]
    public async Task IssueTicket_Concurrent_NoDuplicates()
    {
        var svc = NewService();
        var tasks = Enumerable.Range(0, 50).Select(_ => svc.IssueTicketAsync("Гүйлгээ"));
        var tickets = await Task.WhenAll(tasks);
        var unique = tickets.Select(t => t.Number).Distinct().Count();

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

    /// <summary>Дугаар дуудсаны дараа дарааллын тоо буурч байгааг шалгана.</summary>
    [Fact]
    public async Task QueueCount_DecreasesAfterCallNext()
    {
        var svc = NewService();
        await svc.IssueTicketAsync("Гүйлгээ");
        await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(2, svc.GetQueueCount());
        await svc.CallNextAsync();
        Assert.Equal(1, svc.GetQueueCount());
    }
}