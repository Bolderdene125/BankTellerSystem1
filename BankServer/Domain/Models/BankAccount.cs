namespace BankServer.Domain.Models;

/// <summary>
/// Теллерийн апп болон тестэд ашиглагдах данс.
/// Shared.Entities.BankAccount-аас ялгаатай нь MNT, USD талбартай —
/// нэг харилцагчид хоёр валют хамт харагдана.
/// </summary>
public class BankAccount
{
    /// <summary>Дансны дугаар (жишээ: ACC001)</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Эзэмшигчийн нэр</summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>Төгрөгийн үлдэгдэл</summary>
    public decimal MNT { get; set; }

    /// <summary>Долларын үлдэгдэл</summary>
    public decimal USD { get; set; }
}
