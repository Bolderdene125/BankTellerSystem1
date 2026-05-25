using BankSystem.Shared.Entities;

namespace BankSystem.Shared.Interfaces
{
    /// <summary>
    /// Валютын ханш удирдах repository-н гэрээ
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
