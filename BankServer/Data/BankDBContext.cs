using BankSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankServer.Data;

/// <summary>
/// SQLite database context.
/// BankSystem.Shared.Entities ашиглана.
/// </summary>
public class BankDbContext(DbContextOptions<BankDbContext> options)
    : DbContext(options)
{
    public DbSet<BankAccount> Accounts { get; set; }
    public DbSet<currencyRate> CurrencyRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Teammate-ийн бүтэц: нэг данс = нэг валют
        // ACC001-MNT, ACC001-USD гэж тусдаа данс
        modelBuilder.Entity<BankAccount>().HasData(
            new BankAccount { Id = 1, AccountNumber = "ACC001-MNT", OwnerName = "Болд", Currency = "MNT", Balance = 1_000_000, CreatedAt = seedDate, IsActive = true },
            new BankAccount { Id = 2, AccountNumber = "ACC001-USD", OwnerName = "Болд", Currency = "USD", Balance = 100, CreatedAt = seedDate, IsActive = true },
            new BankAccount { Id = 3, AccountNumber = "ACC002-MNT", OwnerName = "Сарнай", Currency = "MNT", Balance = 500_000, CreatedAt = seedDate, IsActive = true },
            new BankAccount { Id = 4, AccountNumber = "ACC002-USD", OwnerName = "Сарнай", Currency = "USD", Balance = 50, CreatedAt = seedDate, IsActive = true },
            new BankAccount { Id = 5, AccountNumber = "ACC003-MNT", OwnerName = "Ганбаяр", Currency = "MNT", Balance = 2_000_000, CreatedAt = seedDate, IsActive = true },
            new BankAccount { Id = 6, AccountNumber = "ACC003-USD", OwnerName = "Ганбаяр", Currency = "USD", Balance = 200, CreatedAt = seedDate, IsActive = true }
        );

        modelBuilder.Entity<currencyRate>().HasData(
            new currencyRate { Id = 1, CurrencyCode = "USD", CurrencyName = "Америк доллар", BuyRate = 3440, SellRate = 3460, UpdatedAt = seedDate },
            new currencyRate { Id = 2, CurrencyCode = "EUR", CurrencyName = "Евро", BuyRate = 3750, SellRate = 3780, UpdatedAt = seedDate },
            new currencyRate { Id = 3, CurrencyCode = "CNY", CurrencyName = "Хятад юань", BuyRate = 475, SellRate = 480, UpdatedAt = seedDate },
            new currencyRate { Id = 4, CurrencyCode = "RUB", CurrencyName = "Оросын рубль", BuyRate = 38, SellRate = 40, UpdatedAt = seedDate }
        );
    }
}