using Microsoft.AspNetCore.SignalR;

namespace BankServer.Hubs;

/// <summary>
/// Клиентүүд энэ Hub-д WebSocket-ээр холбогдоно.
/// Controller-ууд IHubContext дамжуулж бүх клиентэд мэдэгдэл явуулна.
/// </summary>
public class BankHub : Hub
{
    /// <summary>Клиент холбогдоход консолд бүртгэнэ.</summary>
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] Холбогдлоо: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>Клиент салахад консолд бүртгэнэ.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] Салалаа: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}