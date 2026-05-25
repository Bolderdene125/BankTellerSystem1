namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// Дугаар олгосны хариу — сервер NumberTerminal-д буцаана
    /// </summary>
    public class IssueTicketResponse
    {
        /// <summary>Олгогдсон дугаар</summary>
        public int TicketNumber { get; set; }

        /// <summary>Дугаар олгогдсон огноо цаг</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>Дараалалд хүлээж буй нийт хүний тоо</summary>
        public int QueueCount { get; set; }
    }
}
