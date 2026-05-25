using BankServer.Business;
using BankServer.Data;
using BankServer.Hubs;
using BankSystem.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlite("Data Source=bank.db"));

builder.Services.AddSingleton<TicketQueueService>();

// Interface-ээр бүртгэнэ — Clean Architecture
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<IBankAccountRepository, AccountService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<ICurrencyRateRepository, ExchangeRateService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.MapControllers();
app.MapHub<BankHub>("/bankhub");

app.Run();