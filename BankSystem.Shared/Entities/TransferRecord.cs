namespace BankSystem.Shared.Entities
{
    /// <summary>
    /// Гүйлгээний түүхийн бүртгэл.
    /// Банкны аудитын шаардлагаар бүх гүйлгээ хадгалагдана.
    /// SQLite DbContext-ээр persist хийгдэнэ.
    /// </summary>
    public class TransferRecord
    {
        /// <summary>Өвөрмөц дугаар — автоматаар үүснэ</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Мөнгө гарсан дансны дугаар (жишээ: ACC001)</summary>
        public string FromAccount { get; set; } = string.Empty;

        /// <summary>Мөнгө орсон дансны дугаар (жишээ: ACC002)</summary>
        public string ToAccount { get; set; } = string.Empty;

        /// <summary>Шилжүүлсэн дүн (MNT)</summary>
        public decimal Amount { get; set; }

        /// <summary>Гүйлгээ хийгдсэн огноо, цаг</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Гүйлгээ хийсэн теллерийн цонхны дугаар.
        /// Хариуцлагын бүртгэлд ашиглагдана.
        /// </summary>
        public string TellerId { get; set; } = string.Empty;

        /// <summary>
        /// Гүйлгээний төлөв.
        /// "Completed" = амжилттай, "Failed" = амжилтгүй
        /// </summary>
        public string Status { get; set; } = "Completed";

        /// <summary>Алдааны мэдэгдэл — Status = "Failed" үед дүүргэгдэнэ</summary>
        public string? ErrorMessage { get; set; }
    }
}
