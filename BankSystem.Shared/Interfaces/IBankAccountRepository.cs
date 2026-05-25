using BankSystem.Shared.Entities;

namespace BankSystem.Shared.Interfaces
{
    /// <summary>
    /// Банкны данс удирдах repository-н гэрээ
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
}
