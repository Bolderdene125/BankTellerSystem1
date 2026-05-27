using BankServer.Business;
using BankServer.Data;
using BankSystem.Shared.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BankTests;

// AccountService тестүүд
//
// Яагаад SQLite :memory: ашигласан:
//   EF Core-ийн UseInMemoryDatabase() нь транзакцийг ДЭМЖДЭГГҮЙ.
//   AccountService дотор BeginTransactionAsync() дуудагддаг тул
//   UseInMemoryDatabase()-тай тест бүр "TransactionIgnoredWarning" алдаа өгнө.
//
//   Шийдэл: Microsoft.Data.Sqlite-ийн "Data Source=:memory:" ашиглана.
//   SQLite :memory: нь транзакцийг бүрэн дэмждэг — жинхэнэ DB-тэй адил.
//   SqliteConnection-г тест туршид амьд байлгаж, дуусмагц Close() хийнэ.

public class AccountServiceTests : IDisposable
{
    // Тест бүрт нэг SqliteConnection — :memory: DB холболт хаагдахад устдаг
    private readonly SqliteConnection _connection;
    private readonly BankDbContext _db;

    public AccountServiceTests()
    {
        // SqliteConnection-г эхлүүлж нээнэ — хаагдаагүй тул :memory: DB хадгалагдана
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new BankDbContext(options);
        _db.Database.EnsureCreated();

        // Seed өгөгдөл оруулна — ACC001, ACC002, ACC003
        if (!_db.Accounts.Any())
        {
            _db.Accounts.AddRange(
                new BankAccount { Id = 1, AccountNumber = "ACC001", OwnerName = "Болд", Currency = "MNT", Balance = 1_000_000, CreatedAt = DateTime.UtcNow, IsActive = true },
                new BankAccount { Id = 2, AccountNumber = "ACC002", OwnerName = "Сарнай", Currency = "MNT", Balance = 500_000, CreatedAt = DateTime.UtcNow, IsActive = true },
                new BankAccount { Id = 3, AccountNumber = "ACC003", OwnerName = "Ганбаяр", Currency = "MNT", Balance = 2_000_000, CreatedAt = DateTime.UtcNow, IsActive = true }
            );
            _db.SaveChanges();
        }
    }

    // xUnit тест бүр дуусмагц Dispose() дуудна — connection хаагдаж :memory: DB устна
    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    private AccountService NewService() =>
        new(_db, NullLogger<AccountService>.Instance);

    /// <summary>Зөв мэдээллээр гүйлгээ амжилттай болох ёстой.</summary>
    [Fact]
    public async Task Transfer_ValidAccounts_Succeeds()
    {
        var (success, msg, id) = await NewService().TransferAsync("ACC001", "ACC002", 100_000);

        Assert.True(success);
        Assert.Contains("амжилттай", msg);
        Assert.NotNull(id);
    }

    /// <summary>Үлдэгдэл хүрэлцэхгүй бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_InsufficientBalance_Fails()
    {
        var (success, msg, _) = await NewService().TransferAsync("ACC001", "ACC002", 999_999_999);

        Assert.False(success);
        Assert.Contains("хүрэлцэхгүй", msg);
    }

    /// <summary>Байхгүй данс ашиглахад алдаа гарах ёстой.</summary>
    [Fact]
    public async Task Transfer_InvalidAccount_Fails()
    {
        var (success, _, _) = await NewService().TransferAsync("INVALID", "ACC001", 1000);

        Assert.False(success);
    }

    /// <summary>Дүн 0 бол амжилтгүй байх ёстой.</summary>
    [Fact]
    public async Task Transfer_ZeroAmount_Fails()
    {
        var (success, msg, _) = await NewService().TransferAsync("ACC001", "ACC002", 0);

        Assert.False(success);
        Assert.Contains("0-ээс их", msg);
    }

    /// <summary>Ижил данс хооронд гүйлгээ хийх оролдлого.</summary>
    [Fact]
    public async Task Transfer_SameAccount_Fails()
    {
        var (success, msg, _) = await NewService().TransferAsync("ACC001", "ACC001", 1000);

        Assert.False(success);
        Assert.Contains("ижил", msg);
    }

