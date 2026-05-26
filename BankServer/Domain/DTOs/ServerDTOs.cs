namespace BankServer.Domain.DTOs;

/// <summary>Тасалбар олгох хүсэлт — NumberTerminal-аас ирнэ.</summary>
public record IssueTicketRequestDto(string ServiceType);

/// <summary>Дарааллын байдлын хариу.</summary>
public record QueueStatusDto(int CurrentNumber, int QueueCount);

/// <summary>Дансны мэдээллийн хариу.</summary>
public record AccountResponseDto(
    string  AccountNumber,
    string  OwnerName,
    string  Currency,
    decimal Balance,
    bool    IsActive);

/// <summary>
/// Ханшийн мэдээллийн хариу.
/// ЗАСВАР: UpdatedBy талбар нэмэгдсэн.
/// </summary>
public record ExchangeRateResponseDto(
    string   CurrencyCode,
    string   CurrencyName,
    decimal  BuyRate,
    decimal  SellRate,
    DateTime UpdatedAt,
    string   UpdatedBy = "");
