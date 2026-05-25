using BankServer.Business;
using Xunit;

namespace BankTests;

/// <summary>ExchangeRateService-ийн CRUD тестүүд.</summary>
public class ExchangeRateServiceTests
{
    private ExchangeRateService NewService() => new();

    /// <summary>Анхны ханш дүүргэгдсэн байгааг шалгана.</summary>
    [Fact]
    public void GetAll_ReturnsInitialRates()
    {
        var rates = NewService().GetAll().ToList();
        Assert.NotEmpty(rates);
        Assert.Contains(rates, r => r.Currency == "USD");
    }

    /// <summary>4 валют байгааг шалгана.</summary>
    [Fact]
    public void GetAll_ReturnsFourCurrencies()
    {
        var rates = NewService().GetAll().ToList();
        Assert.Equal(4, rates.Count);
    }

    /// <summary>Байгаа валютын ханш авахад null биш байх ёстой.</summary>
    [Fact]
    public void Get_ExistingCurrency_ReturnsRate()
    {
        var svc = NewService();
        var rate = svc.Get("USD");

        Assert.NotNull(rate);
        Assert.Equal("USD", rate.Currency);
    }

    /// <summary>Байхгүй валют хайхад null буцаана.</summary>
    [Fact]
    public void Get_NonExistentCurrency_ReturnsNull()
    {
        Assert.Null(NewService().Get("XYZ"));
    }

    /// <summary>Ханш шинэчлэгдсэний дараа шинэ утга буцааж байгааг шалгана.</summary>
    [Fact]
    public void Update_ValidCurrency_UpdatesRate()
    {
        var svc = NewService();
        svc.Update("USD", 3500, 3520);

        Assert.Equal(3500, svc.Get("USD")!.BuyRate);
        Assert.Equal(3520, svc.Get("USD")!.SellRate);
    }

    /// <summary>Байхгүй валют шинэчлэхэд false буцаах ёстой.</summary>
    [Fact]
    public void Update_InvalidCurrency_ReturnsFalse()
    {
        Assert.False(NewService().Update("XYZ", 100, 110));
    }

    /// <summary>Ханш шинэчлэгдсэний дараа UpdatedAt өөрчлөгдөх ёстой.</summary>
    [Fact]
    public void Update_ValidCurrency_UpdatesTimestamp()
    {
        var svc = NewService();
        var before = svc.Get("USD")!.UpdatedAt;

        Thread.Sleep(10);
        svc.Update("USD", 3500, 3520);

        Assert.True(svc.Get("USD")!.UpdatedAt > before);
    }

    /// <summary>SellRate нь BuyRate-ээс их байгааг шалгана.</summary>
    [Fact]
    public void GetAll_SellRateHigherThanBuyRate()
    {
        var rates = NewService().GetAll();
        foreach (var rate in rates)
            Assert.True(rate.SellRate > rate.BuyRate,
                $"{rate.Currency}: SellRate ({rate.SellRate}) BuyRate ({rate.BuyRate})-аас их байх ёстой");
    }
}