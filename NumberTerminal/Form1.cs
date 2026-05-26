using System.Net.Http.Json;
using System.Drawing.Printing;
using Microsoft.Extensions.Configuration;

namespace NumberTerminal;

/// <summary>
/// Банкны үүдэнд байдаг дугаар авах терминал.
/// Хэрэглэгчийн Банк — цэнхэр/цагаан дизайн.
///
/// Зочин товч дарахад:
///   1. POST /api/ticket/issue — сервераас дугаар авна
///   2. WinForm дотор дугаарыг харуулна
///   3. Хэвлэх товч идэвхждэг
///
/// ЗАСВАР: btn.Enabled = false — давхар хүсэлтээс сэргийлнэ.
/// ЗАСВАР: Хэрэглэгчийн Банк брэнд нэр, өнгөний палитр.
/// </summary>
public partial class Form1 : Form
{
    private static readonly HttpClient _http = new();

    private static readonly string ServerUrl =
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build()["ServerUrl"] ?? "http://localhost:5000";

    // Хамгийн сүүлд авсан тасалбар — print-д хэрэгтэй
    private int      _lastNumber     = 0;
    private DateTime _lastIssuedAt   = DateTime.Now;
    private int      _lastQueueCount = 0;

    // ── Өнгөний палитр ────────────────────────────────────────────────────
    private static readonly Color HBNavy  = Color.FromArgb(11, 44, 74);
    private static readonly Color HBBlue  = Color.FromArgb(21, 101, 192);
    private static readonly Color HBSky   = Color.FromArgb(227, 240, 255);
    private static readonly Color HBGreen = Color.FromArgb(27, 127, 58);
    private static readonly Color HBRed   = Color.FromArgb(192, 57, 43);
    private static readonly Color HBSub   = Color.FromArgb(100, 116, 139);

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text            = "Хэрэглэгчийн Банк — Дугаарын Терминал";
        Size            = new Size(480, 560);
        BackColor       = HBSky;
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        // ── Header ────────────────────────────────────────────────────────
        var header = new Panel
        {
            Location  = new Point(0, 0),
            Size      = new Size(480, 72),
            BackColor = HBNavy
        };
        header.Controls.Add(new Label
        {
            Text      = "ХЭРЭГЛЭГЧИЙН БАНК",
            Font      = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location  = new Point(0, 12),
            Size      = new Size(480, 36),
            TextAlign = ContentAlignment.MiddleCenter
        });
        header.Controls.Add(new Label
        {
            Text      = "Teller Banking System",
            Font      = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(144, 185, 225),
            Location  = new Point(0, 46),
            Size      = new Size(480, 22),
            TextAlign = ContentAlignment.MiddleCenter
        });

        // ── Дугаарын карт ─────────────────────────────────────────────────
        var card = new Panel
        {
            Location  = new Point(30, 88),
            Size      = new Size(420, 200),
            BackColor = Color.White
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(187, 222, 251), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var acc = new SolidBrush(HBBlue);
            e.Graphics.FillRectangle(acc, 0, 0, 4, card.Height);
        };

        card.Controls.Add(new Label
        {
            Text      = "ТАНЫ ДУГААР",
            Font      = new Font("Segoe UI", 10),
            ForeColor = HBSub,
            Location  = new Point(0, 14),
            Size      = new Size(420, 24),
            TextAlign = ContentAlignment.MiddleCenter
        });

        var lblNumber = new Label
        {
            Name      = "lblNumber",
            Text      = "—",
            Font      = new Font("Segoe UI", 96, FontStyle.Bold),
            ForeColor = HBBlue,
            Location  = new Point(0, 36),
            Size      = new Size(420, 120),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblNumber);

        var lblTime = new Label
        {
            Name      = "lblTime",
            Text      = "",
            Font      = new Font("Segoe UI", 11),
            ForeColor = HBSub,
            Location  = new Point(0, 158),
            Size      = new Size(420, 28),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lblTime);

