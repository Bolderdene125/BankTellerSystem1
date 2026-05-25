using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TellerApp;

/// <summary>
/// Теллерийн гурван үндсэн үйлдэл:
///   Таб 1 — Дараагийн үйлчлүүлэгч дуудах
///   Таб 2 — А → Б гүйлгээ хийх
///   Таб 3 — Валютын ханш өөрчлөх
/// </summary>
public partial class Form1 : Form
{
    private static readonly HttpClient _http = new();
    private static readonly string ServerUrl =
    new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .Build()["ServerUrl"] ?? "http://localhost:5000";
    private TabControl _tabs = null!;

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text = "Теллерийн Апп — Хаан Банк";
        Size = new Size(700, 560);
        StartPosition = FormStartPosition.CenterScreen;

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(CreateQueueTab());
        _tabs.TabPages.Add(CreateTransferTab());
        _tabs.TabPages.Add(CreateRateTab());
        Controls.Add(_tabs);
    }

    // ── Таб 1: Дугаар дуудах ─────────────────────────────────────

    /// <summary>Дугаар дуудах табыг байгуулна.</summary>
    private TabPage CreateQueueTab()
    {
        var tab = new TabPage("🔢 Дугаар дуудах");

        var lblCurrent = new Label
        {
            Name = "lblCurrentNumber",
            Text = "Одоогийн дугаар: —",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Location = new Point(20, 20),
            Size = new Size(400, 40),
            ForeColor = Color.FromArgb(0, 102, 204)
        };

        var lblQueue = new Label
        {
            Name = "lblQueueCount",
            Text = "Хүлээж байна: — хүн",
            Font = new Font("Segoe UI", 13),
            Location = new Point(20, 70),
            Size = new Size(400, 30)
        };

        var btnCallNext = new Button
        {
            Text = "▶ ДАРААГИЙН ҮЙЛЧЛҮҮЛЭГЧ",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 153, 51),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(20, 120),
            Size = new Size(360, 60),
            Cursor = Cursors.Hand
        };
        btnCallNext.Click += BtnCallNext_Click;

        var btnRefresh = new Button
        {
            Text = "🔄 Шинэчлэх",
            Font = new Font("Segoe UI", 11),
            Location = new Point(20, 200),
            Size = new Size(150, 38),
            Cursor = Cursors.Hand
        };
        btnRefresh.Click += async (s, e) => await RefreshQueueStatus();

        tab.Controls.AddRange(new Control[] { lblCurrent, lblQueue, btnCallNext, btnRefresh });
        return tab;
    }

    /// <summary>
    /// "Дараагийн үйлчлүүлэгч" дарахад сервер рүү POST явуулна.
    /// Сервер дарааллаас дугаар авч SignalR-ээр дэлгэцүүдэд явуулна.
    /// </summary>
    private async void BtnCallNext_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        btn.Enabled = false;
        try
        {
            var response = await _http.PostAsync($"{ServerUrl}/api/ticket/call-next", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CallNextResponse>();
                var tab = _tabs.TabPages[0];
                ((Label)tab.Controls["lblCurrentNumber"]!).Text =
                    $"Одоогийн дугаар: {result?.CalledNumber:D3}";
                MessageBox.Show($"Дугаар {result?.CalledNumber:D3} дуудагдлаа!");
            }
        }
        catch (Exception ex) { MessageBox.Show($"Алдаа: {ex.Message}"); }
        finally { btn.Enabled = true; }
    }

    /// <summary>Дарааллын одоогийн байдлыг сервераас татна.</summary>
    private async Task RefreshQueueStatus()
    {
        try
        {
            var res = await _http.GetAsync($"{ServerUrl}/api/ticket/status");
            if (!res.IsSuccessStatusCode) return;
            var result = await res.Content.ReadFromJsonAsync<StatusResponse>();
            var tab = _tabs.TabPages[0];
            ((Label)tab.Controls["lblCurrentNumber"]!).Text = $"Одоогийн дугаар: {result?.CurrentNumber:D3}";
            ((Label)tab.Controls["lblQueueCount"]!).Text = $"Хүлээж байна: {result?.QueueCount} хүн";
        }
        catch { }
    }

    // ── Таб 2: Гүйлгээ ──────────────────────────────────────────

    /// <summary>Гүйлгээний табыг байгуулна.</summary>
    private TabPage CreateTransferTab()
    {
        var tab = new TabPage("💳 Гүйлгээ");

        tab.Controls.Add(MakeLabel("Илгээгч данс:", 20, 25));
        tab.Controls.Add(MakeTextBox("txtFrom", 190, 20));
        tab.Controls.Add(MakeLabel("Хүлээн авагч:", 20, 70));
        tab.Controls.Add(MakeTextBox("txtTo", 190, 65));
        tab.Controls.Add(MakeLabel("Дүн (₮):", 20, 115));
        tab.Controls.Add(MakeTextBox("txtAmount", 190, 110));

        var btnTransfer = new Button
        {
            Text = "💸 ГҮЙЛГЭЭ ХИЙХ",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(20, 160),
            Size = new Size(300, 50),
            Cursor = Cursors.Hand
        };
        btnTransfer.Click += BtnTransfer_Click;

        tab.Controls.Add(btnTransfer);
        tab.Controls.Add(new Label
        {
            Name = "lblResult",
            Location = new Point(20, 230),
            Size = new Size(550, 50),
            Font = new Font("Segoe UI", 11)
        });

        return tab;
    }

    /// <summary>Гүйлгээний товч дарахад validation хийж серверт явуулна.</summary>
    private async void BtnTransfer_Click(object? sender, EventArgs e)
    {
        var tab = _tabs.TabPages[1];
        var from = ((TextBox)tab.Controls["txtFrom"]!).Text.Trim();
        var to = ((TextBox)tab.Controls["txtTo"]!).Text.Trim();
        var amtTxt = ((TextBox)tab.Controls["txtAmount"]!).Text.Trim();
        var lbl = (Label)tab.Controls["lblResult"]!;

        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        { MessageBox.Show("Дансны дугаар оруулна уу!"); return; }

        if (!decimal.TryParse(amtTxt, out decimal amount) || amount <= 0)
        { MessageBox.Show("Зөв дүн оруулна уу!"); return; }

        try
        {
            var res = await _http.PostAsJsonAsync($"{ServerUrl}/api/account/transfer",
                new { fromAccount = from, toAccount = to, amount });
            var msg = await res.Content.ReadFromJsonAsync<MessageResponse>();
            lbl.Text = msg?.Message ?? "";
            lbl.ForeColor = res.IsSuccessStatusCode ? Color.Green : Color.Red;
        }
        catch (Exception ex)
        {
            lbl.Text = $"Алдаа: {ex.Message}";
            lbl.ForeColor = Color.Red;
        }
    }

    // ── Таб 3: Ханш ─────────────────────────────────────────────

    /// <summary>Ханш өөрчлөх табыг байгуулна.</summary>
    private TabPage CreateRateTab()
    {
        var tab = new TabPage("💱 Ханш");

        tab.Controls.Add(MakeLabel("Валют:", 20, 25));
        var cmb = new ComboBox
        {
            Name = "cmbCurrency",
            Location = new Point(140, 20),
            Size = new Size(120, 28),
            Font = new Font("Segoe UI", 11),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmb.Items.AddRange(new[] { "USD", "EUR", "CNY", "RUB" });
        cmb.SelectedIndex = 0;
        tab.Controls.Add(cmb);

        tab.Controls.Add(MakeLabel("Авах ханш:", 20, 70));
        tab.Controls.Add(MakeTextBox("txtBuy", 140, 65));
        tab.Controls.Add(MakeLabel("Зарах ханш:", 20, 115));
        tab.Controls.Add(MakeTextBox("txtSell", 140, 110));

        var btnUpdate = new Button
        {
            Text = "📊 ХАНШ ШИНЭЧЛЭХ",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = Color.FromArgb(204, 102, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(20, 160),
            Size = new Size(280, 50),
            Cursor = Cursors.Hand
        };
        btnUpdate.Click += BtnUpdateRate_Click;

        tab.Controls.Add(btnUpdate);
        tab.Controls.Add(new Label
        {
            Name = "lblRateResult",
            Location = new Point(20, 230),
            Size = new Size(500, 40),
            Font = new Font("Segoe UI", 11)
        });

        return tab;
    }

    /// <summary>
    /// Ханш шинэчлэх товч дарахад серверт PUT явуулна.
    /// Сервер SignalR-ээр Blazor дэлгэцэнд автоматаар мэдэгдэнэ.
    /// </summary>
    private async void BtnUpdateRate_Click(object? sender, EventArgs e)
    {
        var tab = _tabs.TabPages[2];
        var currency = ((ComboBox)tab.Controls["cmbCurrency"]!).SelectedItem!.ToString()!;
        var lbl = (Label)tab.Controls["lblRateResult"]!;

        if (!decimal.TryParse(((TextBox)tab.Controls["txtBuy"]!).Text, out decimal buy) ||
            !decimal.TryParse(((TextBox)tab.Controls["txtSell"]!).Text, out decimal sell))
        { MessageBox.Show("Зөв ханш оруулна уу!"); return; }

        try
        {
            var res = await _http.PutAsJsonAsync(
                $"{ServerUrl}/api/exchangerate/{currency}",
                new { buyRate = buy, sellRate = sell });

            lbl.Text = res.IsSuccessStatusCode
                ? $"{currency}: авах {buy}, зарах {sell} — шинэчлэгдлээ!"
                : "Алдаа гарлаа";
            lbl.ForeColor = res.IsSuccessStatusCode ? Color.Green : Color.Red;
        }
        catch (Exception ex)
        {
            lbl.Text = $"Алдаа: {ex.Message}";
        }
    }

    // ── Туслах методууд ─────────────────────────────────────────

    /// <summary>Давтагдах Label үүсгэх туслах метод.</summary>
    private static Label MakeLabel(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(160, 25),
        Font = new Font("Segoe UI", 11)
    };

    /// <summary>Давтагдах TextBox үүсгэх туслах метод.</summary>
    private static TextBox MakeTextBox(string name, int x, int y) => new()
    {
        Name = name,
        Location = new Point(x, y),
        Size = new Size(200, 28),
        Font = new Font("Segoe UI", 11)
    };
}

record CallNextResponse(int CalledNumber);
record StatusResponse(int CurrentNumber, int QueueCount);
record MessageResponse(string Message);