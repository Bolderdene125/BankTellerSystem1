using BankServer.Business;
using BankServer.Domain.DTOs;
using BankServer.Hubs;
using BankSystem.Shared.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BankServer.Controllers;

/// <summary>
/// Валютын ханш харах болон шинэчлэх endpoint-ууд.
///
/// GET  /api/exchangerate               — бүх ханш
/// PUT  /api/exchangerate/{code}        — ханш шинэчлэх + SignalR broadcast
/// GET  /api/exchangerate/history       — ханшийн өөрчлөлтийн түүх (ШИНЭ)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExchangeRateController : ControllerBase
{
    private readonly ExchangeRateService _rateService;
    private readonly IHubContext<BankHub> _hub;

    public ExchangeRateController(
        ExchangeRateService rateService, IHubContext<BankHub> hub)
    {
        _rateService = rateService;
        _hub         = hub;
    }

    /// <summary>Бүх валютын ханшийн жагсаалт буцаана.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponseDto>>> GetAll()
    {
        var rates = await _rateService.GetAllAsync();
        return Ok(rates.Select(r => new ExchangeRateResponseDto(
            r.CurrencyCode, r.CurrencyName,
            r.BuyRate, r.SellRate, r.UpdatedAt, r.UpdatedBy)));
    }

    /// <summary>
    /// Ханш шинэчилж SignalR-ээр Blazor дэлгэцэнд realtime явуулна.
    /// ЗАСВАР: UpdatedBy дамжуулагдана — хэн өөрчилснийг мэдэнэ.
    /// </summary>
    [HttpPut("{currencyCode}")]
    public async Task<ActionResult<ExchangeRateResponseDto>> Update(
        string currencyCode, [FromBody] UpdateRateRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (req.SellRate <= req.BuyRate)
            return BadRequest("Зарах ханш авах ханшаас их байх ёстой");

        if (!await _rateService.UpdateRateAsync(
                currencyCode, req.BuyRate, req.SellRate, req.UpdatedBy))
            return NotFound($"'{currencyCode}' валют олдсонгүй");

        var updated = await _rateService.GetByCurrencyCodeAsync(currencyCode);
        var dto = new ExchangeRateResponseDto(
            updated!.CurrencyCode, updated.CurrencyName,
            updated.BuyRate, updated.SellRate,
            updated.UpdatedAt, updated.UpdatedBy);

        // SignalR — CurrencyDisplay Blazor-д realtime явуулна
        await _hub.Clients.All.SendAsync("ReceiveRateUpdate", dto);
        return Ok(dto);
    }

    /// <summary>
    /// Ханшийн өөрчлөлтийн бүртгэлийн түүх.
    /// ШИНЭ: аудитад ашиглана.
    /// </summary>
    [HttpGet("history")]
    public ActionResult GetHistory() =>
        Ok(_rateService.GetHistory().Select(h => new
        {
            h.Currency,
            OldBuy    = h.OldBuy.ToString("N0"),
            OldSell   = h.OldSell.ToString("N0"),
            NewBuy    = h.NewBuy.ToString("N0"),
            NewSell   = h.NewSell.ToString("N0"),
            ChangedAt = h.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            h.ChangedBy
        }));
}
