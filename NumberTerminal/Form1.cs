using System.Net.Http.Json;
using System.Drawing.Printing;
using Microsoft.Extensions.Configuration;

namespace NumberTerminal;

/// <summary>
/// Банкны үүдэнд байдаг дугаар авах терминал.
/// Зочин товч дарахад сервераас дугаар авч WinForm дотор харуулна.
/// Print товч дарахад тасалбар хэвлэнэ.
/// </summary>
public partial class Form1 : Form
{
    private static readonly HttpClient _http = new();

    // appsettings.json-оос URL авна — IP өөрчлөхөд код засахгүй
    private static readonly string ServerUrl =
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build()["ServerUrl"] ?? "http://localhost:5000";

    // Хамгийн сүүлд авсан тасалбарын мэдээлэл — print-д хэрэгтэй
    private int _lastNumber = 0;
    private DateTime _lastIssuedAt = DateTime.Now;
    private int _lastQueueCount = 0;

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text = "Банкны Тикетийн Терминал";
        Size = new Size(480, 520);
        BackColor = Color.FromArgb(240, 248, 255);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // Банкны нэр
        var lblTitle = new Label
        {
            Text = "🏦 ХААН БАНК",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0, 102, 204),
            Location = new Point(0, 20),
            Size = new Size(480, 55)
        };

        // Ялгах зураас
        var separator = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(30, 80),
            Size = new Size(420, 2)
        };

        // "Таны дугаар" гарчиг
        var lblHeader = new Label
        {
            Text = "ТАНЫ ДУГААР",
            Font = new Font("Segoe UI", 12),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Location = new Point(0, 95),
            Size = new Size(480, 30)
        };

        // Дугаарыг том font-оор харуулдаг label — гол элемент
        var lblNumber = new Label
        {
            Name = "lblNumber",
            Text = "—",
            Font = new Font("Segoe UI", 96, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0, 102, 204),
            Location = new Point(0, 125),
            Size = new Size(480, 150)
        };

        // Авсан цаг харуулах
        var lblTime = new Label
        {
            Name = "lblTime",
            Text = "",
            Font = new Font("Segoe UI", 13),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(0, 280),
            Size = new Size(480, 30)
        };

        // Хүлээлтийн тоо
        var lblQueue = new Label
        {
            Name = "lblQueue",
            Text = "",
            Font = new Font("Segoe UI", 12),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Location = new Point(0, 315),
            Size = new Size(480, 28)
        };

        // Дугаар авах товч
        var btnIssue = new Button
        {
            Name = "btnIssue",
            Text = "🎫  ДУГААР АВАХ",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 153, 51),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(40, 360),
            Size = new Size(260, 60),
            Cursor = Cursors.Hand
        };
        btnIssue.FlatAppearance.BorderSize = 0;
        btnIssue.Click += BtnIssue_Click;

        // Print товч — дугаар авсны дараа идэвхждэг
        var btnPrint = new Button
        {
            Name = "btnPrint",
            Text = "🖨  ХЭВЛЭХ",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(315, 360),
            Size = new Size(125, 60),
            Cursor = Cursors.Hand,
            Enabled = false  // дугаар авахаас өмнө идэвхгүй
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += BtnPrint_Click;

        // Сервер хаяг харуулах — холбогдсон эсэхийг мэдэхэд хэрэгтэй
        var lblServer = new Label
        {
            Text = $"Сервер: {ServerUrl}",
            Font = new Font("Segoe UI", 8),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.LightGray,
            Location = new Point(0, 440),
            Size = new Size(480, 20)
        };

        Controls.AddRange(new Control[]
        {
            lblTitle, separator, lblHeader, lblNumber,
            lblTime, lblQueue, btnIssue, btnPrint, lblServer
        });
    }

    /// <summary>
    /// "Дугаар авах" дарахад сервер рүү POST явуулна.
    /// Зөвхөн "Гүйлгээ" сервис ашиглана — бусад төрөл хэрэггүй.
    /// async void: UI event handler-д зөвшөөрөгдөнө.
    /// </summary>
    private async void BtnIssue_Click(object? sender, EventArgs e)
    {
        var btn = (Button)Controls["btnIssue"]!;
        var btnPrint = (Button)Controls["btnPrint"]!;

        btn.Enabled = false;
        btn.Text = "Уншиж байна...";

        try
        {
            // DTO ашиглан хүсэлт явуулна
            var response = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api/ticket/issue",
                new { serviceType = "Гүйлгээ" });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<IssueTicketResponseDto>();

                if (result is not null)
                {
                    // Дугаарыг санах — print-д хэрэгтэй
                    _lastNumber = result.Number;
                    _lastIssuedAt = result.IssuedAt;
                    _lastQueueCount = result.QueueCount;

                    // WinForm дотор харуулна — popup биш
                    UpdateDisplay(result.Number, result.IssuedAt, result.QueueCount);

                    // Print товчийг идэвхжүүлнэ
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
            btn.Enabled = true;
            btn.Text = "🎫  ДУГААР АВАХ";
        }
    }

    /// <summary>
    /// WinForm дотор дугаарын мэдээллийг шинэчилнэ.
    /// Popup харуулахгүй — form дотор харагдана.
    /// </summary>
    private void UpdateDisplay(int number, DateTime issuedAt, int queueCount)
    {
        ((Label)Controls["lblNumber"]!).Text = number.ToString("D3");
        ((Label)Controls["lblTime"]!).Text = $"Цаг: {issuedAt:HH:mm}  |  Сервис: Гүйлгээ";
        ((Label)Controls["lblQueue"]!).Text = $"Таны өмнө {queueCount} хүн хүлээж байна";

        // Дугаар авсны дараа ногоон өнгөтэй болгоно
        ((Label)Controls["lblNumber"]!).ForeColor = Color.FromArgb(0, 153, 51);
    }

    /// <summary>
    /// Тасалбар хэвлэх. PrintDocument ашиглана.
    /// Жинхэнэ POS printer-д ESC/POS protocol ашиглана.
    /// </summary>
    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        if (_lastNumber == 0) return;

        var printDoc = new PrintDocument();
        printDoc.PrintPage += (s, ev) =>
        {
            if (ev.Graphics is null) return;

            var g = ev.Graphics;
            var black = Brushes.Black;
            var center = new StringFormat { Alignment = StringAlignment.Center };
            float pageCenter = ev.PageBounds.Width / 2f;

            // Банкны нэр
            g.DrawString("ХААН БАНК",
                new Font("Arial", 16, FontStyle.Bold),
                black, pageCenter, 20, center);

            // Ялгах зураас
            g.DrawLine(Pens.Black, 20, 55, ev.PageBounds.Width - 20, 55);

            // Дугаар
            g.DrawString(_lastNumber.ToString("D3"),
                new Font("Arial", 48, FontStyle.Bold),
                black, pageCenter, 65, center);

            // Дэлгэрэнгүй мэдээлэл
            g.DrawLine(Pens.Black, 20, 130, ev.PageBounds.Width - 20, 130);
            g.DrawString($"Сервис: Гүйлгээ",
                new Font("Arial", 10), black, 25, 140);
            g.DrawString($"Цаг: {_lastIssuedAt:yyyy-MM-dd HH:mm}",
                new Font("Arial", 10), black, 25, 160);
            g.DrawString($"Хүлээлт: {_lastQueueCount} хүн",
                new Font("Arial", 10), black, 25, 180);
            g.DrawLine(Pens.Black, 20, 200, ev.PageBounds.Width - 20, 200);

            g.DrawString("Тавтай морилно уу!",
                new Font("Arial", 9), Brushes.Gray, pageCenter, 210, center);
        };

        try
        {
            // Print dialog харуулж printer сонгуулна
            using var dlg = new PrintDialog { Document = printDoc };
            if (dlg.ShowDialog() == DialogResult.OK)
                printDoc.Print();
        }
        catch (Exception ex)
        {
            ShowError($"Хэвлэхэд алдаа гарлаа: {ex.Message}");
        }
    }

    /// <summary>Алдааны мэдэгдлийг form дотор харуулна.</summary>
    private void ShowError(string message)
    {
        ((Label)Controls["lblNumber"]!).Text = "!";
        ((Label)Controls["lblNumber"]!).ForeColor = Color.Red;
        ((Label)Controls["lblTime"]!).Text = message;
        ((Label)Controls["lblQueue"]!).Text = "";
    }
}

/// <summary>IssueTicket endpoint-ийн DTO хариу.</summary>
record IssueTicketResponseDto(
    int Number,
    DateTime IssuedAt,
    string ServiceType,
    int QueueCount
);