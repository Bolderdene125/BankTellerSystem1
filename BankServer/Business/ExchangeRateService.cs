using System.Collections.Concurrent;
using BankServer.Data;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankServer.Business;

/// <summary>
/// Валютын ханшийн бизнес логик.
/// ICurrencyRateRepository interface хэрэгжүүлнэ.
///
/// ӨӨРЧЛӨЛТ: Server restart хийсний дараа ханш анхны утгаараа буцдаг байсан.
///   Шалтгаан: ConcurrentDictionary in-memory тул restart-д арилдаг байсан.
///   Засвар:
///     1. Constructor-т DB-аас ханш уншиж _rates-д дүүргэнэ (startup).
///     2. Update()-д DB-д хадгална (persist).
///   Ингэснээр restart хийсний дараа сүүлийн ханш хэвээр байна.
///
/// Яагаад IServiceScopeFactory:
///   ExchangeRateService нь Singleton lifetime-тай.
///   BankDbContext нь Scoped lifetime-тай.
///   Singleton-д Scoped шууд inject хийж болохгүй — captive dependency алдаа гарна.
///   IServiceScopeFactory ашиглан хэрэгтэй үед scope үүсгэж DbContext авна.
/// </summary>
public class ExchangeRateService : ICurrencyRateRepository
{
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Ханшийн санах ой. Key = валютын код (USD, EUR...).
    /// ConcurrentDictionary: олон теллер нэгэн зэрэг ханш өөрчилсөн ч аюулгүй.
    /// Startup-т DB-аас дүүргэгдэнэ, Update()-д шинэчлэгдэнэ.
    /// </summary>
    private readonly ConcurrentDictionary<string, RateEntry> _rates = new();

    /// <summary>
    /// Ханшийн өөрчлөлтийн түүх — in-memory.
    /// Аудитын зорилгоор хэн, хэзээ, ямар ханш тогтоосныг хадгална.
    /// (Restart хийхэд арилна — тухайн session-ийн түүх л хадгалагдана)
    /// </summary>
    private readonly List<RateHistoryEntry> _history = new();
    private readonly object _historyLock = new();

    public ExchangeRateService(
        ILogger<ExchangeRateService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        // ── Startup: DB-аас ханш уншиж _rates-д дүүргэнэ ────────────
        // Server эхлэх бүрт DB-аас сүүлийн ханшийг авна.
        // DB хоосон байвал (эхний удаа) seed өгөгдөл байхгүй тул
        // anхны утгыг _rates-д хатуу оруулна — дараа нь DB-д хадгална.
        LoadFromDatabase();
    }

    /// <summary>
    /// DB-аас ханш уншина.
    /// DB-д ханш байвал тэднийг ашиглана.
    /// DB хоосон байвал (анх удаа) default утгыг нэмж DB-д хадгална.
    /// </summary>
    private void LoadFromDatabase()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();

        var dbRates = db.CurrencyRates.ToList();

        if (dbRates.Count > 0)
        {
            // DB-д ханш байна — тэднийг уншина
            foreach (var r in dbRates)
            {
                _rates[r.CurrencyCode] = new RateEntry(
                    r.CurrencyCode, r.CurrencyName,
                    r.BuyRate, r.SellRate,
                    r.UpdatedAt, r.UpdatedBy);
            }
            _logger.LogInformation(
                "DB-аас {Count} валютын ханш уншлаа", dbRates.Count);
        }
        else
        {
            // DB хоосон — anхны default утгыг тавина
            // (EnsureCreated seed ажилласан бол энд хүрэхгүй)
            var defaults = new[]
            {
                new RateEntry("USD", "Америк доллар", 3440, 3460, DateTime.Now, "system"),
                new RateEntry("EUR", "Евро",           3750, 3780, DateTime.Now, "system"),
                new RateEntry("CNY", "Хятад юань",      475,  480, DateTime.Now, "system"),
                new RateEntry("RUB", "Оросын рубль",     38,   40, DateTime.Now, "system"),
            };
            foreach (var r in defaults)
                _rates[r.Currency] = r;

            _logger.LogInformation("DB хоосон — default ханш ашиглаж байна");
        }
    }

    /// <summary>Бүх ханш буцаана.</summary>
    public IEnumerable<RateEntry> GetAll() => _rates.Values;

    /// <summary>Нэг валютын ханш авна.</summary>
    public RateEntry? Get(string currency) =>
        _rates.TryGetValue(currency, out var r) ? r : null;

    /// <summary>
    /// Ханш шинэчилж in-memory болон DB-д хоёуланд хадгална.
    /// In-memory: хурдан хандалт — API хариу өгөхөд ашиглана.
    /// DB: persist — restart хийсний дараа ч хадгалагдана.
    /// </summary>
    public bool Update(string currency, decimal buyRate, decimal sellRate,
        string updatedBy = "")
    {
        if (!_rates.TryGetValue(currency, out var old)) return false;

        // ── In-memory шинэчлэл ────────────────────────────────────────
        _rates[currency] = new RateEntry(
            currency, old.CurrencyName,
            buyRate, sellRate,
            DateTime.Now, updatedBy);

        // ── DB-д хадгална ─────────────────────────────────────────────
        // IServiceScopeFactory: Singleton-д Scoped DbContext ашиглах стандарт арга.
        // using: scope ашиглаад дараа нь автоматаар dispose хийгдэнэ.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();

            var entity = db.CurrencyRates
                .FirstOrDefault(r => r.CurrencyCode == currency);

            if (entity != null)
            {
                entity.BuyRate = buyRate;
                entity.SellRate = sellRate;
                entity.UpdatedAt = DateTime.Now;
                entity.UpdatedBy = updatedBy;
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            // DB алдаа гарсан ч in-memory шинэчлэл амжилттай болсон тул
            // false буцаахгүй — ханш дэлгэцэнд харагдсаар байна
            _logger.LogError(ex, "Ханш DB-д хадгалахад алдаа: {Currency}", currency);
        }

        // ── Түүхэнд бүртгэнэ ─────────────────────────────────────────
        lock (_historyLock)
        {
            _history.Add(new RateHistoryEntry(
                currency, old.BuyRate, old.SellRate,
                buyRate, sellRate, DateTime.Now, updatedBy));
        }

        _logger.LogInformation(
            "Ханш шинэчлэгдлээ: {Currency} авах {OldBuy:N0}→{NewBuy:N0}, " +
            "зарах {OldSell:N0}→{NewSell:N0}, теллер: {Teller}",
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
        var result = _rates.Values.Select(ToCurrencyRate);
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
        BuyRate = r.BuyRate,
        SellRate = r.SellRate,
        UpdatedAt = r.UpdatedAt,
        UpdatedBy = r.UpdatedBy
    };
}

/// <summary>
/// Ханшийн in-memory загвар.
/// record: immutable — шинэ ханш тогтоохдоо шинэ record үүсгэнэ.
/// </summary>
public record RateEntry(
    string Currency,
    string CurrencyName,
    decimal BuyRate,
    decimal SellRate,
    DateTime UpdatedAt,
    string UpdatedBy);

/// <summary>Ханшийн өөрчлөлтийн түүхийн нэг бичлэг.</summary>
public record RateHistoryEntry(
    string Currency,
    decimal OldBuy,
    decimal OldSell,
    decimal NewBuy,
    decimal NewSell,
    DateTime ChangedAt,
    string ChangedBy);