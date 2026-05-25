namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// Валютын ханш шинэчлэх хүсэлт — TellerApp-аас сервер рүү явуулна
    /// (Валютын код нь URL-д байна: PUT /api/exchangerate/{code})
    /// </summary>
    public class UpdateRateRequest
    {
        /// <summary>Худалдан авах ханш</summary>
        public decimal BuyRate { get; set; }

        /// <summary>Худалдах ханш</summary>
        public decimal SellRate { get; set; }

        /// <summary>Шинэчилж буй теллерийн цонхны ID</summary>
        public int TellerWindowId { get; set; }
    }
}
