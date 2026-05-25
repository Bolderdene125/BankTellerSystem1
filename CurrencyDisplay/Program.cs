using CurrencyDisplay.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// appsettings.json-оос URL авна
var bankServerUrl = builder.Configuration["BankServerUrl"] ?? "http://localhost:5000";

// HttpClient inject
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(bankServerUrl)
});

// BankServerUrl-г inject хийхийн тулд wrapper класс ашиглана
builder.Services.AddSingleton(new BankServerConfig(bankServerUrl));

// Бүх network interface-д сонсоно — өөр компьютераас хандаж болно
builder.WebHost.UseUrls("http://0.0.0.0:5186");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>BankServer URL-г Blazor component-д inject хийх wrapper.</summary>
public class BankServerConfig(string url)
{
    public string Url { get; } = url;
}