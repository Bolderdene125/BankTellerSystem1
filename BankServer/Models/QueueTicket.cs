namespace BankServer.Models;

/// <summary>Дугаарын тасалбар. record учир олгосны дараа өөрчлөгдөхгүй.</summary>
/// <param name="Number">Дарааллын дугаар — 1-ээс эхэлнэ.</param>
/// <param name="IssuedAt">Авсан цаг.</param>
/// <param name="ServiceType">Үйлчилгээний төрөл: "Гүйлгээ", "Лавлагаа" гэх мэт.</param>
public record QueueTicket(int Number, DateTime IssuedAt, string ServiceType);