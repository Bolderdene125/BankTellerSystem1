using BankServer.Business;
using BankServer.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// Business давхаргын service-ууд
builder.Services.AddSingleton<TicketQueueService>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<ExchangeRateService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Local: localhost:5000 | Network: 0.0.0.0:5000
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