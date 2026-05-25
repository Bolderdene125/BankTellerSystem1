namespace BankServer.Domain.Models;

/// <summary>Дугаарын тасалбар. record учир олгосны дараа өөрчлөгдөхгүй.</summary>
public record QueueTicket(int Number, DateTime IssuedAt, string ServiceType);