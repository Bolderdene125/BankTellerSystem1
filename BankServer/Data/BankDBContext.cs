using BankSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankServer.Data;

/// <summary>
/// SQLite өгөгдлийн сан.
/// Shared.Entities загваруудыг хүснэгт болгоно.
///
/// ЗАСВАР: TransferRecord хүснэгт нэмэгдсэн — гүйлгээний бүртгэл.
/// ЗАСВАР: currencyRate → CurrencyRate (PascalCase).
///
/// EnsureCreated() — migration ашиглахгүй үед seed өгөгдөлтэй DB үүсгэнэ.
/// bank.db файл нь ажлын директорид үүснэ.
/// </summary>
public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    /// <summary>Дансны хүснэгт</summary>
    public DbSet<BankAccount> Accounts { get; set; }

    /// <summary>Валютын ханшийн хүснэгт</summary>
    public DbSet<CurrencyRate> CurrencyRates { get; set; }

    /// <summary>
    /// Гүйлгээний бүртгэлийн хүснэгт.
    /// Аудитын зорилгоор бүх гүйлгээ хадгалагдана.
    /// </summary>
    public DbSet<TransferRecord> TransferRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seed = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Данс seed ─────────────────────────────────────────────────────
        modelBuilder.Entity<BankAccount>().HasData(
            new BankAccount { Id = 1, AccountNumber = "ACC001", OwnerName = "Болд",    Currency = "MNT", Balance = 1_000_000, CreatedAt = seed, IsActive = true },
            new BankAccount { Id = 2, AccountNumber = "ACC002", OwnerName = "Сарнай",  Currency = "MNT", Balance =   500_000, CreatedAt = seed, IsActive = true },
            new BankAccount { Id = 3, AccountNumber = "ACC003", OwnerName = "Ганбаяр", Currency = "MNT", Balance = 2_000_000, CreatedAt = seed, IsActive = true }
        );

        // ── Ханш seed ─────────────────────────────────────────────────────
        modelBuilder.Entity<CurrencyRate>().HasData(
            new CurrencyRate { Id = 1, CurrencyCode = "USD", CurrencyName = "Америк доллар", BuyRate = 3440, SellRate = 3460, UpdatedAt = seed, UpdatedBy = "system" },
            new CurrencyRate { Id = 2, CurrencyCode = "EUR", CurrencyName = "Евро",          BuyRate = 3750, SellRate = 3780, UpdatedAt = seed, UpdatedBy = "system" },
            new CurrencyRate { Id = 3, CurrencyCode = "CNY", CurrencyName = "Хятад юань",    BuyRate =  475, SellRate =  480, UpdatedAt = seed, UpdatedBy = "system" },
            new CurrencyRate { Id = 4, CurrencyCode = "RUB", CurrencyName = "Оросын рубль",  BuyRate =   38, SellRate =   40, UpdatedAt = seed, UpdatedBy = "system" }
        );

        // TransferRecord — Guid primary key тохиргоо
        modelBuilder.Entity<TransferRecord>()
            .Property(t => t.Id)
            .ValueGeneratedOnAdd();
    }
}
