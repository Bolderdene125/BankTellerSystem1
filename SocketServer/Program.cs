// SocketServer/Program.cs
// TCP Socket сервер — дугаарын дэлгэцүүдтэй холбогдоно.
// BankServer-аас SignalR-ээр дугаар авч, дэлгэцүүдэд TCP-ээр явуулна.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("Socket сервер эхэлж байна...");

// Холбогдсон клиентүүдийн жагсаалт — thread-safe
var clients = new List<TcpClient>();
var clientLock = new object();

// TCP Listener — дэлгэцүүд 5001 port-д холбогдоно
var listener = new TcpListener(IPAddress.Any, 5001);
listener.Start();
Console.WriteLine("TCP Listener: 0.0.0.0:5001 дээр хүлээж байна...");

// SignalR — BankServer-аас дугаар авна
var hub = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/bankhub")
    .WithAutomaticReconnect()
    .Build();

// BankServer-аас дугаар ирэхэд бүх дэлгэцэнд явуулна
hub.On<int>("ReceiveNumberUpdate", async (number) =>
{
    Console.WriteLine($"Дугаар авлаа: {number} → бүх дэлгэцэнд явуулж байна...");

    var message = JsonSerializer.Serialize(new { type = "SHOW_NUMBER", number });
    var data = Encoding.UTF8.GetBytes(message + "\n");

    List<TcpClient> deadClients = new();

    lock (clientLock)
    {
        foreach (var client in clients)
        {
            try
            {
                if (client.Connected)
                    client.GetStream().WriteAsync(data).AsTask().Wait();
                else
                    deadClients.Add(client);
            }
            catch
            {
                deadClients.Add(client);
            }
        }

        // Салсан клиентүүдийг устгана
        foreach (var dead in deadClients)
            clients.Remove(dead);
    }
});

await hub.StartAsync();
Console.WriteLine("SignalR: BankServer-т холбогдлоо ✅");

// Клиент холбогдохыг хүлээх — background task
_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        Console.WriteLine($"Дэлгэц холбогдлоо: {client.Client.RemoteEndPoint}");

        lock (clientLock)
            clients.Add(client);
    }
});

Console.WriteLine("Socket сервер бэлэн. Ctrl+C дарж зогсооно.");
await Task.Delay(Timeout.Infinite);