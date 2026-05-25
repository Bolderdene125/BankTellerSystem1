using BankSystem.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NumberDisplay;

/// <summary>
/// Банкны залд байх дугаарын дэлгэц — fullscreen WinForm.
/// Хаан банкны ногоон/цагаан дизайн.
/// SocketServer-аас TCP-ээр зөвхөн өөрийн RoomId-тай мессеж хүлээн авна.
/// Shared.DTOs.SocketMessage ашиглана.
/// </summary>
public partial class Form1 : Form
{
    // ── Тохиргоо ─────────────────────────────────────────────────────────
    private static readonly IConfiguration _cfg =
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

    private static readonly string ServerIp = _cfg["SocketServerIp"] ?? "127.0.0.1";
    private static readonly int ServerPort = int.Parse(_cfg["SocketServerPort"] ?? "5001");

    /// <summary>
    /// RoomId = "305" эсвэл "306" — appsettings.json-аас авна.
    /// SocketServer зөвхөн энэ roomId-тай TELLER_CALL мессежийг явуулна.
    /// Компьютер Б: "305", Компьютер В: "306"
    /// </summary>
    private static readonly string RoomId = _cfg["RoomId"] ?? "000";

    // ── Хаан банкны өнгөний палитр ────────────────────────────────────────
    private static readonly Color BgColor = Color.FromArgb(10, 70, 35);
    private static readonly Color BgFlash = Color.FromArgb(15, 90, 45);
    private static readonly Color Gold = Color.FromArgb(220, 180, 40);
    private static readonly Color SubText = Color.FromArgb(180, 230, 200);
    private static readonly Color ConnOk = Color.FromArgb(100, 220, 140);
    private static readonly Color ConnFail = Color.FromArgb(255, 100, 100);

    // ── UI элементүүд ─────────────────────────────────────────────────────
    private Label _lblBank = null!;
    private Label _lblRoom = null!;
    private Label _lblNumber = null!;
    private Label _lblMsg = null!;
    private Label _lblTime = null!;
    private Label _lblStatus = null!;

    private TcpClient? _client;
    private readonly CancellationTokenSource _cts = new();

    public Form1()
    {
        InitializeComponent();
        BuildUI();
        Load += async (s, e) => await ConnectAsync();
    }

    private void BuildUI()
    {
        Text = $"Дэлгэц — Өрөө {RoomId}";
        BackColor = BgColor;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        Cursor = Cursors.Default;

        _lblBank = NewLabel("ХААН БАНК",
            new Font("Segoe UI", 28, FontStyle.Bold), Color.White,
            ContentAlignment.MiddleCenter);

        _lblRoom = NewLabel($"ЦОНХ  {RoomId}",
            new Font("Segoe UI", 20), SubText,
            ContentAlignment.MiddleCenter);

        _lblNumber = NewLabel("---",
            new Font("Segoe UI", 200, FontStyle.Bold), Color.White,
            ContentAlignment.MiddleCenter);
        _lblNumber.AutoSize = false;

        _lblMsg = NewLabel("Үйлчлүүлэгчийг хүлээж байна...",
            new Font("Segoe UI", 24), Gold,
            ContentAlignment.MiddleCenter);

        _lblTime = NewLabel(DateTime.Now.ToString("HH:mm"),
            new Font("Segoe UI", 18), SubText,
            ContentAlignment.MiddleRight);

        _lblStatus = NewLabel($"Холбогдож байна... ({ServerIp}:{ServerPort})",
            new Font("Segoe UI", 11), SubText,
            ContentAlignment.MiddleCenter);

        Controls.AddRange(new Control[]
            { _lblBank, _lblRoom, _lblNumber, _lblMsg, _lblTime, _lblStatus });

        Resize += (s, e) => DoLayout();
        DoLayout();

        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (s, e) => _lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();

        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    private void DoLayout()
    {
        int w = ClientSize.Width, h = ClientSize.Height;
        _lblBank.Size = new Size(w, 60); _lblBank.Location = new Point(0, 18);
        _lblRoom.Size = new Size(w, 44); _lblRoom.Location = new Point(0, 76);
        _lblNumber.Size = new Size(w, (int)(h * 0.52));
        _lblNumber.Location = new Point(0, 118);
        _lblMsg.Size = new Size(w, 55); _lblMsg.Location = new Point(0, h - 130);
        _lblStatus.Size = new Size(w, 32); _lblStatus.Location = new Point(0, h - 72);
        _lblTime.Size = new Size(w - 20, 40); _lblTime.Location = new Point(0, h - 40);
    }

    private static Label NewLabel(string text, Font font, Color fg,
                                   ContentAlignment align) => new()
                                   {
                                       Text = text,
                                       Font = font,
                                       ForeColor = fg,
                                       TextAlign = align,
                                       BackColor = Color.Transparent,
                                       AutoSize = false
                                   };

    // ── TCP холболт ───────────────────────────────────────────────────────

    /// <summary>
    /// SocketServer-т TCP-ээр холбогдоно.
    /// Shared.SocketMessage ашиглан REGISTER мессеж явуулна.
    /// Тасарвал 3 секундын дараа дахин холбогдоно.
    /// </summary>
    private async Task ConnectAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ServerIp, ServerPort, _cts.Token);
                SetStatus("Бүртгэж байна...", Gold);

