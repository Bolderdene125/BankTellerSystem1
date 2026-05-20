using BankServer.Hubs;
using BankServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BankServer.Controllers;

/// <summary>Ханш харах, теллер шинэчлэх endpoint-ууд.</summary>
[ApiController]
[Route("api/[controller]")]
public class ExchangeRateController : ControllerBase
{
    private readonly ExchangeRateService _rateService;
    private readonly IHubContext<BankHub> _hub;

    public ExchangeRateController(ExchangeRateService rateService, IHubContext<BankHub> hub)
    {
        _rateService = rateService;
        _hub = hub;
    }

    /// <summary>Бүх валютын ханш. GET /api/exchangerate</summary>
    [HttpGet]
    public IActionResult GetAll() => Ok(_rateService.GetAll());

    /// <summary>
    /// Ханш шинэчилнэ. Шинэчилсний дараа SignalR-ээр Blazor дэлгэцэнд явуулна.
    /// PUT /api/exchangerate/USD
    /// Body: { "buyRate": 3450, "sellRate": 3470 }
    /// </summary>
    [HttpPut("{currency}")]
    public async Task<IActionResult> Update(string currency, [FromBody] RateUpdateRequest req)
    {
        if (!_rateService.Update(currency, req.BuyRate, req.SellRate))
            return NotFound($"'{currency}' валют олдсонгүй");

        var updated = _rateService.Get(currency);

        // "ReceiveRateUpdate" — Blazor дэлгэц энэ event-г сонсоод тэр даруй шинэчлэгдэнэ
        await _hub.Clients.All.SendAsync("ReceiveRateUpdate", updated);

        return Ok(updated);
    }
}

/// <summary>Ханш шинэчлэх хүсэлтийн body загвар.</summary>
public record RateUpdateRequest(decimal BuyRate, decimal SellRate);