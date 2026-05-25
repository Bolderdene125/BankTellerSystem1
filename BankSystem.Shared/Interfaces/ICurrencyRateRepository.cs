using BankSystem.Shared.Entities;

namespace BankSystem.Shared.Interfaces
{
    /// <summary>
    /// Валютын ханш удирдах repository-н гэрээ
    /// </summary>
    public interface ICurrencyRateRepository
    {
        /// <summary>Бүх валютын ханш авах</summary>
        Task<IEnumerable<currencyRate>> GetAllAsync();

        /// <summary>Валютын кодоор ханш хайх</summary>
        Task<currencyRate?> GetByCurrencyCodeAsync(string currencyCode);

        /// <summary>Валютын ханш шинэчлэх</summary>
        Task UpdateAsync(currencyRate rate);
    }
}
