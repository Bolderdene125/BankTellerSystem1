using BankServer.Hubs;
using BankServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BankServer.Controllers;

/// <summary>Дугаар олгох, дуудах, дарааллын байдал харах endpoint-ууд.</summary>
[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly TicketQueueService _queueService;
    private readonly IHubContext<BankHub> _hub;

    // DI container эдгээр хоёрыг constructor-д автоматаар өгнө
    public TicketController(TicketQueueService queueService, IHubContext<BankHub> hub)
    {
        _queueService = queueService;
        _hub = hub;
    }

    /// <summary>
    /// Шинэ тасалбар олгоно. Дугаар авах терминалаас дуудна.
    /// POST /api/ticket/issue?serviceType=Гүйлгээ
    /// </summary>
    [HttpPost("issue")]
    public async Task<IActionResult> IssueTicket([FromQuery] string serviceType = "Гүйлгээ")
    {
        var ticket = await _queueService.IssueTicketAsync(serviceType);
        return Ok(new
        {
            number = ticket.Number,
            issuedAt = ticket.IssuedAt,
            serviceType = ticket.ServiceType,
            queueCount = _queueService.GetQueueCount()
        });
    }

    /// <summary>
    /// Дарааллаас дараагийн дугаарыг авч SignalR-ээр дэлгэцүүдэд явуулна.
    /// POST /api/ticket/call-next
    /// </summary>
    [HttpPost("call-next")]
    public async Task<IActionResult> CallNext()
    {
        int number = await _queueService.CallNextAsync();

        // "ReceiveNumberUpdate" — дугаарын дэлгэцүүд энэ event-г сонсоно
        await _hub.Clients.All.SendAsync("ReceiveNumberUpdate", number);

        return Ok(new { calledNumber = number });
    }

    /// <summary>
    /// Одоогийн дугаар болон хүлээлтийн тоо.
    /// GET /api/ticket/status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new
    {
        currentNumber = _queueService.GetCurrentNumber(),
        queueCount = _queueService.GetQueueCount()
    });
}