using BankSystem.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TellerApp;

/// <summary>
/// Теллерийн апп — Хаан банкны дизайн.
/// Таб 1: Дараагийн үйлчлүүлэгч дуудах (RoomId-тай)
/// Таб 2: А → Б мөнгөн гүйлгээ
/// Таб 3: Валютын ханш шинэчлэх
/// Shared.DTOs.CallNextResponse ашиглана — Form1 дотор давхар тодорхойлохгүй.
/// </summary>
public partial class Form1 : Form
{
    // ── Тохиргоо ──────────────────────────────────────────────────────────
    private static readonly IConfiguration _cfg =
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

    private static readonly string ServerUrl =
        _cfg["ServerUrl"] ?? "http://localhost:5000";

    /// <summary>
    /// RoomId — appsettings.json-аас авна.
    /// Компьютер Б: "305", Компьютер В: "306"
    /// SocketServer энэ утгаар зөвхөн тухайн дэлгэцэнд TCP явуулна.
    /// </summary>
    private static readonly string RoomId =
        _cfg["RoomId"] ?? "000";

    private static readonly HttpClient _http = new();

    // ── Хаан банкны өнгөний палитр ────────────────────────────────────────
    private static readonly Color KhanDark = Color.FromArgb(10, 70, 35);
    private static readonly Color KhanGreen = Color.FromArgb(20, 120, 60);
    private static readonly Color KhanLight = Color.FromArgb(230, 247, 237);
    private static readonly Color KhanGold = Color.FromArgb(180, 140, 30);
    private static readonly Color KhanRed = Color.FromArgb(200, 40, 40);
    private static readonly Color KhanText = Color.FromArgb(30, 30, 30);

    private TabControl _tabs = null!;

    public Form1()
    {
        InitializeComponent();
        BuildUI();
    }

    // ── UI байгуулалт ─────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text = $"Теллерийн Апп — Хаан Банк  |  Цонх {RoomId}";
        Size = new Size(760, 600);
        MinimumSize = new Size(700, 540);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = KhanLight;
        Font = new Font("Segoe UI", 10);

