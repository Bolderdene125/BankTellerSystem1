using BankSystem.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

Console.WriteLine("╔══════════════════════════╗");
Console.WriteLine("║  БАНКНЫ SOCKET СЕРВЕР    ║");
Console.WriteLine("╚══════════════════════════╝");

// roomId → TcpClient: "305" → Компьютер Б-ийн дэлгэц
// Тухайн теллерийн дэлгэцэнд л TCP явуулна
var clients = new Dictionary<string, TcpClient>(StringComparer.OrdinalIgnoreCase);
var clientLock = new object();

var listener = new TcpListener(IPAddress.Any, 5001);
listener.Start();
Console.WriteLine("TCP: 0.0.0.0:5001 хүлээж байна...");

var hub = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/bankhub")
    .WithAutomaticReconnect()
    .Build();

/// <summary>
/// Теллер "Дараагийн үйлчлүүлэгч" дарахад ирнэ.
/// roomId = "305" → зөвхөн 305-ийн дэлгэцэнд TCP явуулна.
/// Shared.SocketMessage ашиглана.
/// </summary>
hub.On<int, string>("ReceiveTellerCall", async (number, roomId) =>
{
    Console.WriteLine($"[TellerCall] Дугаар {number:D3} → Өрөө {roomId}");
    var msg = new SocketMessage
    {
        Type = "TELLER_CALL",
        Payload = JsonSerializer.Serialize(new
        {
            number = number,
            roomId = roomId,
            time = DateTime.Now.ToString("HH:mm")
        })
    };
    await SendToRoomAsync(roomId, JsonSerializer.Serialize(msg));
});

/// <summary>
/// NumberTerminal дугаар авахад бүх дэлгэцэнд мэдэгдэнэ.
/// </summary>
hub.On<int>("ReceiveNumberUpdate", async (number) =>
{
    Console.WriteLine($"[NumberUpdate] Дугаар {number:D3} → бүх дэлгэцэнд");
    var msg = new SocketMessage
    {
        Type = "SHOW_NUMBER",
        Payload = JsonSerializer.Serialize(new
        {
            number = number,
            time = DateTime.Now.ToString("HH:mm")
        })
    };
    await SendToAllAsync(JsonSerializer.Serialize(msg));
});

await hub.StartAsync();
Console.WriteLine("SignalR: BankServer-т холбогдлоо ✓");

_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client));
    }
});

Console.WriteLine("Socket сервер бэлэн. Ctrl+C → зогсооно.\n");
await Task.Delay(Timeout.Infinite);

// ── NumberDisplay REGISTER мессеж хүлээн авна ────────────────────────────
/// <summary>
/// NumberDisplay холбогдоход RoomId-г бүртгэнэ.
/// Мессеж: { "type": "REGISTER", "payload": "\"305\"" }
/// </summary>
async Task HandleClientAsync(TcpClient client)
{
    var ep = client.Client.RemoteEndPoint;
    Console.WriteLine($"Клиент холбогдлоо: {ep}");
    try
    {
        var stream = client.GetStream();
        var buf = new byte[512];
        int n = await stream.ReadAsync(buf);
        var json = Encoding.UTF8.GetString(buf, 0, n).Trim();
        var msg = JsonSerializer.Deserialize<SocketMessage>(json);

        if (msg?.Type == "REGISTER" && !string.IsNullOrEmpty(msg.Payload))
        {
            var roomId = msg.Payload.Trim('"');
            lock (clientLock)
            {
                if (clients.ContainsKey(roomId)) clients[roomId].Dispose();
                clients[roomId] = client;
            }
            Console.WriteLine($"Бүртгэлт: Өрөө {roomId} ← {ep}");

            // ACK явуулна
            var ack = new SocketMessage { Type = "ACK", Payload = $"\"Өрөө {roomId} бүртгэгдлээ\"" };
            var ackB = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ack) + "\n");
            await stream.WriteAsync(ackB);
        }
        else
        {
            Console.WriteLine($"[WARN] Бүртгэл амжилтгүй: {json}");
            client.Dispose();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        client.Dispose();
    }
}

async Task SendToRoomAsync(string roomId, string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    TcpClient? target;
    lock (clientLock) clients.TryGetValue(roomId, out target);
    if (target == null) { Console.WriteLine($"[WARN] Өрөө {roomId} холбогдоогүй"); return; }
    try { await target.GetStream().WriteAsync(data); }
    catch { lock (clientLock) clients.Remove(roomId); }
}

async Task SendToAllAsync(string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    var dead = new List<string>();
    lock (clientLock)
    {
        foreach (var (id, c) in clients)
        {
            try { c.GetStream().WriteAsync(data).AsTask().Wait(); }
            catch { dead.Add(id); }
        }
        foreach (var id in dead) clients.Remove(id);
    }
}