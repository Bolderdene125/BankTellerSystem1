using BankSystem.Shared.DTOs.Events;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NumberDisplay;

/// <summary>
/// Банкны залд байх дугаарын дэлгэц — fullscreen WinForm.
/// Хэрэглэгчийн Банк — цэнхэр/цагаан дизайн.
///
/// SocketServer-аас TCP-ээр зөвхөн өөрийн RoomId-тай мессеж хүлээн авна.
/// Shared.DTOs.Events.SocketMessage ашиглана.
///
/// ЗАСВАР: Smooth flash animation — Timer-д суурилсан.
/// ЗАСВАР: Divider зураас нэмэгдсэн.
/// ЗАСВАР: RoomId pill badge харагдал.
/// </summary>
public partial class Form1 : Form
{
    // ── Тохиргоо ──────────────────────────────────────────────────────────
    private static readonly IConfiguration _cfg =
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

    private static readonly string ServerIp   = _cfg["SocketServerIp"]   ?? "127.0.0.1";
    private static readonly int    ServerPort  = int.Parse(_cfg["SocketServerPort"] ?? "5001");
    private static readonly string RoomId      = _cfg["RoomId"] ?? "000";

    // ── Хэрэглэгчийн Банк өнгөний палитр — дэлгэцэд ─────────────────────
    private static readonly Color BgColor   = Color.FromArgb(11,  44,  74);  // #0B2C4A — харанхуй цэнхэр
    private static readonly Color BgFlash   = Color.FromArgb(21,  80, 130);  // Flash үед
    private static readonly Color AccentBlue= Color.FromArgb(66, 165, 245);  // #42A5F5 — лого, дугаар
    private static readonly Color GoldColor = Color.FromArgb(255, 193,   7);  // #FFC107 — мэдэгдэл
    private static readonly Color SubText   = Color.FromArgb(144, 185, 225);  // дэд текст
    private static readonly Color ConnOk    = Color.FromArgb( 76, 175,  80);  // холбогдсон
    private static readonly Color ConnFail  = Color.FromArgb(244,  67,  54);  // тасарсан

    // ── UI элементүүд ──────────────────────────────────────────────────────
    private Label _lblBank    = null!;
    private Label _lblSub     = null!;
    private Label _lblRoom    = null!;
    private Label _lblNumber  = null!;
    private Label _lblMsg     = null!;
    private Label _lblTime    = null!;
    private Label _lblStatus  = null!;

    private TcpClient? _client;
    private readonly CancellationTokenSource _cts = new();

    // Smooth flash
    private System.Windows.Forms.Timer? _flashTimer;
    private int   _flashStep  = 0;
    private bool  _flashRise  = true;
    private const int FlashSteps = 16;

    public Form1()
    {
        InitializeComponent();
        BuildUI();
        Load += async (s, e) => await ConnectAsync();
    }

