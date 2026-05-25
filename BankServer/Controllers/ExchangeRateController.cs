using BankServer.Business;
using BankServer.Domain.DTOs;
using BankServer.Hubs;
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
    public ActionResult<IEnumerable<ExchangeRateResponseDto>> GetAll() =>
        Ok(_rateService.GetAll()
            .Select(r => new ExchangeRateResponseDto(
                r.Currency, r.BuyRate, r.SellRate, r.UpdatedAt)));

    /// <summary>
    /// Ханш шинэчилнэ. SignalR-ээр Blazor-д тэр даруй явуулна.
    /// PUT /api/exchangerate/USD
    /// Body: { "buyRate": 3450, "sellRate": 3470 }
    /// </summary>
    [HttpPut("{currency}")]
    public async Task<ActionResult<ExchangeRateResponseDto>> Update(
        string currency, [FromBody] RateUpdateRequestDto req)
    {
        if (!_rateService.Update(currency, req.BuyRate, req.SellRate))
            return NotFound($"'{currency}' валют олдсонгүй");

        var updated = _rateService.Get(currency)!;
        var dto = new ExchangeRateResponseDto(
            updated.Currency, updated.BuyRate, updated.SellRate, updated.UpdatedAt);

        // SignalR — Blazor дэлгэц тэр даруй шинэчлэгдэнэ
        await _hub.Clients.All.SendAsync("ReceiveRateUpdate", dto);

        return Ok(dto);
    }
}