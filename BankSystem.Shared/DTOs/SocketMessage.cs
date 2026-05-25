namespace BankSystem.Shared.DTOs
{
    /// <summary>
    /// TCP Socket-ээр дамжих мессежийн формат —
    /// сервер болон NumberTerminal хоёрын хооронд ашиглана
    /// </summary>
    public class SocketMessage
    {
        /// <summary>Мессежийн төрөл (жишээ: "SHOW_NUMBER", "PING", "ACK")</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Агуулга — JSON string хэлбэрээр (төрлөөс хамааран өөр өөр өгөгдөл агуулна)</summary>
        public string? Payload { get; set; }
    }
}
