using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TellerApp;

public partial class Form1 : Form
{
    private static readonly HttpClient _http = new();
    private static readonly IConfiguration _config =
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

    private static readonly string ServerUrl = _config["ServerUrl"] ?? "http://localhost:5000";
    // RoomId = "305" эсвэл "306" — appsettings.json-д тохируулна
    // SocketServer энэ утгаар зөвхөн тухайн дэлгэцэнд TCP явуулна
    private static readonly string RoomId    = _config["RoomId"]    ?? "000";
    private static readonly string TellerName = $"Цонх {RoomId}";

    private TabControl _tabs = null!;

    public Form1()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text          = $"Теллерийн Апп — {TellerName}";
        Size          = new Size(700, 560);
        StartPosition = FormStartPosition.CenterScreen;

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(CreateQueueTab());
        _tabs.TabPages.Add(CreateTransferTab());
        _tabs.TabPages.Add(CreateRateTab());
        Controls.Add(_tabs);
    }

    // ── Таб 1: Дугаар дуудах ─────────────────────────────────
    private TabPage CreateQueueTab()
    {
        var tab = new TabPage("Дугаар дуудах");

        var lblCurrent = new Label
        {
            Name      = "lblCurrentNumber",
            Text      = "Одоогийн дугаар: —",
            Font      = new Font("Segoe UI", 16, FontStyle.Bold),
            Location  = new Point(20, 20),
            Size      = new Size(400, 40),
            ForeColor = Color.FromArgb(0, 102, 204)
        };
        var lblQueue = new Label
        {
            Name     = "lblQueueCount",
            Text     = "Хүлээж байна: — хүн",
            Font     = new Font("Segoe UI", 13),
            Location = new Point(20, 70),
            Size     = new Size(500, 30)
        };
        var lblTeller = new Label
        {
            Text      = $"Теллер: {TellerName}  (RoomId: {RoomId})",
            Font      = new Font("Segoe UI", 11),
            Location  = new Point(20, 105),
            Size      = new Size(500, 28),
            ForeColor = Color.Gray
        };
        var btnCallNext = new Button
        {
            Text      = "ДАРААГИЙН ҮЙЛЧЛҮҮЛЭГЧ",
            Font      = new Font("Segoe UI", 14, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 153, 51),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(20, 145),
            Size      = new Size(360, 60),
            Cursor    = Cursors.Hand
        };
        btnCallNext.Click += BtnCallNext_Click;

        var btnRefresh = new Button
        {
            Text     = "Шинэчлэх",
            Font     = new Font("Segoe UI", 11),
            Location = new Point(20, 225),
            Size     = new Size(150, 38),
            Cursor   = Cursors.Hand
        };
        btnRefresh.Click += async (s, e) => await RefreshQueueStatus();

        tab.Controls.AddRange(new Control[]
            { lblCurrent, lblQueue, lblTeller, btnCallNext, btnRefresh });
        return tab;
    }

    private async void BtnCallNext_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        btn.Enabled = false;
        try
        {
            // RoomId-г query param-аар явуулна
            // SocketServer үүнийг ашиглан зөвхөн тухайн дэлгэцэнд TCP явуулна
            var response = await _http.PostAsync(
                $"{ServerUrl}/api/ticket/call-next?roomId={RoomId}", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<CallNextResponse>();
                var tab    = _tabs.TabPages[0];
                ((Label)tab.Controls["lblCurrentNumber"]!).Text =
                    $"Одоогийн дугаар: {result?.CalledNumber:D3}";
                ((Label)tab.Controls["lblQueueCount"]!).Text =
                    $"{TellerName} → Дугаар {result?.CalledNumber:D3} дуудагдлаа";
            }
        }
        catch (Exception ex) { MessageBox.Show($"Алдаа: {ex.Message}"); }
        finally { btn.Enabled = true; }
    }

    private async Task RefreshQueueStatus()
    {
        try
        {
            var res = await _http.GetAsync($"{ServerUrl}/api/ticket/status");
            if (!res.IsSuccessStatusCode) return;
            var result = await res.Content.ReadFromJsonAsync<StatusResponse>();
            var tab    = _tabs.TabPages[0];
            ((Label)tab.Controls["lblCurrentNumber"]!).Text =
                $"Одоогийн дугаар: {result?.CurrentNumber:D3}";
            ((Label)tab.Controls["lblQueueCount"]!).Text =
                $"Хүлээж байна: {result?.QueueCount} хүн";
        }
        catch { }
    }

    // ── Таб 2: Гүйлгээ ───────────────────────────────────────
    private TabPage CreateTransferTab()
    {
        var tab = new TabPage("Гүйлгээ");

        tab.Controls.Add(MakeLabel("Илгээгч данс:", 20, 25));
        tab.Controls.Add(MakeTextBox("txtFrom", 190, 20));
        tab.Controls.Add(MakeLabel("Хүлээн авагч:", 20, 70));
        tab.Controls.Add(MakeTextBox("txtTo", 190, 65));
        tab.Controls.Add(MakeLabel("Дүн:", 20, 115));
        tab.Controls.Add(MakeTextBox("txtAmount", 190, 110));

        var btnTransfer = new Button
        {
            Text      = "ГҮЙЛГЭЭ ХИЙХ",
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 102, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(20, 160),
            Size      = new Size(300, 50),
            Cursor    = Cursors.Hand
        };
        btnTransfer.Click += BtnTransfer_Click;
        tab.Controls.Add(btnTransfer);
        tab.Controls.Add(new Label
        {
            Name     = "lblResult",
            Location = new Point(20, 230),
            Size     = new Size(600, 60),
            Font     = new Font("Segoe UI", 11)
        });
        return tab;
    }

    private async void BtnTransfer_Click(object? sender, EventArgs e)
    {
        var tab    = _tabs.TabPages[1];
        var from   = ((TextBox)tab.Controls["txtFrom"]!).Text.Trim();
        var to     = ((TextBox)tab.Controls["txtTo"]!).Text.Trim();
        var amtTxt = ((TextBox)tab.Controls["txtAmount"]!).Text.Trim();
        var lbl    = (Label)tab.Controls["lblResult"]!;

        if (string.IsNullOrEmpty(from))
        { lbl.Text = "Илгээгч данс оруулна уу!"; lbl.ForeColor = Color.OrangeRed; return; }
        if (string.IsNullOrEmpty(to))
        { lbl.Text = "Хүлээн авагч данс оруулна уу!"; lbl.ForeColor = Color.OrangeRed; return; }
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        { lbl.Text = "Илгээгч болон хүлээн авагч данс ижил байж болохгүй!"; lbl.ForeColor = Color.OrangeRed; return; }
        if (!decimal.TryParse(amtTxt, out decimal amount) || amount <= 0)
        { lbl.Text = "Зөв дүн оруулна уу!"; lbl.ForeColor = Color.OrangeRed; return; }

        try
        {
            var res = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api/account/transfer",
                new { fromAccount = from, toAccount = to, amount });
            var msg = await res.Content.ReadFromJsonAsync<MessageResponse>();
            lbl.Text      = msg?.Message ?? "";
            lbl.ForeColor = res.IsSuccessStatusCode ? Color.Green : Color.Red;
        }
        catch (Exception ex) { lbl.Text = $"Алдаа: {ex.Message}"; lbl.ForeColor = Color.Red; }
    }

    // ── Таб 3: Ханш ──────────────────────────────────────────
    private TabPage CreateRateTab()
    {
        var tab = new TabPage("Ханш");
        tab.Controls.Add(MakeLabel("Валют:", 20, 25));
        var cmb = new ComboBox
        {
            Name          = "cmbCurrency",
            Location      = new Point(140, 20),
            Size          = new Size(120, 28),
            Font          = new Font("Segoe UI", 11),
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
            Text      = "ХАНШ ШИНЭЧЛЭХ",
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = Color.FromArgb(204, 102, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(20, 160),
            Size      = new Size(280, 50),
            Cursor    = Cursors.Hand
        };
        btnUpdate.Click += BtnUpdateRate_Click;
        tab.Controls.Add(btnUpdate);
        tab.Controls.Add(new Label
        {
            Name     = "lblRateResult",
            Location = new Point(20, 230),
            Size     = new Size(500, 40),
            Font     = new Font("Segoe UI", 11)
        });
        return tab;
    }

    private async void BtnUpdateRate_Click(object? sender, EventArgs e)
    {
        var tab      = _tabs.TabPages[2];
        var currency = ((ComboBox)tab.Controls["cmbCurrency"]!).SelectedItem!.ToString()!;
        var lbl      = (Label)tab.Controls["lblRateResult"]!;

        if (!decimal.TryParse(((TextBox)tab.Controls["txtBuy"]!).Text, out decimal buy) ||
            !decimal.TryParse(((TextBox)tab.Controls["txtSell"]!).Text, out decimal sell))
        { lbl.Text = "Зөв ханш оруулна уу!"; lbl.ForeColor = Color.OrangeRed; return; }

        if (sell <= buy)
        { lbl.Text = "Зарах ханш авах ханшаас их байх ёстой!"; lbl.ForeColor = Color.OrangeRed; return; }

        try
        {
            var res = await _http.PutAsJsonAsync(
                $"{ServerUrl}/api/exchangerate/{currency}",
                new { buyRate = buy, sellRate = sell });
            lbl.Text      = res.IsSuccessStatusCode
                ? $"{currency}: авах {buy:N0}, зарах {sell:N0} — шинэчлэгдлээ!"
                : "Алдаа гарлаа";
            lbl.ForeColor = res.IsSuccessStatusCode ? Color.Green : Color.Red;
        }
        catch (Exception ex) { lbl.Text = $"Алдаа: {ex.Message}"; lbl.ForeColor = Color.Red; }
    }

    private static Label MakeLabel(string text, int x, int y) => new()
    { Text = text, Location = new Point(x, y), Size = new Size(160, 25), Font = new Font("Segoe UI", 11) };

    private static TextBox MakeTextBox(string name, int x, int y) => new()
    { Name = name, Location = new Point(x, y), Size = new Size(200, 28), Font = new Font("Segoe UI", 11) };
}

record CallNextResponse(int CalledNumber);
record StatusResponse(int CurrentNumber, int QueueCount);
record MessageResponse(string Message);
