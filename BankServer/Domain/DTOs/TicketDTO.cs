namespace BankServer.Domain.DTOs;

/// <summary>
/// Тасалбар олгох хүсэлт — NumberTerminal-аас POST /api/ticket/issue-д ирнэ.
/// Shared-д байхгүй тул энд тодорхойлно.
/// </summary>
public record IssueTicketRequestDto(string ServiceType);

/// <summary>
/// Дарааллын одоогийн байдлын хариу — GET /api/ticket/status-д буцаана.
/// Shared-д байхгүй тул энд тодорхойлно.
/// </summary>
public record QueueStatusDto(int CurrentNumber, int QueueCount);

/// <summary>
/// Дансны мэдээллийн хариу — GET /api/account, GET /api/account/{id}-д буцаана.
/// Shared-д байхгүй тул энд тодорхойлно.
/// </summary>
public record AccountResponseDto(
    string AccountNumber,
    string OwnerName,
    string Currency,
    decimal Balance,
    bool IsActive
);

/// <summary>
/// Ханшийн мэдээллийн хариу — GET /api/exchangerate,
/// PUT /api/exchangerate/{code}-д буцаана.
/// Shared-д байхгүй тул энд тодорхойлно.
/// </summary>
public record ExchangeRateResponseDto(
    string CurrencyCode,
    string CurrencyName,
    decimal BuyRate,
    decimal SellRate,
    DateTime UpdatedAt
);