    /// <summary>Гүйлгээний дараа баланс DB-д зөв өөрчлөгдсөн эсэхийг шалгана.</summary>
    [Fact]
    public async Task Transfer_BalanceUpdatesCorrectly()
    {
        var svc = NewService();
        var fromBefore = _db.Accounts.First(a => a.AccountNumber == "ACC001").Balance;
        var toBefore = _db.Accounts.First(a => a.AccountNumber == "ACC002").Balance;

        await svc.TransferAsync("ACC001", "ACC002", 100_000);

        // EF Core change tracking — дахин уншихгүйгээр шинэчлэгдсэн утгыг авна
        Assert.Equal(fromBefore - 100_000, _db.Accounts.First(a => a.AccountNumber == "ACC001").Balance);
        Assert.Equal(toBefore + 100_000, _db.Accounts.First(a => a.AccountNumber == "ACC002").Balance);
    }

    /// <summary>
    /// Амжилттай гүйлгээний дараа TransferRecord DB-д хадгалагдах ёстой.
    /// DB transaction ашигладаг тул гүйлгээ болон бүртгэл нэгэн зэрэг хадгалагдана.
    /// </summary>
    [Fact]
    public async Task Transfer_Success_RecordSaved()
    {
        await NewService().TransferAsync("ACC001", "ACC002", 50_000, "Цонх305");

        var history = _db.TransferRecords.ToList();
        Assert.Single(history);
        Assert.Equal("ACC001", history[0].FromAccount);
        Assert.Equal("ACC002", history[0].ToAccount);
        Assert.Equal(50_000m, history[0].Amount);
        Assert.Equal("Цонх305", history[0].TellerId);
        Assert.Equal("Completed", history[0].Status);
    }

    /// <summary>Үлдэгдэл хүрэлцэхгүй validation алдаа — DB-д record хадгалагдахгүй.</summary>
    [Fact]
    public async Task Transfer_Failure_NoRecordForValidationError()
    {
        await NewService().TransferAsync("ACC001", "ACC002", 999_999_999, "Цонх305");

        Assert.Empty(_db.TransferRecords.ToList());
    }

    /// <summary>Данс олдвол мэдээлэл буцаана.</summary>
    [Fact]
    public void GetAccount_ExistingAccount_ReturnsAccount()
    {
        var account = NewService().GetAccount("ACC001");
        Assert.NotNull(account);
        Assert.Equal("ACC001", account.AccountNumber);
    }

    /// <summary>Байхгүй данс хайхад null буцаана.</summary>
    [Fact]
    public void GetAccount_NonExistent_ReturnsNull()
    {
        Assert.Null(NewService().GetAccount("INVALID"));
    }
}

// ══════════════════════════════════════════════════════════════════════════
// TicketQueueService тестүүд — өөрчлөгдөөгүй
// ══════════════════════════════════════════════════════════════════════════

public class TicketQueueServiceTests
{
    private static TicketQueueService NewService() =>
        new(NullLogger<TicketQueueService>.Instance);

    [Fact]
    public async Task IssueTicket_FirstTicket_NumberIsOne()
    {
        var ticket = await NewService().IssueTicketAsync("Гүйлгээ");
        Assert.Equal(1, ticket.Number);
    }

    [Fact]
    public async Task IssueTicket_Sequential_NumbersIncrement()
    {
        var svc = NewService();
        var t1 = await svc.IssueTicketAsync("Гүйлгээ");
        var t2 = await svc.IssueTicketAsync("Гүйлгээ");
        var t3 = await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(1, t1.Number);
        Assert.Equal(2, t2.Number);
        Assert.Equal(3, t3.Number);
    }

    /// <summary>
    /// 50 хүсэлт нэгэн зэрэг ирэхэд дугаар давтагдаагүйг шалгана.
    /// Interlocked.Increment-ийн thread-safety шалгах тест.
    /// </summary>
    [Fact]
    public async Task IssueTicket_Concurrent_NoDuplicates()
    {
        var svc = NewService();
        var tasks = Enumerable.Range(0, 50).Select(_ => svc.IssueTicketAsync("Гүйлгээ"));
        var tickets = await Task.WhenAll(tasks);

        Assert.Equal(50, tickets.Select(t => t.Number).Distinct().Count());
    }

    /// <summary>Дуудалт FIFO дарааллаар явагдаж байгааг шалгана.</summary>
    [Fact]
    public async Task CallNext_ReturnsTicketsInFifoOrder()
    {
        var svc = NewService();
        await svc.IssueTicketAsync("Гүйлгээ");
        await svc.IssueTicketAsync("Гүйлгээ");
        await svc.IssueTicketAsync("Гүйлгээ");

        Assert.Equal(1, await svc.CallNextAsync());
        Assert.Equal(2, await svc.CallNextAsync());
        Assert.Equal(3, await svc.CallNextAsync());
    }

