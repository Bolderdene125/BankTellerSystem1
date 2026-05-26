namespace BankSystem.Shared.Entities
{
    /// <summary>
    /// Валютын ханшийн загвар.
    /// Теллер өөрчилж, CurrencyDisplay Blazor-д realtime харагдана.
    /// UpdatedBy — хэн теллер ханш өөрчилснийг бүртгэнэ (аудит).
    ///
    /// ЗАСВАР: currencyRate → CurrencyRate (C# PascalCase дүрэм)
    /// </summary>
    public class CurrencyRate
    {
        /// <summary>Өгөгдлийн сангийн өвөрмөц дугаар</summary>
        public int Id { get; set; }

        /// <summary>Валютын код (жишээ: USD, EUR, CNY)</summary>
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Валютын бүтэн нэр (жишээ: Америк доллар)</summary>
        public string CurrencyName { get; set; } = string.Empty;

        /// <summary>Авах ханш — банк харилцагчаас валют авах үнэ</summary>
        public decimal BuyRate { get; set; }

        /// <summary>Зарах ханш — банк харилцагчид валют зарах үнэ</summary>
        public decimal SellRate { get; set; }

        /// <summary>Ханш сүүлд өөрчлөгдсөн огноо цаг</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Ханш өөрчилсөн теллерийн цонхны ID.
        /// Аудитын шаардлагаар хэн өөрчилснийг мэдэх боломж олгоно.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
