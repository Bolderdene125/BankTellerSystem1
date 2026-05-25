namespace BankSystem.Shared.Entities
{
    /// <summary>
    /// Валютын ханшийн мэдээлэл — теллер өөрчилж, дэлгэцэнд realtime харагдана
    /// </summary>
    public class currencyRate
    {
        /// <summary>Өгөгдлийн сангийн өвөрмөц дугаар</summary>
        public int Id { get; set; }

        /// <summary>Валютын код (жишээ: USD, EUR, CNY)</summary>
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Валютын бүтэн нэр (жишээ: Америк доллар)</summary>
        public string CurrencyName { get; set; } = string.Empty;

        /// <summary>Худалдан авах ханш (банк харилцагчаас валют авах үнэ)</summary>
        public decimal BuyRate { get; set; }

        /// <summary>Худалдах ханш (банк харилцагчид валют зарах үнэ)</summary>
        public decimal SellRate { get; set; }

        /// <summary>Ханш сүүлд өөрчлөгдсөн огноо цаг</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>Ханш өөрчилсөн теллерийн цонхны ID</summary>
        public int UpdatedByTellerWindowId { get; set; }
    }
}
