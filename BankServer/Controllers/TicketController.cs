using BankServer.Business;
using BankServer.Domain.DTOs;
using BankServer.Hubs;
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

    public TicketController(TicketQueueService queueService, IHubContext<BankHub> hub)
    {
        _queueService = queueService;
        _hub = hub;
    }

    /// <summary>
    /// Шинэ тасалбар олгоно. Дугаар авах терминалаас дуудна.
    /// POST /api/ticket/issue
    /// Body: { "serviceType": "Гүйлгээ" }
    /// </summary>
    [HttpPost("issue")]
    public async Task<ActionResult<IssueTicketResponseDto>> IssueTicket(
        [FromBody] IssueTicketRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.ServiceType))
            return BadRequest("ServiceType хоосон байж болохгүй");

        var ticket = await _queueService.IssueTicketAsync(req.ServiceType);

        // Model → DTO хөрвүүлэлт — клиент зөвхөн хэрэгтэй өгөгдлийг авна
        return Ok(new IssueTicketResponseDto(
            ticket.Number,
            ticket.IssuedAt,
            ticket.ServiceType,
            _queueService.GetQueueCount()
        ));
    }

    /// <summary>
    /// Дарааллаас дараагийн дугаарыг авч SignalR-ээр дэлгэцүүдэд явуулна.
    /// POST /api/ticket/call-next
    /// </summary>
    [HttpPost("call-next")]
    public async Task<ActionResult<CallNextResponseDto>> CallNext()
    {
        int number = await _queueService.CallNextAsync();

        // SignalR — бүх холбогдсон дэлгэцэнд тэр даруй явуулна
        await _hub.Clients.All.SendAsync("ReceiveNumberUpdate", number);

        return Ok(new CallNextResponseDto(number));
    }

    /// <summary>
    /// Одоогийн дугаар болон хүлээлтийн тоо.
    /// GET /api/ticket/status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<QueueStatusDto> GetStatus() =>
        Ok(new QueueStatusDto(
            _queueService.GetCurrentNumber(),
            _queueService.GetQueueCount()
        ));
}