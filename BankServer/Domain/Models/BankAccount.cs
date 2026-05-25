namespace BankServer.Domain.Models;

/// <summary>Банкны харилцагчийн данс.</summary>
public class BankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Төгрөгийн үлдэгдэл. decimal ашигласан шалтгаан:
    /// double/float нь мөнгөн тооцоонд нарийвчлалын алдаа өгдөг.
    /// </summary>
    public decimal MNT { get; set; }
    public decimal USD { get; set; }
}