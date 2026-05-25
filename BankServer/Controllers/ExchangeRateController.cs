using BankServer.Business;
using BankServer.Domain.DTOs;
using BankServer.Hubs;
using BankSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BankServer.Controllers;

/// <summary>
/// Валютын ханш харах болон шинэчлэх endpoint-ууд.
/// GET /api/exchangerate              — бүх ханш
/// PUT /api/exchangerate/{currencyCode} — ханш шинэчлэх + SignalR broadcast
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
        _hub = hub;
    }

    /// <summary>Бүх валютын ханшийн жагсаалт буцаана.</summary>
    [HttpGet]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExchangeRateResponseDto>>> GetAll()
    {
        var rates = await _rateService.GetAllAsync();
        return Ok(rates.Select(r => new ExchangeRateResponseDto(
            r.CurrencyCode, r.CurrencyName, r.BuyRate, r.SellRate, r.UpdatedAt)));
    }

    /// <summary>
    /// Ханш шинэчилж SignalR-ээр Blazor дэлгэцэнд realtime явуулна.
    /// UpdateRateRequest — Shared.DTOs-аас авна:
    ///   BuyRate, SellRate, TellerWindowId
    /// </summary>
    [HttpPut("{currencyCode}")]
    public async Task<ActionResult<ExchangeRateResponseDto>> Update(
        string currencyCode, [FromBody] UpdateRateRequest req)
    {
        if (!await _rateService.UpdateRateAsync(currencyCode, req.BuyRate, req.SellRate))
            return NotFound($"'{currencyCode}' валют олдсонгүй");

        var updated = await _rateService.GetByCurrencyCodeAsync(currencyCode);
        var dto = new ExchangeRateResponseDto(
            updated!.CurrencyCode, updated.CurrencyName,
            updated.BuyRate, updated.SellRate, updated.UpdatedAt);

        // SignalR — CurrencyDisplay Blazor-д realtime явуулна
        await _hub.Clients.All.SendAsync("ReceiveRateUpdate", dto);
        return Ok(dto);
    }
}