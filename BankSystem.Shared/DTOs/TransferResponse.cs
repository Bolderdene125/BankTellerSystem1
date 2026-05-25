namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// Мөнгө шилжүүлэх хүсэлтийн хариу — сервер TellerApp-д буцаана
    /// </summary>
    public class TransferResponse
    {
        /// <summary>Гүйлгээ амжилттай эсэх</summary>
        public bool Success { get; set; }

        /// <summary>Амжилтгүй бол алдааны мэдэгдэл, амжилттай бол null</summary>
        public string? Message { get; set; }

        /// <summary>Үүссэн гүйлгээний бүртгэлийн ID</summary>
        public int? TransactionId { get; set; }
    }
}
