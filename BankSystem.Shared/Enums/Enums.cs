namespace BankSystem.Shared.Enums
{
    /// <summary>
    /// Дугаарын тасалбарын төлөв.
    /// TicketQueueService дотор ашиглагдана.
    /// </summary>
    public enum TicketStatus
    {
        /// <summary>Хүлээж байна</summary>
        Waiting,
        /// <summary>Теллер дуудсан</summary>
        Called,
        /// <summary>Үйлчилгээ явагдаж байна</summary>
        Serving,
        /// <summary>Үйлчилгээ дууссан</summary>
        Done,
        /// <summary>Алгасагдсан</summary>
        Skipped
    }
}
