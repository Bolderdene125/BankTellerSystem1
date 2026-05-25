namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// Мөнгө шилжүүлэх хүсэлт — TellerApp-аас сервер рүү явуулна
    /// </summary>
    public class TransferRequest
    {
        /// <summary>Мөнгө гарах дансны дугаар</summary>
        public string FromAccountNumber { get; set; } = string.Empty;

        /// <summary>Мөнгө орох дансны дугаар</summary>
        public string ToAccountNumber { get; set; } = string.Empty;

        /// <summary>Шилжүүлэх дүн</summary>
        public decimal Amount { get; set; }

        /// <summary>Гүйлгээ хийж буй теллерийн цонхны ID</summary>
        public int TellerWindowId { get; set; }
    }
}
