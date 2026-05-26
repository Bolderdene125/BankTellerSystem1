using Microsoft.AspNetCore.SignalR;

namespace BankServer.Hubs;

/// <summary>
/// SignalR Hub — клиентүүд WebSocket-ээр холбогдоно.
///
/// Холболтын урсгал:
///  
///   SocketServer  ── BankHub ── Controller-ууд
///   CurrencyDisplay Blazor  ── broadcast (ReceiveRateUpdate,
///                                            ReceiveTellerCall,
///                                            ReceiveNumberUpdate)
///
/// Controller-ууд IHubContext&lt;BankHub&gt; дамжуулж бүх клиентэд явуулна.
/// ЗАСВАР: ILogger нэмэгдсэн.
/// </summary>
public class BankHub : Hub
{
    private readonly ILogger<BankHub> _logger;

    public BankHub(ILogger<BankHub> logger)
    {
        _logger = logger;
    }

    /// <summary>Клиент холбогдоход бүртгэнэ.</summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR клиент холбогдлоо: {ConnectionId}",
            Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>Клиент салахад бүртгэнэ.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
            _logger.LogInformation("SignalR клиент салалаа: {ConnectionId}",
                Context.ConnectionId);
        else
            _logger.LogWarning(exception,
                "SignalR клиент алдаатай салалаа: {ConnectionId}",
                Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}
