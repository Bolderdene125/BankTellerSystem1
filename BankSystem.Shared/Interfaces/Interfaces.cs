using BankSystem.Shared.Entities;

namespace BankSystem.Shared.Interfaces
{
    /// <summary>
    /// Банкны данс удирдах repository-н гэрээ.
    /// AccountService энэ interface-г хэрэгжүүлнэ.
    /// </summary>
    public interface IBankAccountRepository
    {
        /// <summary>Дансны дугаараар данс хайх</summary>
        Task<BankAccount?> GetByAccountNumberAsync(string accountNumber);

        /// <summary>ID-аар данс хайх</summary>
        Task<BankAccount?> GetByIdAsync(int id);

        /// <summary>Дансны мэдээлэл шинэчлэх</summary>
        Task UpdateAsync(BankAccount account);
    }

    /// <summary>
    /// Валютын ханш удирдах repository-н гэрээ.
    /// ExchangeRateService энэ interface-г хэрэгжүүлнэ.
    /// ЗАСВАР: currencyRate → CurrencyRate (PascalCase)
    /// </summary>
    public interface ICurrencyRateRepository
    {
        /// <summary>Бүх валютын ханш авах</summary>
        Task<IEnumerable<CurrencyRate>> GetAllAsync();

        /// <summary>Валютын кодоор ханш хайх</summary>
        Task<CurrencyRate?> GetByCurrencyCodeAsync(string currencyCode);

        /// <summary>Валютын ханш шинэчлэх</summary>
        Task UpdateAsync(CurrencyRate rate);
    }
}
