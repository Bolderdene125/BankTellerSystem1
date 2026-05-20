using System.Net.Http.Json;

namespace NumberTerminal;

/// <summary>
/// Зочин товч дарахад BankServer-ээс дугаар авч харуулна.
/// Жинхэнэ тохиолдолд авсан дугаарыг POS printer-т явуулна.
/// </summary>
public partial class Form1 : Form
{
    // HttpClient: програм дотор нэг л instance байх хэрэгтэй (static)
    private static readonly HttpClient _http = new();
    private const string ServerUrl = "http://localhost:5000";

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    /// <summary>Form-ийн UI-г кодоор байгуулна.</summary>
    private void SetupUI()
    {
        Text = "Банкны Тикетийн Терминал";
        Size = new Size(500, 420);
        BackColor = Color.FromArgb(240, 248, 255);
        StartPosition = FormStartPosition.CenterScreen;

        var lblTitle = new Label
        {
            Text = "🏦 ХААН БАНК",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0, 102, 204),
            Location = new Point(50, 20),
            Size = new Size(400, 50)
        };

        // Олгосон дугаарыг том font-оор харуулдаг label
        var lblNumber = new Label
        {
            Name = "lblNumber",
            Text = "—",
            Font = new Font("Segoe UI", 72, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0, 102, 204),
            Location = new Point(50, 80),
            Size = new Size(400, 130)
        };

        var lblQueue = new Label
        {
            Name = "lblQueue",
            Text = "Хүлээлт: —",
            Font = new Font("Segoe UI", 14),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(100, 215),
            Size = new Size(300, 30)
        };

        var cmbService = new ComboBox
        {
            Name = "cmbService",
            Font = new Font("Segoe UI", 12),
            Location = new Point(150, 255),
            Size = new Size(200, 35),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbService.Items.AddRange(new[] { "Гүйлгээ", "Лавлагаа", "Зээл", "Карт" });
        cmbService.SelectedIndex = 0;

        var btnIssue = new Button
        {
            Name = "btnIssue",
            Text = "🎫 ДУГААР АВАХ",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 153, 51),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(100, 305),
            Size = new Size(300, 60),
            Cursor = Cursors.Hand
        };
        btnIssue.Click += BtnIssue_Click;

        Controls.AddRange(new Control[] { lblTitle, lblNumber, lblQueue, cmbService, btnIssue });
    }

    /// <summary>
    /// "Дугаар авах" дарахад ажиллана.
    /// async void: UI event handler-д зөвшөөрөгдөнө, бусад газар хэрэглэхгүй.
    /// </summary>
    private async void BtnIssue_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        var cmbService = (ComboBox)Controls["cmbService"]!;
        var lblNumber = (Label)Controls["lblNumber"]!;
        var lblQueue = (Label)Controls["lblQueue"]!;

        btn.Enabled = false; // давхар дарахаас сэргийлнэ
        btn.Text = "Уншиж байна...";

        try
        {
            string serviceType = cmbService.SelectedItem?.ToString() ?? "Гүйлгээ";

            var response = await _http.PostAsync(
                $"{ServerUrl}/api/ticket/issue?serviceType={Uri.EscapeDataString(serviceType)}",
                null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TicketResponse>();
                if (result is not null)
                {
                    lblNumber.Text = result.Number.ToString("D3"); // 001, 042 гэх мэт
                    lblQueue.Text = $"Хүлээж байгаа: {result.QueueCount} хүн";
                    PrintTicket(result.Number, result.IssuedAt, serviceType);
                }
            }
            else
            {
                MessageBox.Show("Серверт холбогдоход алдаа гарлаа!", "Алдаа",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (HttpRequestException)
        {
            MessageBox.Show("Сервер ажиллаж байгаа эсэхийг шалгана уу.",
                "Холболтын алдаа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btn.Enabled = true;
            btn.Text = "🎫 ДУГААР АВАХ";
        }
    }

    /// <summary>
    /// Тасалбар "хэвлэх". Жинхэнэ проектод ESC/POS printer protocol дуудна.
    /// </summary>
    private void PrintTicket(int number, DateTime issuedAt, string serviceType)
    {
        MessageBox.Show(
            $"Таны дугаар: {number:D3}\nСервис: {serviceType}\nЦаг: {issuedAt:HH:mm}",
            "Дугаар амжилттай авлаа!",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

/// <summary>IssueTicket endpoint-ийн JSON хариуг хүлээн авах загвар.</summary>
record TicketResponse(int Number, DateTime IssuedAt, string ServiceType, int QueueCount);