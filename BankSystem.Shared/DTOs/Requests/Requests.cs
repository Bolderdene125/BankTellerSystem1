using System.ComponentModel.DataAnnotations;

namespace BankSystem.Shared.DTOs.Requests
{
    /// <summary>
    /// Мөнгө шилжүүлэх хүсэлт — TellerApp-аас сервер рүү явуулна.
    /// [Required], [Range] — серверт буруу өгөгдөл орохоос сэргийлнэ.
    /// </summary>
    public class TransferRequest
    {
        /// <summary>Мөнгө гарах дансны дугаар</summary>
        [Required(ErrorMessage = "Илгээгч дансны дугаар шаардлагатай")]
        public string FromAccountNumber { get; set; } = string.Empty;

        /// <summary>Мөнгө орох дансны дугаар</summary>
        [Required(ErrorMessage = "Хүлээн авагч дансны дугаар шаардлагатай")]
        public string ToAccountNumber { get; set; } = string.Empty;

        /// <summary>Шилжүүлэх дүн — 1-ээс 100,000,000₮ хүртэл</summary>
        [Range(1, 100_000_000, ErrorMessage = "Дүн 1-ээс 100,000,000₮ хооронд байх ёстой")]
        public decimal Amount { get; set; }

        /// <summary>Гүйлгээ хийсэн теллерийн цонхны дугаар</summary>
        public string TellerWindowId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Валютын ханш шинэчлэх хүсэлт — TellerApp-аас явуулна.
    /// </summary>
    public class UpdateRateRequest
    {
        /// <summary>Авах ханш</summary>
        [Range(1, 10_000_000, ErrorMessage = "Авах ханш буруу байна")]
        public decimal BuyRate { get; set; }

        /// <summary>Зарах ханш — авах ханшаас их байх ёстой</summary>
        [Range(1, 10_000_000, ErrorMessage = "Зарах ханш буруу байна")]
        public decimal SellRate { get; set; }

        /// <summary>Ханш өөрчилсөн теллерийн ID — аудитад хэрэгтэй</summary>
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
