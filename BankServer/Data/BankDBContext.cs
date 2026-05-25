using BankSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankServer.Data;

/// <summary>
/// SQLite database context.
/// Shared.Entities.BankAccount болон Shared.Entities.currencyRate ашиглана.
/// EnsureCreated() — Migration ажиллахгүй тохиолдолд seed өгөгдөлтэй DB үүсгэнэ.
/// </summary>
public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    /// <summary>Дансны хүснэгт — Shared.Entities.BankAccount entity.</summary>
    public DbSet<BankAccount> Accounts { get; set; }

    /// <summary>Ханшийн хүснэгт — Shared.Entities.currencyRate entity.</summary>
    public DbSet<currencyRate> CurrencyRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seed = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Seed данс ────────────────────────────────────────────────────
        // Нэг харилцагч = хоёр данс: MNT болон USD
        modelBuilder.Entity<BankAccount>().HasData(
            new BankAccount { Id = 1, AccountNumber = "ACC001", OwnerName = "Болд",    Currency = "MNT", Balance = 1_000_000, CreatedAt = seed, IsActive = true },
            new BankAccount { Id = 2, AccountNumber = "ACC002", OwnerName = "Сарнай",  Currency = "MNT", Balance =   500_000, CreatedAt = seed, IsActive = true },
            new BankAccount { Id = 3, AccountNumber = "ACC003", OwnerName = "Ганбаяр", Currency = "MNT", Balance = 2_000_000, CreatedAt = seed, IsActive = true }
        );

        // ── Seed ханш ────────────────────────────────────────────────────
        modelBuilder.Entity<currencyRate>().HasData(
            new currencyRate { Id = 1, CurrencyCode = "USD", CurrencyName = "Америк доллар", BuyRate = 3440, SellRate = 3460, UpdatedAt = seed },
            new currencyRate { Id = 2, CurrencyCode = "EUR", CurrencyName = "Евро",          BuyRate = 3750, SellRate = 3780, UpdatedAt = seed },
            new currencyRate { Id = 3, CurrencyCode = "CNY", CurrencyName = "Хятад юань",    BuyRate =  475, SellRate =  480, UpdatedAt = seed },
            new currencyRate { Id = 4, CurrencyCode = "RUB", CurrencyName = "Оросын рубль",  BuyRate =   38, SellRate =   40, UpdatedAt = seed }
        );
    }
}