    /// <summary>
    /// Хоёр теллер нэгэн зэрэг CallNext дарахад нэг дугаар хоёрт очихгүй.
    /// Channel.TryRead атомик үйлдэл — race condition байхгүй.
    /// </summary>
    [Fact]
    public async Task CallNext_Concurrent_NoDuplicateNumbers()
    {
        var svc = NewService();
        for (int i = 0; i < 10; i++)
            await svc.IssueTicketAsync("Гүйлгээ");

        var results = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => svc.CallNextAsync()));

        Assert.Equal(10, results.Distinct().Count());
    }

    [Fact]
    public async Task CallNext_EmptyQueue_ReturnsZero()
    {
        Assert.Equal(0, await NewService().CallNextAsync());
    }
}

// ══════════════════════════════════════════════════════════════════════════
// ExchangeRateService тестүүд — өөрчлөгдөөгүй
// ══════════════════════════════════════════════════════════════════════════

public class ExchangeRateServiceTests
{
    private static ExchangeRateService NewService() =>
        new(NullLogger<ExchangeRateService>.Instance, new NullScopeFactory());

    [Fact]
    public void GetAll_ReturnsInitialRates()
    {
        var rates = NewService().GetAll().ToList();
        Assert.NotEmpty(rates);
        Assert.Contains(rates, r => r.Currency == "USD");
    }

    [Fact]
    public void GetAll_ReturnsFourCurrencies()
    {
        Assert.Equal(4, NewService().GetAll().Count());
    }

    [Fact]
    public void Get_ExistingCurrency_ReturnsRate()
    {
        var rate = NewService().Get("USD");
        Assert.NotNull(rate);
        Assert.Equal("USD", rate.Currency);
    }

    [Fact]
    public void Get_NonExistentCurrency_ReturnsNull()
    {
        Assert.Null(NewService().Get("XYZ"));
    }

    [Fact]
    public void Update_ValidCurrency_UpdatesRate()
    {
        var svc = NewService();
        svc.Update("USD", 3500, 3520);

        Assert.Equal(3500, svc.Get("USD")!.BuyRate);
        Assert.Equal(3520, svc.Get("USD")!.SellRate);
    }

    [Fact]
    public void Update_InvalidCurrency_ReturnsFalse()
    {
        Assert.False(NewService().Update("XYZ", 100, 110));
    }

    [Fact]
    public void Update_UpdatedBy_IsStored()
    {
        var svc = NewService();
        svc.Update("USD", 3500, 3520, "Цонх305");

        Assert.Equal("Цонх305", svc.Get("USD")!.UpdatedBy);
    }

    [Fact]
    public void Update_HistoryRecorded()
    {
        var svc = NewService();
        var oldBuy = svc.Get("USD")!.BuyRate;
        svc.Update("USD", 3500, 3520, "Цонх305");

        var history = svc.GetHistory();
        Assert.Single(history);
        Assert.Equal("USD", history[0].Currency);
        Assert.Equal(oldBuy, history[0].OldBuy);
        Assert.Equal(3500, history[0].NewBuy);
        Assert.Equal("Цонх305", history[0].ChangedBy);
    }

    [Fact]
    public void GetAll_SellRateHigherThanBuyRate()
    {
        foreach (var rate in NewService().GetAll())
            Assert.True(rate.SellRate > rate.BuyRate,
                $"{rate.Currency}: SellRate > BuyRate байх ёстой");
    }
}

// ══════════════════════════════════════════════════════════════════════════
// NullScopeFactory — тестэд ExchangeRateService-г DB-гүй үүсгэхэд хэрэгтэй
//
// ExchangeRateService constructor дотор LoadFromDatabase() дуудагддаг.
// LoadFromDatabase() нь _scopeFactory.CreateScope() дуудна.
// Тестэд DB байхгүй тул NullScopeFactory нь юу ч хийхгүй scope буцаана.
// Ингэснээр ExchangeRateService default утгаараа (USD, EUR, CNY, RUB) эхэлнэ.
// ══════════════════════════════════════════════════════════════════════════

public class NullScopeFactory : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new NullServiceScope();
}

public class NullServiceScope : IServiceScope
{
    public IServiceProvider ServiceProvider => new NullServiceProvider();
    public void Dispose() { }
}

public class NullServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}