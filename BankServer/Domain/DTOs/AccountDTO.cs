namespace BankServer.Domain.DTOs;

/// <summary>Гүйлгээний хүсэлт — клиентээс ирнэ.</summary>
public record TransferRequestDto(
    string FromAccount,
    string ToAccount,
    decimal Amount
);

/// <summary>Гүйлгээний хариу — клиентэд явна.</summary>
public record TransferResponseDto(bool Success, string Message);

/// <summary>Дансны мэдээлэл — клиентэд явна. Нууц талбар агуулахгүй.</summary>
public record AccountResponseDto(
    string AccountNumber,
    string OwnerName,
    decimal MNT,
    decimal USD
);