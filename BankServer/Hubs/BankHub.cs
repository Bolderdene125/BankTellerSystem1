using Microsoft.AspNetCore.SignalR;

namespace BankServer.Hubs;

/// <summary>
/// SignalR Hub — бүх клиентүүдийг холбоно.
/// TellerApp, CurrencyDisplay, SocketServer энд холбогдоно.
/// </summary>
public class BankHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] Холбогдлоо: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] Саллаа: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Теллер дугаар дуудахад энэ метод дуудагдана.
    /// Дугаар болон теллерийн нэрийг бүх дэлгэцэнд явуулна.
    /// </summary>
    public async Task NotifyTellerCall(int number, string tellerName)
    {
        await Clients.All.SendAsync("ReceiveTellerCall", number, tellerName);
    }
}