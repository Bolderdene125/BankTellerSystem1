using BankServer.Business;
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
        var t2 = await svc.IssueTicketAsync("Гүйлгээ");
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
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => svc.IssueTicketAsync("Гүйлгээ"));
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

    /// <summary>
    /// Хоёр теллер нэгэн зэрэг CallNext дарахад
    /// нэг дугаар хоёрт очихгүйг шалгана.
    /// Багшийн шаардлага: "нэг дугаарыг олон дэлгэцэнд харуулахаас сэргийлсэн"
    /// </summary>
    [Fact]
    public async Task CallNext_Concurrent_NoDuplicateNumbers()
    {
        var svc = NewService();

        for (int i = 0; i < 10; i++)
            await svc.IssueTicketAsync("Гүйлгээ");

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => svc.CallNextAsync());
        var results = await Task.WhenAll(tasks);

        var unique = results.Distinct().Count();
        Assert.Equal(10, unique);
    }

    /// <summary>
    /// Дараалал хоосон үед CallNext дуудахад
    /// 0 буцаана — crash болохгүй.
    /// </summary>
    [Fact]
    public async Task CallNext_EmptyQueue_ReturnsCurrentNumber()
    {
        var svc = NewService();
        var result = await svc.CallNextAsync();

        Assert.Equal(0, result);
    }

    /// <summary>ServiceType зөв хадгалагддаг эсэхийг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_ServiceType_IsStored()
    {
        var svc = NewService();
        var ticket = await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal("Гүйлгээ", ticket.ServiceType);
    }

    /// <summary>IssuedAt цаг зөв хадгалагддаг эсэхийг шалгана.</summary>
    [Fact]
    public async Task IssueTicket_IssuedAt_IsRecent()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var svc = NewService();
        var ticket = await svc.IssueTicketAsync("Гүйлгээ");
        var after = DateTime.Now.AddSeconds(1);

        Assert.InRange(ticket.IssuedAt, before, after);
    }
}