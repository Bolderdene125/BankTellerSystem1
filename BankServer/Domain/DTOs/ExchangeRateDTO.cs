namespace BankServer.Domain.DTOs;

/// <summary>Ханш шинэчлэх хүсэлт — теллерээс ирнэ.</summary>
public record RateUpdateRequestDto(decimal BuyRate, decimal SellRate);

/// <summary>Ханшийн мэдээлэл — клиентэд явна.</summary>
public record ExchangeRateResponseDto(
    string Currency,
    decimal BuyRate,
    decimal SellRate,
    DateTime UpdatedAt
);