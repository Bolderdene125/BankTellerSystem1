namespace BankServer.Domain.DTOs;

/// <summary>Тасалбар олгох хүсэлт — клиентээс ирнэ.</summary>
public record IssueTicketRequestDto(string ServiceType);

/// <summary>Тасалбар олгосны хариу — клиентэд явна.</summary>
public record IssueTicketResponseDto(
    int Number,
    DateTime IssuedAt,
    string ServiceType,
    int QueueCount
);

/// <summary>Дараагийн дугаар дуудсны хариу.</summary>
public record CallNextResponseDto(int CalledNumber);

/// <summary>Дарааллын байдлын хариу.</summary>
public record QueueStatusDto(int CurrentNumber, int QueueCount);