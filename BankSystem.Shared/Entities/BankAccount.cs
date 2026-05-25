namespace BankSystem.Shared.Entities
{
    /// <summary>
    /// Харилцагчийн банкны данс
    /// </summary>
    public class BankAccount
    {
        /// <summary>Өгөгдлийн сангийн өвөрмөц дугаар</summary>
        public int Id { get; set; }

        /// <summary>Дансны дугаар (жишээ: ACC001)</summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>Дансны эзэмшигчийн нэр</summary>
        public string OwnerName { get; set; } = string.Empty;

        /// <summary>Дансны валют (үндсэн утга: MNT)</summary>
        public string Currency { get; set; } = "MNT";

        /// <summary>Дансны үлдэгдэл</summary>
        public decimal Balance { get; set; }

        /// <summary>Данс үүссэн огноо цаг</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Данс идэвхтэй эсэх</summary>
        public bool IsActive { get; set; } = true;
    }
}
