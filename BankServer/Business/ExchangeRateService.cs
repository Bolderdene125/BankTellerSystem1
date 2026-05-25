using System.Collections.Concurrent;
using BankServer.Domain.Models;

namespace BankServer.Business;

/// <summary>
/// Валютын ханшийг санах ойд хадгалдаг service.
/// ConcurrentDictionary: нэмэлт lock шаардахгүй, thread-safe.
/// </summary>
public class ExchangeRateService
{
    private readonly ConcurrentDictionary<string, ExchangeRate> _rates = new()
    {
        ["USD"] = new ExchangeRate { Currency = "USD", BuyRate = 3440, SellRate = 3460, UpdatedAt = DateTime.Now },
        ["EUR"] = new ExchangeRate { Currency = "EUR", BuyRate = 3750, SellRate = 3780, UpdatedAt = DateTime.Now },
        ["CNY"] = new ExchangeRate { Currency = "CNY", BuyRate = 475, SellRate = 480, UpdatedAt = DateTime.Now },
        ["RUB"] = new ExchangeRate { Currency = "RUB", BuyRate = 38, SellRate = 40, UpdatedAt = DateTime.Now },
    };

    /// <summary>Бүх валютын ханш.</summary>
    public IEnumerable<ExchangeRate> GetAll() => _rates.Values;

    /// <summary>Нэг валютын ханш. Байхгүй бол null.</summary>
    public ExchangeRate? Get(string currency) =>
        _rates.TryGetValue(currency, out var rate) ? rate : null;

    /// <summary>
    /// Теллер ханш өөрчлөхөд дуудагдана.
    /// Шинэчилсний дараа Controller SignalR-ээр Blazor-д мэдэгдэнэ.
    /// </summary>
    public bool Update(string currency, decimal buyRate, decimal sellRate)
    {
        if (!_rates.ContainsKey(currency)) return false;
        _rates[currency] = new ExchangeRate
        {
            Currency = currency,
            BuyRate = buyRate,
            SellRate = sellRate,
            UpdatedAt = DateTime.Now
        };
        return true;
    }
}