        // ── Header ────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = KhanDark
        };
        header.Controls.Add(new Label
        {
            Text = "ХААН БАНК",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 14),
            AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = $"Цонх  {RoomId}",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.FromArgb(180, 230, 200),
            AutoSize = true,
            Location = new Point(630, 22)
        });

        // ── Tabs ──────────────────────────────────────────────────────────
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11),
            Padding = new Point(20, 8)
        };
        _tabs.TabPages.Add(BuildQueueTab());
        _tabs.TabPages.Add(BuildTransferTab());
        _tabs.TabPages.Add(BuildRateTab());

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        body.Controls.Add(_tabs);

        Controls.Add(body);
        Controls.Add(header);
    }

    // ── Таб 1: Дугаар дуудах ─────────────────────────────────────────────

    private TabPage BuildQueueTab()
    {
        var tab = new TabPage("  Дугаар дуудах  ") { BackColor = Color.White };

        var card = MakeCard(20, 16, 690, 230);

        card.Controls.Add(new Label
        {
            Text = "Дараагийн үйлчлүүлэгчийг дуудах",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = KhanDark,
            Location = new Point(20, 16),
            AutoSize = true
        });

        card.Controls.Add(new Label
        {
            Name = "lblCurrentNumber",
            Text = "Одоогийн дугаар:  —",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = KhanGreen,
            Location = new Point(20, 52),
            Size = new Size(500, 50),
            AutoSize = false
        });

        card.Controls.Add(new Label
        {
            Name = "lblQueueCount",
            Text = "Хүлээж байна:  — хүн",
            Font = new Font("Segoe UI", 13),
            ForeColor = KhanText,
            Location = new Point(20, 108),
            Size = new Size(400, 28)
        });

        card.Controls.Add(new Label
        {
            Text = $"Таны цонх:  {RoomId}",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            Location = new Point(20, 143),
            AutoSize = true
        });

        var btnCall = MakeBtn("ДАРААГИЙН ҮЙЛЧЛҮҮЛЭГЧ", 20, 174, 320, 50, KhanGreen);
        btnCall.Click += BtnCallNext_Click;

        var btnRefresh = MakeBtn("Шинэчлэх", 355, 174, 130, 50, KhanDark);
        btnRefresh.Font = new Font("Segoe UI", 10);
        btnRefresh.Click += async (s, e) => await RefreshStatusAsync();

        card.Controls.AddRange(new Control[] { btnCall, btnRefresh });
        tab.Controls.Add(card);
        return tab;
    }

    /// <summary>
    /// "Дараагийн үйлчлүүлэгч" дарахад серверт POST явуулна.
    /// roomId query param — SocketServer зөвхөн тухайн дэлгэцэнд TCP явуулна.
    /// Shared.DTOs.CallNextResponse ашиглана: TicketNumber, RemainingCount.
    /// </summary>
    private async void BtnCallNext_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        btn.Enabled = false;
        btn.BackColor = KhanDark;
        try
        {
            var res = await _http.PostAsync(
                $"{ServerUrl}/api/ticket/call-next?roomId={RoomId}", null);

            if (res.IsSuccessStatusCode)
            {
                // Shared.DTOs.CallNextResponse — TicketNumber талбар
                var result = await res.Content
                    .ReadFromJsonAsync<CallNextResponse>();

                var tab = _tabs.TabPages[0];
                ((Label)tab.Controls["lblCurrentNumber"]!).Text =
                    $"Одоогийн дугаар:  {result?.TicketNumber:D3}";
                ((Label)tab.Controls["lblQueueCount"]!).Text =
                    $"Үлдсэн хүлээлт:  {result?.RemainingCount} хүн";
            }
            else
            {
                ShowMsg("Сервер хариу өгсөнгүй", KhanRed);
            }
        }
        catch (Exception ex) { ShowMsg($"Алдаа: {ex.Message}", KhanRed); }
        finally
        {
            btn.Enabled = true;
            btn.BackColor = KhanGreen;
        }
    }

    /// <summary>Дарааллын байдлыг серверээс шинэчлэн авна.</summary>
    private async Task RefreshStatusAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{ServerUrl}/api/ticket/status");
            if (!res.IsSuccessStatusCode) return;

            // StatusResponse — QueueStatusDto-г deserialize хийнэ
            var r = await res.Content.ReadFromJsonAsync<StatusResponse>();
            var tab = _tabs.TabPages[0];
            ((Label)tab.Controls["lblCurrentNumber"]!).Text =
                $"Одоогийн дугаар:  {r?.CurrentNumber:D3}";
            ((Label)tab.Controls["lblQueueCount"]!).Text =
                $"Хүлээж байна:  {r?.QueueCount} хүн";
        }
        catch { }
    }

    // ── Таб 2: Гүйлгээ ───────────────────────────────────────────────────

    private TabPage BuildTransferTab()
    {
        var tab = new TabPage("  Гүйлгээ  ") { BackColor = Color.White };
        var card = MakeCard(20, 16, 690, 310);

        card.Controls.Add(new Label
        {
            Text = "Мөнгөн гүйлгээ",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = KhanDark,
            Location = new Point(20, 16),
            AutoSize = true
        });

        // Мөр тус бүрийг байгуулна
        AddFormRow(card, "Илгээгч данс:", "txtFrom", 20, 56);
        AddFormRow(card, "Хүлээн авагч:", "txtTo", 20, 104);
        AddFormRow(card, "Дүн (₮):", "txtAmount", 20, 152);

        var btnTransfer = MakeBtn("ГҮЙЛГЭЭ ХИЙХ", 20, 206, 260, 50, KhanGreen);
        btnTransfer.Click += BtnTransfer_Click;

        card.Controls.Add(btnTransfer);
        card.Controls.Add(new Label
        {
            Name = "lblResult",
            Location = new Point(20, 270),
            Size = new Size(640, 34),
            Font = new Font("Segoe UI", 11)
        });

        tab.Controls.Add(card);
        return tab;
    }

    /// <summary>
    /// Гүйлгээний товч. Клиент талд validation хийнэ:
    ///   — Хоосон данс
    ///   — Ижил данс
    ///   — Буруу дүн
    /// Серверийн алдааг (үлдэгдэл хүрэлцэхгүй гэх мэт) хэрэглэгчид харуулна.
    /// </summary>
    private async void BtnTransfer_Click(object? sender, EventArgs e)
    {
        var card = ((Control)sender!).Parent!;
        var from = GetText(card, "txtFrom");
        var to = GetText(card, "txtTo");
        var amtS = GetText(card, "txtAmount");
        var lbl = (Label)card.Controls["lblResult"]!;

        // ── Клиент талын validation ────────────────────────────────────
        if (string.IsNullOrEmpty(from))
        { SetLbl(lbl, "Илгээгч дансны дугаар оруулна уу!", KhanRed); return; }

        if (string.IsNullOrEmpty(to))
        { SetLbl(lbl, "Хүлээн авагч дансны дугаар оруулна уу!", KhanRed); return; }

        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        { SetLbl(lbl, "Илгээгч болон хүлээн авагч данс ижил байж болохгүй!", KhanRed); return; }

        if (!decimal.TryParse(amtS, out decimal amount) || amount <= 0)
        { SetLbl(lbl, "Зөв дүн оруулна уу! (тоо, 0-ээс их)", KhanRed); return; }

        try
        {
            // TransferRequest — Shared.DTOs-аас авна
            var res = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api/account/transfer",
                new { fromAccountNumber = from, toAccountNumber = to, amount });

            // MessageResponse — серверийн хариуг deserialize хийнэ
            var msg = await res.Content.ReadFromJsonAsync<MessageResponse>();
            SetLbl(lbl, msg?.Message ?? "",
                res.IsSuccessStatusCode ? KhanGreen : KhanRed);
        }
        catch (Exception ex)
        {
            SetLbl(lbl, $"Сервертэй холбогдох үед алдаа: {ex.Message}", KhanRed);
        }
    }

    // ── Таб 3: Ханш ──────────────────────────────────────────────────────

    private TabPage BuildRateTab()
    {
        var tab = new TabPage("  Ханш  ") { BackColor = Color.White };
        var card = MakeCard(20, 16, 690, 280);

        card.Controls.Add(new Label
        {
            Text = "Валютын ханш шинэчлэх",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = KhanDark,
            Location = new Point(20, 16),
            AutoSize = true
        });

        card.Controls.Add(new Label
        {
            Text = "Валют:",
            Font = new Font("Segoe UI", 11),
            Location = new Point(20, 62),
            AutoSize = true
        });

        var cmb = new ComboBox
        {
            Name = "cmbCurrency",
            Location = new Point(170, 58),
            Size = new Size(130, 30),
            Font = new Font("Segoe UI", 11),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = KhanLight
        };
        cmb.Items.AddRange(new[] { "USD", "EUR", "CNY", "RUB" });
        cmb.SelectedIndex = 0;
        card.Controls.Add(cmb);

        AddFormRow(card, "Авах ханш:", "txtBuy", 20, 104);
        AddFormRow(card, "Зарах ханш:", "txtSell", 20, 152);

        var btnUpdate = MakeBtn("ХАНШ ШИНЭЧЛЭХ", 20, 206, 260, 50, KhanGold);
        btnUpdate.Click += BtnUpdateRate_Click;

        card.Controls.Add(btnUpdate);
        card.Controls.Add(new Label
        {
            Name = "lblRateResult",
            Location = new Point(20, 268),
            Size = new Size(640, 34),
            Font = new Font("Segoe UI", 11)
        });

        tab.Controls.Add(card);
        return tab;
    }

    /// <summary>
    /// Ханш шинэчлэх товч. Validation:
    ///   — Зарах ханш авах ханшаас их байх ёстой.
    /// UpdateRateRequest — Shared.DTOs-аас авна.
    /// </summary>
    private async void BtnUpdateRate_Click(object? sender, EventArgs e)
    {
        var card = ((Control)sender!).Parent!;
        var currency = ((ComboBox)card.Controls["cmbCurrency"]!).SelectedItem!.ToString()!;
        var lbl = (Label)card.Controls["lblRateResult"]!;

        if (!decimal.TryParse(GetText(card, "txtBuy"), out decimal buy) ||
            !decimal.TryParse(GetText(card, "txtSell"), out decimal sell))
        { SetLbl(lbl, "Зөв ханш оруулна уу!", KhanRed); return; }

        if (sell <= buy)
        { SetLbl(lbl, "Зарах ханш авах ханшаас их байх ёстой!", KhanRed); return; }

        try
        {
            // UpdateRateRequest — Shared.DTOs-аас авна
            var res = await _http.PutAsJsonAsync(
                $"{ServerUrl}/api/exchangerate/{currency}",
                new { buyRate = buy, sellRate = sell });

            SetLbl(lbl,
                res.IsSuccessStatusCode
                    ? $"{currency}: авах {buy:N0}₮,  зарах {sell:N0}₮ — шинэчлэгдлээ!"
                    : "Серверт алдаа гарлаа",
                res.IsSuccessStatusCode ? KhanGreen : KhanRed);
        }
        catch (Exception ex)
        {
            SetLbl(lbl, $"Алдаа: {ex.Message}", KhanRed);
        }
    }

    // ── Дизайн туслах методууд ────────────────────────────────────────────

    /// <summary>Карт хэлбэрийн дэвсгэр Panel үүсгэнэ.</summary>
    private static Panel MakeCard(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.White
        };
        p.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(200, 220, 210));
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    /// <summary>Хаан банкны дизайны товч үүсгэнэ.</summary>
    private static Button MakeBtn(string text, int x, int y, int w, int h, Color bg)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(w, h),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    /// <summary>Label + TextBox хос мөр үүсгэж parent-д нэмнэ.</summary>
    private static void AddFormRow(Panel parent, string labelText,
                                    string boxName, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 11),
            Location = new Point(x, y + 6),
            Size = new Size(148, 24)
        });
        parent.Controls.Add(new TextBox
        {
            Name = boxName,
            Location = new Point(x + 152, y + 2),
            Size = new Size(220, 30),
            Font = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(240, 250, 244)
        });
    }

    /// <summary>TextBox утгыг хайна — Panel дотор байх тул рекурсив хайна.</summary>
    private static string GetText(Control parent, string name)
    {
        foreach (Control c in parent.Controls)
        {
            if (c.Name == name) return c.Text.Trim();
        }
        return "";
    }

    private static void SetLbl(Label lbl, string text, Color color)
    {
        lbl.Text = text;
        lbl.ForeColor = color;
    }

    private static void ShowMsg(string msg, Color color) =>
        MessageBox.Show(msg, "Мэдэгдэл",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
}

// ── Form1 дотор ашиглах жижиг DTOs ───────────────────────────────────────

/// <summary>
/// GET /api/ticket/status хариуг deserialize хийнэ.
/// QueueStatusDto-г тусгайлан reference хийхгүйн тулд Form1 дотор тодорхойлно.
/// </summary>
record StatusResponse(int CurrentNumber, int QueueCount);

/// <summary>
/// Серверийн Message талбартай JSON хариуг deserialize хийнэ.
/// TransferResponse-г шууд ашиглах боломжтой боловч Success+Message хоёуланг
/// хянах хэрэггүй тул энгийн record ашиглана.
/// </summary>
record MessageResponse(string? Message);

// CallNextResponse — Shared.DTOs-аас авна, энд давхар тодорхойлохгүй