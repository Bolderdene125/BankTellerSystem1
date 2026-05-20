using BankServer.Hubs;
using BankServer.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// .NET 10-д Swashbuckle байхгүй — native OpenAPI ашиглана
builder.Services.AddOpenApi();

// SignalR — realtime мэдэгдэлд хэрэгтэй
builder.Services.AddSignalR();

// Singleton: програм дуустал нэг л instance амьдарна, state хадгалагдана
builder.Services.AddSingleton<TicketQueueService>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<ExchangeRateService>();

// CORS: WinForm, Blazor өөр port-оос хандахыг зөвшөөрнө
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // .NET 10-д native OpenAPI endpoint
    app.MapOpenApi();

    // Swagger UI-г Scalar орлоно — http://localhost:5000/scalar/v1
    app.MapScalarApiReference();
}

// UseCors заавал MapControllers-с өмнө байх ёстой
app.UseCors();
app.MapControllers();
app.MapHub<BankHub>("/bankhub"); // SignalR WebSocket endpoint

app.Run();