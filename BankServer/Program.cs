using BankServer.Business;
using BankServer.Hubs;
using BankSystem.Shared.Interfaces;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// ── Singleton services — серверийн турш нэг instance ────────────────────
// AccountService болон ExchangeRateService in-memory тул Singleton
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<IBankAccountRepository>(
    sp => sp.GetRequiredService<AccountService>());

builder.Services.AddSingleton<ExchangeRateService>();
builder.Services.AddSingleton<ICurrencyRateRepository>(
    sp => sp.GetRequiredService<ExchangeRateService>());

builder.Services.AddSingleton<TicketQueueService>();

// ── CORS — TellerApp, Blazor бүгд хандаж чадна ──────────────────────────
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Сүлжээнд хүртээмжтэй байхын тулд 0.0.0.0 ашиглана
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.MapControllers();
app.MapHub<BankHub>("/bankhub");

app.Run();