                var stream = _client.GetStream();

                // REGISTER — Shared.SocketMessage ашиглана
                var reg = new SocketMessage { Type = "REGISTER", Payload = $"\"{RoomId}\"" };
                var regB = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reg) + "\n");
                await stream.WriteAsync(regB, _cts.Token);

                var buf = new byte[4096];
                var sb = new StringBuilder();

                while (!_cts.Token.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buf, _cts.Token);
                    if (n == 0) break;
                    sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                    string raw = sb.ToString(); int idx;
                    while ((idx = raw.IndexOf('\n')) >= 0)
                    {
                        string line = raw[..idx].Trim();
                        raw = raw[(idx + 1)..];
                        if (!string.IsNullOrEmpty(line)) ProcessMsg(line);
                    }
                    sb.Clear(); sb.Append(raw);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                SetStatus($"Дахин холбогдож байна... ({ex.Message})", ConnFail);
                await Task.Delay(3000, _cts.Token);
            }
            finally { _client?.Dispose(); }
        }
    }

    /// <summary>
    /// TCP мессеж боловсруулна.
    /// ACK: бүртгэл амжилттай.
    /// TELLER_CALL: теллер дуудсан дугаарыг харуулна.
    /// SHOW_NUMBER: шинэ дугаар олгогдсон.
    /// </summary>
    private void ProcessMsg(string json)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<SocketMessage>(json);
            if (msg == null) return;

            switch (msg.Type)
            {
                case "ACK":
                    SetStatus($"Холбогдлоо — Цонх {RoomId} ✓", ConnOk);
                    break;

                case "TELLER_CALL":
                    {
                        var p = JsonDocument.Parse(msg.Payload ?? "{}").RootElement;
                        int num = p.GetProperty("number").GetInt32();
                        string t = p.GetProperty("time").GetString() ?? "";
                        ShowNumber(num, $"Цонх {RoomId} руу ирнэ үү", t);
                        break;
                    }

                case "SHOW_NUMBER":
                    {
                        var p = JsonDocument.Parse(msg.Payload ?? "{}").RootElement;
                        int num = p.GetProperty("number").GetInt32();
                        string t = p.GetProperty("time").GetString() ?? "";
                        ShowNumber(num, "Дугаар олгогдлоо", t);
                        break;
                    }
            }
        }
        catch (Exception ex) { Console.WriteLine($"ProcessMsg: {ex.Message}"); }
    }

    private void ShowNumber(int number, string msg, string time)
    {
        if (InvokeRequired) { Invoke(() => ShowNumber(number, msg, time)); return; }
        _lblNumber.Text = number.ToString("D3");
        _lblMsg.Text = $"{msg}  —  {time}";
        _lblMsg.ForeColor = Gold;
        FlashAsync();
    }

    private async void FlashAsync()
    {
        for (int i = 0; i < 3; i++)
        {
            if (InvokeRequired) Invoke(() => BackColor = BgFlash); else BackColor = BgFlash;
            await Task.Delay(220);
            if (InvokeRequired) Invoke(() => BackColor = BgColor); else BackColor = BgColor;
            await Task.Delay(220);
        }
    }

    private void SetStatus(string msg, Color color)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(msg, color)); return; }
        _lblStatus.Text = msg;
        _lblStatus.ForeColor = color;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts.Cancel();
        base.OnFormClosing(e);
    }
}