using BankSystem.Shared.DTOs.Events;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  ХЭРЭГЛЭГЧИЙН БАНК — SOCKET СЕРВЕР  ║");
Console.WriteLine("╚══════════════════════════════════════╝");

// roomId → TcpClient: "305" → тухайн теллерийн дэлгэц
// Зөвхөн тухайн roomId-тай NumberDisplay-д TCP явуулна
var clients    = new Dictionary<string, TcpClient>(StringComparer.OrdinalIgnoreCase);
var clientLock = new object();

// ── TCP Listener ──────────────────────────────────────────────────────────
var listener = new TcpListener(IPAddress.Any, 5001);
listener.Start();
Console.WriteLine("[TCP] 0.0.0.0:5001 хүлээж байна...");

// ── SignalR холболт — BankServer-тай ──────────────────────────────────────
var hub = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/bankhub")
    .WithAutomaticReconnect()
    .Build();

// Холбогдсон/салсан мэдэгдэл
hub.Reconnecting  += ex => { Console.WriteLine($"[SignalR] Дахин холбогдож байна..."); return Task.CompletedTask; };
hub.Reconnected   += id => { Console.WriteLine($"[SignalR] Дахин холбогдлоо ✓"); return Task.CompletedTask; };
hub.Closed        += ex => { Console.WriteLine($"[SignalR] Холболт хаагдлаа: {ex?.Message}"); return Task.CompletedTask; };

/// <summary>
/// Теллер "Дараагийн үйлчлүүлэгч" дарахад ирнэ.
/// roomId = "305" → зөвхөн 305-ийн NumberDisplay-д TCP явуулна.
/// Shared.Events.SocketMessage ашиглана.
/// </summary>
hub.On<int, string>("ReceiveTellerCall", async (number, roomId) =>
{
    Console.WriteLine($"[TellerCall] Дугаар #{number:D3} → Өрөө {roomId}");
    var msg = new SocketMessage
    {
        Type    = "TELLER_CALL",
        Payload = JsonSerializer.Serialize(new
        {
            number = number,
            roomId = roomId,
            time   = DateTime.Now.ToString("HH:mm")
        })
    };
    await SendToRoomAsync(roomId, JsonSerializer.Serialize(msg));
});

/// <summary>
/// NumberTerminal шинэ дугаар авахад бүх дэлгэцэнд мэдэгдэнэ.
/// </summary>
hub.On<int>("ReceiveNumberUpdate", async (number) =>
{
    Console.WriteLine($"[NumberUpdate] Дугаар #{number:D3} → бүх дэлгэцэнд");
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
Console.WriteLine("[SignalR] BankServer-т холбогдлоо ✓");

// TCP клиент хүлээх loop
_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client));
    }
});

Console.WriteLine("[READY] Socket сервер бэлэн. Ctrl+C → зогсооно.\n");
await Task.Delay(Timeout.Infinite);

// ══════════════════════════════════════════════════════════════════════════

/// <summary>
/// NumberDisplay холбогдоход REGISTER мессеж хүлээн авна.
/// { "type": "REGISTER", "payload": "\"305\"" }
/// Бүртгэлийн дараа ACK явуулна.
///
/// ЗАСВАР: Алдааны мэдэгдэл дэлгэрэнгүй болсон.
/// ЗАСВАР: Хуучин холболт байвал dispose хийнэ.
/// </summary>
async Task HandleClientAsync(TcpClient client)
{
    var ep = client.Client.RemoteEndPoint;
    Console.WriteLine($"[CLIENT] Холбогдлоо: {ep}");
    try
    {
        var stream = client.GetStream();
        var buf    = new byte[512];
        int n      = await stream.ReadAsync(buf);
        var json   = Encoding.UTF8.GetString(buf, 0, n).Trim();
        var msg    = JsonSerializer.Deserialize<SocketMessage>(json);

        if (msg?.Type == "REGISTER" && !string.IsNullOrEmpty(msg.Payload))
        {
            var roomId = msg.Payload.Trim('"');
            lock (clientLock)
            {
                // Хуучин холболт байвал dispose хийнэ
                if (clients.TryGetValue(roomId, out var old))
                {
                    Console.WriteLine($"[CLIENT] Хуучин холболт ({roomId}) солигдлоо");
                    old.Dispose();
                }
                clients[roomId] = client;
            }
            Console.WriteLine($"[REGISTER] Өрөө {roomId} ← {ep}");

            // ACK явуулна
            var ack  = new SocketMessage
            {
                Type    = "ACK",
                Payload = $"\"Өрөө {roomId} бүртгэгдлээ\""
            };
            var ackB = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ack) + "\n");
            await stream.WriteAsync(ackB);

            // Холболтыг амьд байлгах — keep-alive loop
            // Клиент тасарвал ReadAsync 0 буцаана
            try
            {
                var keepBuf = new byte[1];
                while (true)
                {
                    int read = await stream.ReadAsync(keepBuf);
                    if (read == 0) break; // тасарсан
                }
            }
            catch { }

            lock (clientLock)
            {
                if (clients.TryGetValue(roomId, out var current) && current == client)
                {
                    clients.Remove(roomId);
                    Console.WriteLine($"[DISCONNECT] Өрөө {roomId} тасарлаа");
                }
            }
        }
        else
        {
            Console.WriteLine($"[WARN] Бүртгэл амжилтгүй: {json}");
            client.Dispose();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] HandleClient: {ex.Message}");
        client.Dispose();
    }
}

/// <summary>Тодорхой өрөөний дэлгэцэнд мессеж явуулна.</summary>
async Task SendToRoomAsync(string roomId, string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");
    TcpClient? target;
    lock (clientLock) clients.TryGetValue(roomId, out target);

    if (target == null)
    {
        Console.WriteLine($"[WARN] Өрөө {roomId} холбогдоогүй");
        return;
    }

    try
    {
        await target.GetStream().WriteAsync(data);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Өрөө {roomId}-д явуулж чадсангүй: {ex.Message}");
        lock (clientLock) clients.Remove(roomId);
    }
}

/// <summary>
/// Бүх холбогдсон дэлгэцэнд мессеж явуулна.
/// ЗАСВАР: lock{} дотор async ашиглахгүй — deadlock-оос сэргийлнэ.
/// Эхлээд snapshot авч, дараа нь явуулна.
/// </summary>
async Task SendToAllAsync(string message)
{
    var data = Encoding.UTF8.GetBytes(message + "\n");

    // Snapshot — lock дотор async дуудахгүй
    List<(string id, TcpClient client)> snapshot;
    lock (clientLock)
        snapshot = clients.Select(kv => (kv.Key, kv.Value)).ToList();

    var dead = new List<string>();
    foreach (var (id, client) in snapshot)
    {
        try
        {
            await client.GetStream().WriteAsync(data);
        }
        catch
        {
            dead.Add(id);
        }
    }

    if (dead.Count > 0)
    {
        lock (clientLock)
            foreach (var id in dead)
            {
                clients.Remove(id);
                Console.WriteLine($"[DISCONNECT] Өрөө {id} тасарлаа");
            }
    }
}