    private void BuildUI()
    {
        Text            = $"Хэрэглэгчийн Банк — Дугаарын Дэлгэц — Цонх {RoomId}";
        BackColor       = BgColor;
        FormBorderStyle = FormBorderStyle.None;
        WindowState     = FormWindowState.Maximized;
        Cursor          = Cursors.Default;

        _lblBank = NewLabel("ХЭРЭГЛЭГЧИЙН БАНК",
            new Font("Segoe UI", 26, FontStyle.Bold),
            Color.White, ContentAlignment.MiddleCenter);

        _lblSub = NewLabel("Teller Banking System",
            new Font("Segoe UI", 13),
            SubText, ContentAlignment.MiddleCenter);

        _lblRoom = NewLabel($"ЦОНХ  {RoomId}",
            new Font("Segoe UI", 16, FontStyle.Bold),
            AccentBlue, ContentAlignment.MiddleCenter);

        _lblNumber = NewLabel("---",
            new Font("Segoe UI", 200, FontStyle.Bold),
            Color.White, ContentAlignment.MiddleCenter);
        _lblNumber.AutoSize = false;

        _lblMsg = NewLabel("Үйлчлүүлэгчийг хүлээж байна...",
            new Font("Segoe UI", 22),
            GoldColor, ContentAlignment.MiddleCenter);

        _lblTime = NewLabel(DateTime.Now.ToString("HH:mm:ss"),
            new Font("Segoe UI", 16),
            SubText, ContentAlignment.MiddleRight);

        _lblStatus = NewLabel($"Холбогдож байна... ({ServerIp}:{ServerPort})",
            new Font("Segoe UI", 11),
            SubText, ContentAlignment.MiddleCenter);

        Controls.AddRange(new Control[]
        {
            _lblBank, _lblSub, _lblRoom,
            _lblNumber, _lblMsg,
            _lblTime, _lblStatus
        });

        // Цаг шинэчлэх
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (s, e) => _lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();

        // Divider зураас
        Paint += DrawDivider;

        Resize += (s, e) => DoLayout();
        DoLayout();

        // ESC → хаана
        KeyPreview = true;
        KeyDown    += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    /// <summary>
    /// Элементүүдийн байршил тооцоолно.
    /// Form хэмжээ өөрчлөгдөхөд автоматаар дуудагдана.
    /// </summary>
    private void DoLayout()
    {
        int w = ClientSize.Width, h = ClientSize.Height;
        _lblBank.Size     = new Size(w, 50);   _lblBank.Location   = new Point(0, 20);
        _lblSub.Size      = new Size(w, 28);   _lblSub.Location    = new Point(0, 68);
        _lblRoom.Size     = new Size(w, 40);   _lblRoom.Location   = new Point(0, 100);
        _lblNumber.Size   = new Size(w, (int)(h * 0.48));
        _lblNumber.Location = new Point(0, 138);
        _lblMsg.Size      = new Size(w, 52);   _lblMsg.Location    = new Point(0, h - 120);
        _lblStatus.Size   = new Size(w, 30);   _lblStatus.Location = new Point(0, h - 64);
        _lblTime.Size     = new Size(w - 20, 36); _lblTime.Location = new Point(0, h - 36);
        Invalidate(); // divider-г дахин зурна
    }

    /// <summary>
    /// Дэлгэцийн дугаар болон статусын хоорондох нимгэн зураас.
    /// Paint event дээр зурагдана.
    /// </summary>
    private void DrawDivider(object? sender, PaintEventArgs e)
    {
        int y = ClientSize.Height - 130;
        using var pen = new Pen(Color.FromArgb(50, 255, 255, 255), 1);
        e.Graphics.DrawLine(pen, 30, y, ClientSize.Width - 30, y);
    }

    private static Label NewLabel(string text, Font font, Color fg,
        ContentAlignment align) => new()
    {
        Text      = text,
        Font      = font,
        ForeColor = fg,
        TextAlign = align,
        BackColor = Color.Transparent,
        AutoSize  = false
    };

    // ── Smooth flash animation ────────────────────────────────────────────

    /// <summary>
    /// Smooth flash — background өнгийг аажмаар өөрчилнэ.
    /// Өмнөх FlashAsync: BackColor-г 3 удаа агшин зуур солино (хатуу).
    /// Шинэ аргад 16ms Timer-ийн тусламжтайгаар аажим fade хийнэ.
    /// </summary>
    private void FlashSmooth()
    {
        _flashTimer?.Stop();
        _flashStep = 0;
        _flashRise = true;

        _flashTimer = new System.Windows.Forms.Timer { Interval = 25 };
        _flashTimer.Tick += (s, e) =>
        {
            if (_flashRise) _flashStep++;
            else            _flashStep--;

            // BgColor → BgFlash аажим шилжилт
            float ratio = (float)_flashStep / FlashSteps;
            int r = (int)(11  + (21  - 11)  * ratio);
            int g = (int)(44  + (80  - 44)  * ratio);
            int b = (int)(74  + (130 - 74)  * ratio);

            if (InvokeRequired) Invoke(() => BackColor = Color.FromArgb(r, g, b));
            else                BackColor = Color.FromArgb(r, g, b);

            if (_flashStep >= FlashSteps) _flashRise = false;
            if (_flashStep <= 0 && !_flashRise)
            {
                _flashTimer.Stop();
                if (InvokeRequired) Invoke(() => BackColor = BgColor);
                else                BackColor = BgColor;
            }
        };
        _flashTimer.Start();
    }

    // ── TCP холболт ───────────────────────────────────────────────────────

    /// <summary>
    /// SocketServer-т TCP-ээр холбогдоно.
    /// Shared.SocketMessage ашиглан REGISTER мессеж явуулна.
    /// Тасарвал 3 секундын дараа дахин оролдоно (reconnect loop).
    /// </summary>
    private async Task ConnectAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ServerIp, ServerPort, _cts.Token);
                SetStatus("Бүртгэж байна...", GoldColor);

