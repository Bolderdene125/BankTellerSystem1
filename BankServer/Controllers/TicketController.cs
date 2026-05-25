using BankServer.Business;
using BankServer.Domain.DTOs;
using BankServer.Hubs;
using BankSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BankServer.Controllers;

/// <summary>
/// Дугаарын дараалал удирдах endpoint-ууд.
/// POST /api/ticket/issue             — шинэ дугаар олгох
/// POST /api/ticket/call-next?roomId= — дараагийн дугаар дуудах
/// GET  /api/ticket/status            — дарааллын байдал
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly TicketQueueService _queueService;
    private readonly IHubContext<BankHub> _hub;

    public TicketController(
        TicketQueueService queueService, IHubContext<BankHub> hub)
    {
        _queueService = queueService;
        _hub = hub;
    }

    /// <summary>
    /// Шинэ тасалбар олгоно — NumberTerminal дуудна.
    /// IssueTicketResponse — Shared.DTOs-аас авна:
    ///   TicketNumber, IssuedAt, QueueCount
    /// </summary>
    [HttpPost("issue")]
    public async Task<ActionResult<IssueTicketResponse>> IssueTicket(
        [FromBody] IssueTicketRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.ServiceType))
            return BadRequest("ServiceType хоосон байж болохгүй");

        var ticket = await _queueService.IssueTicketAsync(req.ServiceType);

        // SignalR — NumberDisplay дэлгэцүүдэд шинэ дугаар гарсныг мэдэгдэнэ
        await _hub.Clients.All.SendAsync("ReceiveNumberUpdate", ticket.Number);

        return Ok(new IssueTicketResponse
        {
            TicketNumber = ticket.Number,
            IssuedAt = ticket.IssuedAt,
            QueueCount = _queueService.GetQueueCount()
        });
    }

    /// <summary>
    /// Дарааллаас дараагийн дугаарыг авна — TellerApp дуудна.
    /// roomId query param: "305" эсвэл "306" — SocketServer зөвхөн
    /// тухайн өрөөний дэлгэцэнд TCP явуулна.
    /// CallNextResponse — Shared.DTOs-аас авна:
    ///   TicketNumber, TellerWindowId, RemainingCount
    /// </summary>
    [HttpPost("call-next")]
    public async Task<ActionResult<CallNextResponse>> CallNext(
        [FromQuery] string roomId = "000")
    {
        int number = await _queueService.CallNextAsync();

        // SignalR — roomId хамт явуулна, SocketServer тухайн дэлгэцэнд л TCP явуулна
        await _hub.Clients.All.SendAsync("ReceiveTellerCall", number, roomId);

        return Ok(new CallNextResponse
        {
            TicketNumber = number,
            TellerWindowId = 0,
            RemainingCount = _queueService.GetQueueCount()
        });
    }

    /// <summary>
    /// Дарааллын одоогийн байдал буцаана.
    /// CurrentNumber — сүүлд дуудсан дугаар.
    /// QueueCount — одоо хүлээж байгаа хүний тоо.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<QueueStatusDto> GetStatus() =>
        Ok(new QueueStatusDto(
            _queueService.GetCurrentNumber(),
            _queueService.GetQueueCount()));
}