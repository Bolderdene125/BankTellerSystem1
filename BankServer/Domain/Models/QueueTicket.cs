using BankSystem.Shared.Enums;

namespace BankServer.Domain.Models;

/// <summary>
/// TicketQueueService-д ашиглагдах дарааллын тасалбар.
/// Shared.Enums.TicketStatus enum ашиглана.
/// Number, IssuedAt, ServiceType — тест гэрээ.
/// </summary>
public record QueueTicket(int Number, DateTime IssuedAt, string ServiceType)
{
    /// <summary>
    /// Тасалбарын одоогийн төлөв.
    /// Shared.Enums.TicketStatus ашиглана:
    ///   Waiting → Called → Serving → Done
    /// </summary>
    public TicketStatus Status { get; init; } = TicketStatus.Waiting;
}
