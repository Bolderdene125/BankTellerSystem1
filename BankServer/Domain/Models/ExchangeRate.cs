namespace BankServer.Domain.Models;

/// <summary>
/// ExchangeRateService-д ашиглагдах ханшийн дотоод загвар.
/// Shared.Entities.currencyRate-аас Currency талбарын нэрээрээ ялгаана.
/// </summary>
public class ExchangeRate
{
    /// <summary>Валютын код (USD, EUR, CNY, RUB)</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Авах ханш — банк харилцагчаас валют авах үнэ</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Зарах ханш — банк харилцагчид валют зарах үнэ</summary>
    public decimal SellRate { get; set; }

    /// <summary>Ханш сүүлд өөрчлөгдсөн цаг</summary>
    public DateTime UpdatedAt { get; set; }
}
