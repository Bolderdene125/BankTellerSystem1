namespace BankSystem.Shared.DTOs.Responses
{
    /// <summary>
    /// Мөнгө шилжүүлэх хариу — сервераас TellerApp-д ирнэ.
    /// </summary>
    public class TransferResponse
    {
        /// <summary>Гүйлгээ амжилттай болсон эсэх</summary>
        public bool Success { get; set; }

        /// <summary>Хэрэглэгчид харуулах мэдэгдэл</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Гүйлгээний бүртгэлийн ID — аудитад хэрэгтэй</summary>
        public Guid? TransferId { get; set; }
    }

    /// <summary>
    /// Дараагийн үйлчлүүлэгч дуудах хариу.
    /// </summary>
    public class CallNextResponse
    {
        /// <summary>Дуудагдсан тасалбарын дугаар</summary>
        public int TicketNumber { get; set; }

        /// <summary>Теллерийн цонхны дугаар</summary>
        public int TellerWindowId { get; set; }

        /// <summary>Дарааллын үлдсэн хүний тоо</summary>
        public int RemainingCount { get; set; }
    }

    /// <summary>
    /// Шинэ тасалбар олгох хариу — NumberTerminal хүлээн авна.
    /// </summary>
    public class IssueTicketResponse
    {
        /// <summary>Олгогдсон тасалбарын дугаар</summary>
        public int TicketNumber { get; set; }

        /// <summary>Тасалбар олгогдсон цаг</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>Дарааллын нийт хүний тоо</summary>
        public int QueueCount { get; set; }
    }
}