                var stream = _client.GetStream();

                // REGISTER мессеж явуулна — Shared.Events.SocketMessage ашиглана
                var reg  = new SocketMessage { Type = "REGISTER", Payload = $"\"{RoomId}\"" };
                var regB = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reg) + "\n");
                await stream.WriteAsync(regB, _cts.Token);

                // Мессеж унших loop
                var buf = new byte[4096];
                var sb  = new StringBuilder();

                while (!_cts.Token.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buf, _cts.Token);
                    if (n == 0) break;

                    sb.Append(Encoding.UTF8.GetString(buf, 0, n));
                    string raw = sb.ToString();
                    int idx;

                    while ((idx = raw.IndexOf('\n')) >= 0)
                    {
                        string line = raw[..idx].Trim();
                        raw = raw[(idx + 1)..];
                        if (!string.IsNullOrEmpty(line)) ProcessMsg(line);
                    }
                    sb.Clear();
                    sb.Append(raw);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                SetStatus($"Дахин холбогдож байна... ({ex.Message})", ConnFail);
                try { await Task.Delay(3000, _cts.Token); }
                catch (OperationCanceledException) { break; }
            }
            finally
            {
                _client?.Dispose();
            }
        }
    }

    /// <summary>
    /// TCP мессеж боловсруулна.
    /// ACK:         бүртгэл амжилттай.
    /// TELLER_CALL: теллер тухайн цонхны дугаарыг дуудав.
    /// SHOW_NUMBER: шинэ дугаар олгогдов (бүх дэлгэцэд).
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
                    var p   = JsonDocument.Parse(msg.Payload ?? "{}").RootElement;
                    int num = p.GetProperty("number").GetInt32();
                    string t = p.GetProperty("time").GetString() ?? "";
                    ShowNumber(num, $"⚡ Цонх {RoomId} руу ирнэ үү", t);
                    break;
                }

                case "SHOW_NUMBER":
                {
                    var p   = JsonDocument.Parse(msg.Payload ?? "{}").RootElement;
                    int num = p.GetProperty("number").GetInt32();
                    string t = p.GetProperty("time").GetString() ?? "";
                    ShowNumber(num, "Дугаар олгогдлоо", t);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProcessMsg алдаа: {ex.Message}");
        }
    }

    private void ShowNumber(int number, string msg, string time)
    {
        if (InvokeRequired) { Invoke(() => ShowNumber(number, msg, time)); return; }
        _lblNumber.Text = number.ToString("D3");
        _lblMsg.Text    = $"{msg}  —  {time}";
        _lblMsg.ForeColor = GoldColor;
        FlashSmooth();
    }

    private void SetStatus(string msg, Color color)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(msg, color)); return; }
        _lblStatus.Text      = msg;
        _lblStatus.ForeColor = color;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts.Cancel();
        base.OnFormClosing(e);
    }
}
