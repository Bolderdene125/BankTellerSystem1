namespace BankServer.Models;

/// <summary>Нэг валютын ханшийн мэдээлэл.</summary>
public class ExchangeRate
{
    /// <summary>ISO 4217 валютын код. Жишээ: "USD", "EUR".</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Харилцагчаас валют авах ханш (₮).</summary>
    public decimal BuyRate { get; set; }

    /// <summary>Харилцагчид валют зарах ханш (₮). SellRate > BuyRate байна.</summary>
    public decimal SellRate { get; set; }

    /// <summary>Хамгийн сүүлд шинэчлэгдсэн цаг.</summary>
    public DateTime UpdatedAt { get; set; }
}