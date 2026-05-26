using BankServer.Business;
using BankServer.Data;
using BankServer.Hubs;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ── Serilog тохируулга ────────────────────────────────────────────────────
// Console болон файлд нэгэн зэрэг бичнэ.
// logs/bank-.log файл өдөр бүр шинээр үүснэ (rollingInterval).
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/bank-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Хэрэглэгчийн Банк — Сервер эхэлж байна...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog-г ASP.NET Core-ийн logging-д холбоно
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddSignalR();

    // ── SQLite — bank.db файлд persist хийнэ ────────────────────────────
    // ЗАСВАР: DbContext бүртгэгдлээ — өмнө нь зөвхөн тодорхойлогдсон байсан
    builder.Services.AddDbContext<BankDbContext>(opt =>
        opt.UseSqlite("Data Source=bank.db"));

    // ── Singleton services ───────────────────────────────────────────────
    // AccountService болон ExchangeRateService in-memory тул Singleton.
    // Restart хийхэд in-memory дата алдагдана — SQLite-г нэмсэн.
    builder.Services.AddSingleton<AccountService>();
    builder.Services.AddSingleton<IBankAccountRepository>(
        sp => sp.GetRequiredService<AccountService>());

    builder.Services.AddSingleton<ExchangeRateService>();
    builder.Services.AddSingleton<ICurrencyRateRepository>(
        sp => sp.GetRequiredService<ExchangeRateService>());

    builder.Services.AddSingleton<TicketQueueService>();

    // ── CORS ─────────────────────────────────────────────────────────────
    builder.Services.AddCors(o =>
        o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    // Сүлжээнд хүртээмжтэй байхын тулд 0.0.0.0 ашиглана
    builder.WebHost.UseUrls("http://0.0.0.0:5000");

    var app = builder.Build();

    // ── SQLite DB үүсгэх + seed ──────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        db.Database.EnsureCreated();
        Log.Information("SQLite DB бэлэн: bank.db");
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors();
    app.MapControllers();
    app.MapHub<BankHub>("/bankhub");

    Log.Information("Сервер http://0.0.0.0:5000 дээр эхэллээ ✓");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Сервер эхлэхэд алдаа гарлаа");
}
finally
{
    Log.CloseAndFlush();
}