        // ── Дараалал мэдэгдэл ─────────────────────────────────────────────
        var lblQueue = new Label
        {
            Name      = "lblQueue",
            Text      = "",
            Font      = new Font("Segoe UI", 12),
            ForeColor = HBSub,
            Location  = new Point(30, 300),
            Size      = new Size(420, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── Товчнууд ──────────────────────────────────────────────────────
        var btnIssue = new Button
        {
            Name      = "btnIssue",
            Text      = "▶  ДУГААР АВАХ",
            Font      = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = HBBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(30, 345),
            Size      = new Size(260, 60),
            Cursor    = Cursors.Hand
        };
        btnIssue.FlatAppearance.BorderSize = 0;
        btnIssue.Click += BtnIssue_Click;

        var btnPrint = new Button
        {
            Name      = "btnPrint",
            Text      = "🖨  Хэвлэх",
            Font      = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = HBNavy,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(304, 345),
            Size      = new Size(146, 60),
            Cursor    = Cursors.Hand,
            Enabled   = false
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += BtnPrint_Click;

        // ── Сервер хаяг ───────────────────────────────────────────────────
        var lblServer = new Label
        {
            Text      = $"Сервер: {ServerUrl}",
            Font      = new Font("Segoe UI", 8),
            ForeColor = HBSub,
            Location  = new Point(0, 460),
            Size      = new Size(480, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.AddRange(new Control[]
        {
            header, card, lblQueue, btnIssue, btnPrint, lblServer
        });
    }

    /// <summary>
    /// "Дугаар авах" дарахад POST /api/ticket/issue явуулна.
    /// ЗАСВАР: btn.Enabled = false — давхар хүсэлтээс сэргийлнэ.
    /// </summary>
    private async void BtnIssue_Click(object? sender, EventArgs e)
    {
        var btn      = (Button)Controls["btnIssue"]!;
        var btnPrint = (Button)Controls["btnPrint"]!;

        btn.Enabled   = false;
        btn.Text      = "Уншиж байна...";
        btn.BackColor = Color.FromArgb(15, 70, 140);

        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api/ticket/issue",
                new { serviceType = "Гүйлгээ" });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<IssueTicketResponseDto>();

                if (result is not null)
                {
                    _lastNumber     = result.TicketNumber;
                    _lastIssuedAt   = result.IssuedAt;
                    _lastQueueCount = result.QueueCount;

                    UpdateDisplay(result.TicketNumber, result.IssuedAt, result.QueueCount);
                    btnPrint.Enabled = true;
                }
            }
            else
            {
                ShowError("Сервер хариу өгсөнгүй.");
            }
        }
        catch (HttpRequestException)
        {
            ShowError($"Сервер ({ServerUrl})-т холбогдож чадсангүй.");
        }
        finally
        {
            btn.Enabled   = true;
            btn.Text      = "▶  ДУГААР АВАХ";
            btn.BackColor = Color.FromArgb(21, 101, 192);
        }
    }

    private void UpdateDisplay(int number, DateTime issuedAt, int queueCount)
    {
        var card = Controls.OfType<Panel>()
            .FirstOrDefault(p => p.BackColor == Color.White);
        if (card == null) return;

        ((Label)card.Controls["lblNumber"]!).Text      = number.ToString("D3");
        ((Label)card.Controls["lblNumber"]!).ForeColor = Color.FromArgb(27, 127, 58);
        ((Label)card.Controls["lblTime"]!).Text        =
            $"Цаг: {issuedAt:HH:mm}  |  Сервис: Гүйлгээ";

        ((Label)Controls["lblQueue"]!).Text = $"Таны өмнө {queueCount} хүн хүлээж байна";
    }

    /// <summary>Тасалбар хэвлэнэ.</summary>
    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        if (_lastNumber == 0) return;

        var doc = new PrintDocument();
        doc.PrintPage += (s, ev) =>
        {
            if (ev.Graphics is null) return;
            var g      = ev.Graphics;
            var center = new StringFormat { Alignment = StringAlignment.Center };
            float cx   = ev.PageBounds.Width / 2f;

            g.DrawString("ХЭРЭГЛЭГЧИЙН БАНК",
                new Font("Arial", 16, FontStyle.Bold), Brushes.Black, cx, 20, center);
            g.DrawLine(Pens.Black, 20, 55, ev.PageBounds.Width - 20, 55);

            g.DrawString(_lastNumber.ToString("D3"),
                new Font("Arial", 52, FontStyle.Bold), Brushes.Black, cx, 65, center);

            g.DrawLine(Pens.Black, 20, 135, ev.PageBounds.Width - 20, 135);
            g.DrawString($"Сервис: Гүйлгээ",
                new Font("Arial", 10), Brushes.Black, 25, 145);
            g.DrawString($"Цаг: {_lastIssuedAt:yyyy-MM-dd HH:mm}",
                new Font("Arial", 10), Brushes.Black, 25, 165);
            g.DrawString($"Хүлээлт: {_lastQueueCount} хүн",
                new Font("Arial", 10), Brushes.Black, 25, 185);
            g.DrawLine(Pens.Black, 20, 205, ev.PageBounds.Width - 20, 205);

            g.DrawString("Тавтай морилно уу!",
                new Font("Arial", 9), Brushes.Gray, cx, 215, center);
        };

        try
        {
            using var dlg = new PrintDialog { Document = doc };
            if (dlg.ShowDialog() == DialogResult.OK)
                doc.Print();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Хэвлэхэд алдаа: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        var card = Controls.OfType<Panel>()
            .FirstOrDefault(p => p.BackColor == Color.White);
        if (card == null) return;

        ((Label)card.Controls["lblNumber"]!).Text      = "!";
        ((Label)card.Controls["lblNumber"]!).ForeColor = Color.FromArgb(192, 57, 43);
        ((Label)card.Controls["lblTime"]!).Text        = message;
        ((Label)Controls["lblQueue"]!).Text            = "";
    }
}

/// <summary>IssueTicket endpoint-ийн хариу.</summary>
record IssueTicketResponseDto(int TicketNumber, DateTime IssuedAt, int QueueCount);
