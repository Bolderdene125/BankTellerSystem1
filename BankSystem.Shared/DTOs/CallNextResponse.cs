namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// Дараагийн харилцагчийг дуудсаны хариу — сервер TellerApp-д буцаана,
    /// мөн Socket-ээр дэлгэцүүдэд дамжуулна
    /// </summary>
    public class CallNextResponse
    {
        /// <summary>Дуудагдсан дугаар</summary>
        public int TicketNumber { get; set; }

        /// <summary>Дуудсан теллерийн цонхны ID</summary>
        public int TellerWindowId { get; set; }

        /// <summary>Дараалалд үлдсэн хүний тоо</summary>
        public int RemainingCount { get; set; }
    }
}
