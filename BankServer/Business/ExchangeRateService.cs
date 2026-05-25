using System.Collections.Concurrent;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;

namespace BankServer.Business;

/// <summary>
/// Валютын ханшийн бизнес логик.
/// ICurrencyRateRepository interface хэрэгжүүлнэ — Shared гэрээний дагуу.
/// ConcurrentDictionary: thread-safe, нэмэлт lock шаардахгүй.
/// </summary>
public class ExchangeRateService : ICurrencyRateRepository
{
    /// <summary>
    /// Ханшийн санах ой. Key = валютын код.
    /// Тест болон demo-д in-memory ашиглана.
    /// </summary>
    private readonly ConcurrentDictionary<string, RateEntry> _rates = new()
    {
        ["USD"] = new("USD", "Америк доллар", 3440, 3460, DateTime.Now),
        ["EUR"] = new("EUR", "Евро", 3750, 3780, DateTime.Now),
        ["CNY"] = new("CNY", "Хятад юань", 475, 480, DateTime.Now),
        ["RUB"] = new("RUB", "Оросын рубль", 38, 40, DateTime.Now),
    };

    /// <summary>Бүх ханш — тест sync хэлбэрээр дуудна.</summary>
    public IEnumerable<RateEntry> GetAll() => _rates.Values;

    /// <summary>Нэг валютын ханш — тест sync хэлбэрээр дуудна.</summary>
    public RateEntry? Get(string currency) =>
        _rates.TryGetValue(currency, out var r) ? r : null;

    /// <summary>
    /// Ханш шинэчилнэ — тест болон Controller дуудна.
    /// Байхгүй валют бол false буцаана.
    /// </summary>
    public bool Update(string currency, decimal buyRate, decimal sellRate)
    {
        if (!_rates.ContainsKey(currency)) return false;
        _rates[currency] = new RateEntry(
            currency,
            _rates[currency].CurrencyName,
            buyRate, sellRate,
            DateTime.Now);
        return true;
    }

    // ── ICurrencyRateRepository хэрэгжүүлэлт (Shared гэрээ) ─────────────

    /// <summary>Бүх ханш async — ICurrencyRateRepository гэрээ.</summary>
    public Task<IEnumerable<currencyRate>> GetAllAsync()
    {
        var result = _rates.Values.Select(r => ToCurrencyRate(r));
        return Task.FromResult(result);
    }

    /// <summary>Кодоор хайна — ICurrencyRateRepository гэрээ.</summary>
    public Task<currencyRate?> GetByCurrencyCodeAsync(string currencyCode)
    {
        var r = Get(currencyCode);
        return Task.FromResult(r is null ? null : (currencyRate?)ToCurrencyRate(r));
    }

    /// <summary>Ханш шинэчилнэ — ICurrencyRateRepository гэрээ.</summary>
    public Task UpdateAsync(currencyRate rate)
    {
        Update(rate.CurrencyCode, rate.BuyRate, rate.SellRate);
        return Task.CompletedTask;
    }

    private static currencyRate ToCurrencyRate(RateEntry r) => new()
    {
        CurrencyCode = r.Currency,
        CurrencyName = r.CurrencyName,
        BuyRate = r.BuyRate,
        SellRate = r.SellRate,
        UpdatedAt = r.UpdatedAt
    };
}

/// <summary>
/// Ханшийн дотоод загвар — tест гэрээний дагуу:
/// Currency, CurrencyName, BuyRate, SellRate, UpdatedAt талбартай.
/// </summary>
public record RateEntry(
    string Currency,
    string CurrencyName,
    decimal BuyRate,
    decimal SellRate,
    DateTime UpdatedAt);