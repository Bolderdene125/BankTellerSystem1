using BankServer.Business;
using BankServer.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankTests;

/// <summary>
/// ExchangeRateService-ийн тестүүд.
/// InMemory database ашигладаг.
/// </summary>
public class ExchangeRateServiceTests
{
    /// <summary>Тест бүрт тусдаа InMemory database үүсгэнэ.</summary>
    private static ExchangeRateService NewService()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BankDbContext(options);
        db.Database.EnsureCreated();
        return new ExchangeRateService(db);
    }

    /// <summary>Анхны ханш дүүргэгдсэн байгааг шалгана.</summary>
    [Fact]
    public async Task GetAll_ReturnsInitialRates()
    {
        var rates = (await NewService().GetAllAsync()).ToList();
        Assert.NotEmpty(rates);
        Assert.Contains(rates, r => r.Currency == "USD");
    }

    /// <summary>4 валют байгааг шалгана.</summary>
    [Fact]
    public async Task GetAll_ReturnsFourCurrencies()
    {
        var rates = (await NewService().GetAllAsync()).ToList();
        Assert.Equal(4, rates.Count);
    }

    /// <summary>Байгаа валютын ханш авахад null биш байх ёстой.</summary>
    [Fact]
    public async Task Get_ExistingCurrency_ReturnsRate()
    {
        var svc = NewService();
        var rate = await svc.GetAsync("USD");

        Assert.NotNull(rate);
        Assert.Equal("USD", rate.Currency);
    }

    /// <summary>Байхгүй валют хайхад null буцаана.</summary>
    [Fact]
    public async Task Get_NonExistentCurrency_ReturnsNull()
    {
        Assert.Null(await NewService().GetAsync("XYZ"));
    }

    /// <summary>Ханш шинэчлэгдсэний дараа шинэ утга буцааж байгааг шалгана.</summary>
    [Fact]
    public async Task Update_ValidCurrency_UpdatesRate()
    {
        var svc = NewService();
        await svc.UpdateAsync("USD", 3500, 3520);

        var rate = await svc.GetAsync("USD");
        Assert.Equal(3500, rate!.BuyRate);
        Assert.Equal(3520, rate.SellRate);
    }

    /// <summary>Байхгүй валют шинэчлэхэд false буцаах ёстой.</summary>
    [Fact]
    public async Task Update_InvalidCurrency_ReturnsFalse()
    {
        Assert.False(await NewService().UpdateAsync("XYZ", 100, 110));
    }

    /// <summary>Ханш шинэчлэгдсэний дараа UpdatedAt өөрчлөгдөх ёстой.</summary>
    [Fact]
    public async Task Update_ValidCurrency_UpdatesTimestamp()
    {
        var svc = NewService();
        var before = (await svc.GetAsync("USD"))!.UpdatedAt;

        await Task.Delay(10);
        await svc.UpdateAsync("USD", 3500, 3520);

        Assert.True((await svc.GetAsync("USD"))!.UpdatedAt > before);
    }

    /// <summary>SellRate нь BuyRate-ээс их байгааг шалгана.</summary>
    [Fact]
    public async Task GetAll_SellRateHigherThanBuyRate()
    {
        var rates = await NewService().GetAllAsync();
        foreach (var rate in rates)
            Assert.True(rate.SellRate > rate.BuyRate,
                $"{rate.Currency}: SellRate ({rate.SellRate}) BuyRate ({rate.BuyRate})-аас их байх ёстой");
    }
}