using System.Collections.Concurrent;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankServer.Business;

/// <summary>
/// Валютын ханшийн бизнес логик.
/// ICurrencyRateRepository interface хэрэгжүүлнэ.
///
/// ЗАСВАР 1: UpdatedBy — хэн теллер ханш өөрчилснийг бүртгэнэ.
/// ЗАСВАР 2: ILogger нэмэгдсэн.
/// ЗАСВАР 3: Ханшийн өөрчлөлтийн түүх хадгалагдана.
///
/// ConcurrentDictionary: thread-safe, нэмэлт lock шаардахгүй.
/// </summary>
public class ExchangeRateService : ICurrencyRateRepository
{
    private readonly ILogger<ExchangeRateService> _logger;

    /// <summary>
    /// Ханшийн санах ой. Key = валютын код (USD, EUR...).
    /// ConcurrentDictionary: олон теллер нэгэн зэрэг ханш өөрчилсөн ч аюулгүй.
    /// </summary>
    private readonly ConcurrentDictionary<string, RateEntry> _rates = new()
    {
        ["USD"] = new("USD", "Америк доллар", 3440, 3460, DateTime.Now, "system"),
        ["EUR"] = new("EUR", "Евро",           3750, 3780, DateTime.Now, "system"),
        ["CNY"] = new("CNY", "Хятад юань",      475,  480, DateTime.Now, "system"),
        ["RUB"] = new("RUB", "Оросын рубль",     38,   40, DateTime.Now, "system"),
    };

    /// <summary>
    /// Ханшийн өөрчлөлтийн түүх.
    /// Аудитын зорилгоор хэн, хэзээ, ямар ханш тогтоосныг хадгална.
    /// </summary>
    private readonly List<RateHistoryEntry> _history = new();
    private readonly object _historyLock = new();

    public ExchangeRateService(ILogger<ExchangeRateService> logger)
    {
        _logger = logger;
    }

    /// <summary>Бүх ханш — тест sync хэлбэрээр дуудна.</summary>
    public IEnumerable<RateEntry> GetAll() => _rates.Values;

    /// <summary>Нэг валютын ханш авна.</summary>
    public RateEntry? Get(string currency) =>
        _rates.TryGetValue(currency, out var r) ? r : null;

    /// <summary>
    /// Ханш шинэчилнэ.
    /// ЗАСВАР: updatedBy — хэн өөрчилснийг бүртгэнэ.
    /// </summary>
    public bool Update(string currency, decimal buyRate, decimal sellRate,
        string updatedBy = "")
    {
        if (!_rates.ContainsKey(currency)) return false;

        var old = _rates[currency];
        _rates[currency] = new RateEntry(
            currency,
            old.CurrencyName,
            buyRate, sellRate,
            DateTime.Now,
            updatedBy);

        // Өөрчлөлтийн түүхэнд бүртгэнэ
        lock (_historyLock)
        {
            _history.Add(new RateHistoryEntry(
                currency, old.BuyRate, old.SellRate,
                buyRate, sellRate, DateTime.Now, updatedBy));
        }

        _logger.LogInformation(
            "Ханш шинэчлэгдлээ: {Currency} авах {Buy:N0} → {NewBuy:N0}, " +
            "зарах {Sell:N0} → {NewSell:N0}, теллер: {Teller}",
            currency, old.BuyRate, buyRate, old.SellRate, sellRate, updatedBy);

        return true;
    }

    /// <summary>Ханшийн өөрчлөлтийн бүтэн түүх буцаана.</summary>
    public IReadOnlyList<RateHistoryEntry> GetHistory()
    {
        lock (_historyLock) return _history.AsReadOnly();
    }

    // ── ICurrencyRateRepository хэрэгжүүлэлт ────────────────────────────

    public Task<IEnumerable<CurrencyRate>> GetAllAsync()
    {
        var result = _rates.Values.Select(r => ToCurrencyRate(r));
        return Task.FromResult(result);
    }

    public Task<CurrencyRate?> GetByCurrencyCodeAsync(string code)
    {
        var r = Get(code);
        return Task.FromResult(r is null ? null : (CurrencyRate?)ToCurrencyRate(r));
    }

    public Task UpdateAsync(CurrencyRate rate)
    {
        Update(rate.CurrencyCode, rate.BuyRate, rate.SellRate, rate.UpdatedBy);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Async шинэчлэлт — Controller-аас дуудагдана.
    /// UpdatedBy талбарыг хүлээн авна.
    /// </summary>
    public Task<bool> UpdateRateAsync(string code, decimal buy, decimal sell,
        string updatedBy = "")
    {
        return Task.FromResult(Update(code, buy, sell, updatedBy));
    }

    public Task<CurrencyRate?> GetByCurrencyCode(string code) =>
        GetByCurrencyCodeAsync(code);

    private static CurrencyRate ToCurrencyRate(RateEntry r) => new()
    {
        CurrencyCode = r.Currency,
        CurrencyName = r.CurrencyName,
        BuyRate      = r.BuyRate,
        SellRate     = r.SellRate,
        UpdatedAt    = r.UpdatedAt,
        UpdatedBy    = r.UpdatedBy
    };
}

/// <summary>
/// Ханшийн in-memory загвар.
/// Currency, CurrencyName, BuyRate, SellRate, UpdatedAt, UpdatedBy.
/// ЗАСВАР: UpdatedBy талбар нэмэгдсэн.
/// </summary>
public record RateEntry(
    string  Currency,
    string  CurrencyName,
    decimal BuyRate,
    decimal SellRate,
    DateTime UpdatedAt,
    string  UpdatedBy);

/// <summary>Ханшийн өөрчлөлтийн түүхийн нэг бичлэг.</summary>
public record RateHistoryEntry(
    string   Currency,
    decimal  OldBuy,
    decimal  OldSell,
    decimal  NewBuy,
    decimal  NewSell,
    DateTime ChangedAt,
    string   ChangedBy);
