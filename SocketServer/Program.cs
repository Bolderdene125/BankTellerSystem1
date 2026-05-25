using BankSystem.Shared.DTOs;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("Socket сервер эхэлж байна...");

// roomId → TcpClient  e.g. "305" → компьютер Б-ийн дэлгэц
var clients    = new Dictionary<string, TcpClient>(StringComparer.OrdinalIgnoreCase);
var clientLock = new object();

var listener = new TcpListener(IPAddress.Any, 5001);
listener.Start();
Console.WriteLine("TCP Listener: 0.0.0.0:5001 дээр хүлээж байна...");

var hub = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/bankhub")
    .WithAutomaticReconnect()
    .Build();

// Теллер "Дараагийн үйлчлүүлэгч" дарахад ирнэ
// roomId = "305" эсвэл "306" — зөвхөн тухайн дэлгэцэнд явуулна
hub.On<int, string>("ReceiveTellerCall", async (number, roomId) =>
{
    Console.WriteLine($"[TellerCall] Дугаар {number:D3} → Өрөө {roomId}");

    var msg = new SocketMessage
    {
        Type    = "TELLER_CALL",
        Payload = JsonSerializer.Serialize(new
        {
            number   = number,
            roomId   = roomId,
            time     = DateTime.Now.ToString("HH:mm")
        })
    };

    await SendToRoomAsync(roomId, JsonSerializer.Serialize(msg));
});

// NumberTerminal дугаар олгоход бүх дэлгэцэнд явуулна (optional)
hub.On<int>("ReceiveNumberUpdate", async (number) =>
{
    Console.WriteLine($"[NumberUpdate] Дугаар {number:D3} → бүх дэлгэцэнд");
    var msg = new SocketMessage
    {
        Type    = "SHOW_NUMBER",
        Payload = JsonSerializer.Serialize(new
        {
            number = number,
            time   = DateTime.Now.ToString("HH:mm")
        })
    };
    await SendToAllAsync(JsonSerializer.Serialize(msg));
});

await hub.StartAsync();
Console.WriteLine("SignalR: BankServer-т холбогдлоо OK");

// TCP клиент (NumberDisplay) холбогдохыг хүлээх
_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client));
    }
});

Console.WriteLine("Socket сервер бэлэн. Ctrl+C дарж зогсооно.");
await Task.Delay(Timeout.Infinite);

// ------------------------------------------------------------
// NumberDisplay холбогдохдоо эхний мессежээр roomId явуулна
// {"type":"REGISTER","payload":"305"}
// ------------------------------------------------------------
async Task HandleClientAsync(TcpClient client)
{
    var ep = client.Client.RemoteEndPoint;
    Console.WriteLine($"Клиент холбогдлоо: {ep}");
    try
    {
        var stream = client.GetStream();
        var buf    = new byte[256];
        int n      = await stream.ReadAsync(buf);
        var json   = Encoding.UTF8.GetString(buf, 0, n).Trim();
        var msg    = JsonSerializer.Deserialize<SocketMessage>(json);

        if (msg?.Type == "REGISTER" && !string.IsNullOrEmpty(msg.Payload))
        {
            var roomId = msg.Payload.Trim('"');
            lock (clientLock)
            {
                if (clients.ContainsKey(roomId))
                    clients[roomId].Dispose();
                clients[roomId] = client;
            }
            Console.WriteLine($"Бүртгэлт: Өрөө {roomId} = {ep}");

            // Холбогдсон тухай баталгаа явуулна
            var ack = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new SocketMessage
                    { Type = "ACK", Payload = $"\"Өрөө {roomId} бүртгэгдлээ\"" }) + "\n");
            await stream.WriteAsync(ack);
        }
        else
        {
            Console.WriteLine($"Бүртгэл амжилтгүй: {json}");
            client.Dispose();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HandleClient алдаа: {ex.Message}");
        client.Dispose();
    }
}

async Task SendToRoomAsync(string roomId, string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    TcpClient? target;
    lock (clientLock)
        clients.TryGetValue(roomId, out target);

    if (target == null)
    {
        Console.WriteLine($"[WARN] Өрөө {roomId} холбогдоогүй байна");
        return;
    }
    try
    {
        await target.GetStream().WriteAsync(data);
    }
    catch
    {
        lock (clientLock) clients.Remove(roomId);
        Console.WriteLine($"[WARN] Өрөө {roomId} салсан — жагсаалтаас хаслаа");
    }
}

async Task SendToAllAsync(string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    List<string> dead = new();
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
