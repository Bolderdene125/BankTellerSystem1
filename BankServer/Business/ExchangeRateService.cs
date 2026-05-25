using BankServer.Data;
using BankSystem.Shared.Entities;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankServer.Business;

/// <summary>
/// ICurrencyRateRepository interface-г хэрэгжүүлнэ.
/// Валютын ханш унших, шинэчлэх үйлдлүүд энд байна.
/// Теллер ханш өөрчлөхөд SignalR-ээр Blazor дэлгэцэд realtime явуулна.
/// </summary>
public class ExchangeRateService(BankDbContext db) : ICurrencyRateRepository
{
    // ── ICurrencyRateRepository хэрэгжүүлэлт ────────────────────────────

    /// <summary>Бүх валютын ханшийн жагсаалт — ICurrencyRateRepository гэрээ.</summary>
    public async Task<IEnumerable<currencyRate>> GetAllAsync() =>
        await db.CurrencyRates.ToListAsync();

    /// <summary>Валютын кодоор ханш хайна — ICurrencyRateRepository гэрээ.</summary>
    public async Task<currencyRate?> GetByCurrencyCodeAsync(string currencyCode) =>
        await db.CurrencyRates
            .FirstOrDefaultAsync(r => r.CurrencyCode == currencyCode);

    /// <summary>Ханш шинэчилнэ — ICurrencyRateRepository гэрээ.</summary>
    public async Task UpdateAsync(currencyRate rate)
    {
        db.CurrencyRates.Update(rate);
        await db.SaveChangesAsync();
    }

    // ── Нэмэлт үйлдлүүд ─────────────────────────────────────────────────

    /// <summary>
    /// Валютын ханшийг шинэчилнэ — ExchangeRateController дуудна.
    /// Амжилттай бол true, валют олдохгүй бол false буцаана.
    /// </summary>
    public async Task<bool> UpdateRateAsync(
        string currencyCode, decimal buyRate, decimal sellRate)
    {
        var rate = await db.CurrencyRates
            .FirstOrDefaultAsync(r => r.CurrencyCode == currencyCode);

        if (rate is null) return false;

        rate.BuyRate = buyRate;
        rate.SellRate = sellRate;
        rate.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }
}