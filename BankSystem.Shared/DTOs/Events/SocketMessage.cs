namespace BankSystem.Shared.DTOs.Events
{
    /// <summary>
    /// TCP Socket-ээр дамжих мессежийн формат.
    /// SocketServer ↔ NumberDisplay хоорондын харилцаанд ашиглана.
    ///
    /// Мессежийн төрлүүд:
    ///   REGISTER   — NumberDisplay өөрийн RoomId-г бүртгэнэ
    ///   ACK        — Сервер бүртгэлийг баталгаажуулна
    ///   TELLER_CALL — Теллер дараагийн дугаар дуудав
    ///   SHOW_NUMBER — NumberTerminal шинэ дугаар авав
    /// </summary>
    public class SocketMessage
    {
        /// <summary>Мессежийн төрөл</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Агуулга — JSON string хэлбэрээр.
        /// Төрлөөс хамааран өөр өөр өгөгдөл агуулна.
        /// </summary>
        public string? Payload { get; set; }
    }
}
