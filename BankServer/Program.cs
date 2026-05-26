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
    builder.Services.AddDbContext<BankDbContext>(opt =>
        opt.UseSqlite("Data Source=bank.db"));

    // ── AccountService: Scoped — BankDbContext inject хийдэг тул ────────
    //
    // ӨМНӨ (буруу):
    //   builder.Services.AddSingleton<AccountService>();
    //
    // ДАРАА (зөв):
    //   builder.Services.AddScoped<AccountService>();
    //
    // Яагаад:
    //   BankDbContext нь Scoped lifetime-тай (HTTP request бүрт шинэ instance).
    //   Singleton сервис дотор Scoped inject хийхийг ASP.NET Core хориглодог.
    //   Хориглохгүй бол runtime-д "Cannot consume scoped service from singleton"
    //   алдаа гарна.
    //
    // Scoped болсноор HTTP request бүрт шинэ AccountService үүснэ.
    // In-memory state (Dictionary, List) байхгүй болсон тул энэ нь зөв.
    builder.Services.AddScoped<AccountService>();
    builder.Services.AddScoped<IBankAccountRepository>(
        sp => sp.GetRequiredService<AccountService>());

    // ── ExchangeRateService, TicketQueueService: Singleton хэвээр ───────
    // Эдгээр нь DbContext ашигладаггүй, in-memory ConcurrentDictionary,
    // Channel<T> ашигладаг тул Singleton байсаар байна.
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
    // EnsureCreated: bank.db байхгүй бол үүсгэж seed өгөгдлийг оруулна.
    // bank.db аль хэдийн байвал юу ч хийхгүй — өгөгдөл хадгалагдсан хэвээр.
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