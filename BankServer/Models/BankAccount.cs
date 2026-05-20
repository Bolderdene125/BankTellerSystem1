namespace BankServer.Models;

/// <summary>Банкны харилцагчийн данс. Гүйлгээний дараа MNT өөрчлөгдөх тул class.</summary>
public class BankAccount
{
    /// <summary>Дансны дугаар — системд давтагдахгүй. Жишээ: "ACC001".</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Дансны эзэмшигчийн нэр.</summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Төгрөгийн үлдэгдэл. decimal ашигласан шалтгаан:
    /// double/float нь мөнгөн тооцоонд нарийвчлалын алдаа өгдөг.
    /// </summary>
    public decimal MNT { get; set; }

    /// <summary>Долларын үлдэгдэл.</summary>
    public decimal USD { get; set; }
}