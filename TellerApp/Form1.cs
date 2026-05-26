using BankSystem.Shared.DTOs.Requests;
using BankSystem.Shared.DTOs.Responses;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TellerApp;

/// <summary>
/// Теллерийн апп — Хэрэглэгчийн Банк дизайн.
///
/// Таб 1: Дараагийн үйлчлүүлэгч дуудах — stat boxes, queue байдал
/// Таб 2: А → Б мөнгөн гүйлгээ — validation, button disable, result
/// Таб 3: Валютын ханш шинэчлэх — UpdatedBy дамжуулна
///
/// Дизайн шийдвэрүүд:
///   - TabControl-г custom Panel tab bar-аар солисон:
///     TabControl-г WinForms-д өнгөлж чадахгүй (system-drawn).
///   - Stat boxes: plain label-ийн оронд colored panel
///   - Button.Enabled = false: давхар хүсэлтээс сэргийлнэ
///   - ILogger-г ашиглахын оронд Console — WinForms-д DI хялбар биш
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
    /// SocketServer энэ утгаар зөвхөн тухайн дэлгэцэнд TCP явуулна.
    /// </summary>
    private static readonly string RoomId = _cfg["RoomId"] ?? "000";

    private static readonly HttpClient _http = new();

    // ── Хэрэглэгчийн Банк өнгөний палитр ─────────────────────────────────
    private static readonly Color HBNavy   = Color.FromArgb(11,  44, 74);   // #0B2C4A — header
    private static readonly Color HBBlue   = Color.FromArgb(21, 101,192);   // #1565C0 — товч
    private static readonly Color HBSky    = Color.FromArgb(227,240,255);   // #E3F0FF — bg, stat
    private static readonly Color HBBorder = Color.FromArgb(187,222,251);   // #BBDEFB — хүрээ
    private static readonly Color HBGold   = Color.FromArgb(200,148, 10);   // #C8940A — ханш товч
    private static readonly Color HBGreen  = Color.FromArgb( 27,127, 58);   // #1B7F3A — амжилт
    private static readonly Color HBRed    = Color.FromArgb(192, 57, 43);   // #C0392B — алдаа
    private static readonly Color HBText   = Color.FromArgb( 26, 26, 46);   // #1A1A2E — текст
    private static readonly Color HBSub    = Color.FromArgb(100,116,139);   // #64748B — дэд текст

    // ── Custom tab state ──────────────────────────────────────────────────
    private Panel _contentPanel = null!;
    private Button[] _tabBtns   = null!;
    private Panel[]  _tabPanes  = null!;
    private int      _activeTab = 0;

    public Form1()
    {
        InitializeComponent();
        BuildUI();
    }

    // ══════════════════════════════════════════════════════════════════════
    // UI байгуулалт
    // ══════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        Text            = $"Хэрэглэгчийн Банк  |  Теллерийн Систем  |  Цонх {RoomId}";
        Size            = new Size(780, 620);
        MinimumSize     = new Size(720, 560);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Color.FromArgb(245, 249, 255);
        Font            = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        // ── Header ────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 60,
            BackColor = HBNavy
        };

        // Лого хэсэг
        var logoPanel = new Panel
        {
            Location  = new Point(16, 10),
            Size      = new Size(40, 40),
            BackColor = Color.White
        };
        logoPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen  = new Pen(HBNavy, 2f);
            using var pen2 = new Pen(HBBlue, 2f);
            // Хэрэглэгчийн Банк лого — дугуй + зураас
            e.Graphics.DrawEllipse(pen, 4, 4, 32, 32);
            e.Graphics.DrawLine(pen2, 20, 8, 20, 32);
            e.Graphics.DrawLine(pen2, 8, 20, 32, 20);
        };

        header.Controls.Add(logoPanel);
        header.Controls.Add(new Label
        {
            Text      = "ХЭРЭГЛЭГЧИЙН БАНК",
            Font      = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            Location  = new Point(64, 16),
            AutoSize  = true
        });
        header.Controls.Add(new Label
        {
            Text      = "Teller Banking System",
            Font      = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(160, 200, 240),
            Location  = new Point(66, 38),
            AutoSize  = true
        });
        header.Controls.Add(new Label
        {
            Text      = $"Цонх  {RoomId}",
            Font      = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(160, 200, 240),
            Location  = new Point(660, 20),
            AutoSize  = true
        });

        // ── Custom Tab Bar ────────────────────────────────────────────────
        // TabControl-г ашиглахгүй — system-drawn тул өнгөлж чадахгүй.
        // Энгийн Panel + Button хосолбол бүрэн хянах боломжтой.
        var tabBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = Color.FromArgb(21, 60, 100)
        };

        string[] tabNames = { "  Дугаар дуудах  ", "  Гүйлгээ  ", "  Ханш  " };
        _tabBtns = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text      = tabNames[i],
                Location  = new Point(i * 170, 0),
                Size      = new Size(170, 44),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(160, 200, 240),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize     = 0;
            btn.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(30, 255, 255, 255);
            btn.Click += (s, e) => SwitchTab(idx);
            _tabBtns[i] = btn;
            tabBar.Controls.Add(btn);
        }

        // ── Content area ──────────────────────────────────────────────────
        _contentPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 249, 255),
            Padding   = new Padding(16)
        };

        // Tab агуулгыг байгуулна
        _tabPanes = new Panel[]
        {
            BuildQueuePane(),
            BuildTransferPane(),
            BuildRatePane()
        };

        foreach (var pane in _tabPanes)
        {
            pane.Dock    = DockStyle.Fill;
            pane.Visible = false;
            _contentPanel.Controls.Add(pane);
        }

        Controls.Add(_contentPanel);
        Controls.Add(tabBar);
        Controls.Add(header);

        SwitchTab(0);
    }

    /// <summary>
    /// Tab солигдоход идэвхтэй tab-г тодруулж агуулгыг харуулна.
    /// </summary>
    private void SwitchTab(int idx)
    {
        _activeTab = idx;
        for (int i = 0; i < _tabBtns.Length; i++)
        {
            bool active = (i == idx);
            _tabBtns[i].ForeColor  = active ? Color.White
                                             : Color.FromArgb(160, 200, 240);
            _tabBtns[i].BackColor  = active
                ? Color.FromArgb(21, 101, 192)
                : Color.Transparent;
            _tabBtns[i].Font       = new Font("Segoe UI",
                active ? 10 : 10,
                active ? FontStyle.Bold : FontStyle.Regular);
            _tabPanes[i].Visible   = active;
            _tabPanes[i].BringToFront();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Таб 1: Дугаар дуудах
    // ══════════════════════════════════════════════════════════════════════

    private Panel BuildQueuePane()
    {
        var pane = new Panel { BackColor = Color.Transparent };

        var card = MakeCard(0, 0, 720, 260);

        AddCardTitle(card, "Дараагийн үйлчлүүлэгчийг дуудах", 18, 18);

        // ── Stat boxes ────────────────────────────────────────────────────
        // Plain label-ийн оронд colored box ашиглана — харагцуу, мэргэжлийн
        var statCurrent = MakeStatBox("ОДООГИЙН ДУГААР", "lblCurrentNum", 18, 68);
        var statQueue   = MakeStatBox("ХҮЛЭЭЖ БАЙНА",   "lblQueueCount", 240, 68);

        card.Controls.Add(statCurrent);
        card.Controls.Add(statQueue);

        // Цонхны мэдэгдэл
        card.Controls.Add(new Label
        {
            Text      = $"Таны цонх:  {RoomId}",
            Font      = new Font("Segoe UI", 9),
            ForeColor = HBSub,
            Location  = new Point(18, 158),
            AutoSize  = true
        });

        // ── Товчнууд ──────────────────────────────────────────────────────
        var btnCall = MakeBtn("▶  Дараагийн үйлчлүүлэгч", 18, 186, 320, 50, HBBlue);
        btnCall.Name   = "btnCallNext";
        btnCall.Click += BtnCallNext_Click;

        var btnRefresh = MakeBtn("↻ Шинэчлэх", 352, 186, 130, 50, HBNavy);
        btnRefresh.Font  = new Font("Segoe UI", 10);
        btnRefresh.Click += async (s, e) => await RefreshStatusAsync();

        card.Controls.AddRange(new Control[] { btnCall, btnRefresh });
        pane.Controls.Add(card);
        return pane;
    }

    /// <summary>
    /// "Дараагийн үйлчлүүлэгч" дарахад серверт POST явуулна.
    /// ЗАСВАР: btn.Enabled = false — давхар хүсэлтээс сэргийлнэ.
    /// roomId query param — SocketServer зөвхөн тухайн өрөөнд TCP явуулна.
    /// </summary>
    private async void BtnCallNext_Click(object? sender, EventArgs e)
    {
        var btn = (Button)sender!;
        btn.Enabled  = false;
        btn.Text     = "Уншиж байна...";
        btn.BackColor = HBNavy;
        try
        {
            var res = await _http.PostAsync(
                $"{ServerUrl}/api/ticket/call-next?roomId={RoomId}", null);

            if (res.IsSuccessStatusCode)
            {
                var result = await res.Content
                    .ReadFromJsonAsync<CallNextResponse>();

                UpdateStatBox(_tabPanes[0], "lblCurrentNum",
                    result?.TicketNumber.ToString("D3") ?? "---");
                UpdateStatBox(_tabPanes[0], "lblQueueCount",
                    $"{result?.RemainingCount ?? 0} хүн");
            }
            else
            {
                ShowStatusMsg("Сервер хариу өгсөнгүй", HBRed);
            }
        }
        catch (Exception ex)
        {
            ShowStatusMsg($"Алдаа: {ex.Message}", HBRed);
        }
        finally
        {
            btn.Enabled   = true;
            btn.Text      = "▶  Дараагийн үйлчлүүлэгч";
            btn.BackColor = HBBlue;
        }
    }

    /// <summary>Дарааллын байдлыг сервераас шинэчлэн авна.</summary>
    private async Task RefreshStatusAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{ServerUrl}/api/ticket/status");
            if (!res.IsSuccessStatusCode) return;
            var r = await res.Content.ReadFromJsonAsync<StatusResponse>();
            UpdateStatBox(_tabPanes[0], "lblCurrentNum",
                r?.CurrentNumber.ToString("D3") ?? "---");
            UpdateStatBox(_tabPanes[0], "lblQueueCount",
                $"{r?.QueueCount ?? 0} хүн");
        }
        catch { }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Таб 2: Гүйлгээ
    // ══════════════════════════════════════════════════════════════════════

    private Panel BuildTransferPane()
    {
        var pane = new Panel { BackColor = Color.Transparent };
        var card = MakeCard(0, 0, 720, 330);

        AddCardTitle(card, "Мөнгөн гүйлгээ", 18, 18);

        // Form мөрүүд
        AddFormRow(card, "Илгээгч данс:",  "txtFrom",   18, 58);
        AddFormRow(card, "Хүлээн авагч:",  "txtTo",     18, 106);
        AddFormRow(card, "Дүн (₮):",       "txtAmount", 18, 154);

        // Туслах текст
        card.Controls.Add(new Label
        {
            Text      = "Жишээ: ACC001, ACC002, ACC003",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = HBSub,
            Location  = new Point(186, 84),
            AutoSize  = true
        });

        var btnTransfer = MakeBtn("Гүйлгээ хийх", 18, 206, 240, 50, HBBlue);
        btnTransfer.Name   = "btnTransfer";
        btnTransfer.Click += BtnTransfer_Click;

        // Тасалбарын теллер ID автоматаар дамжуулагдана
        card.Controls.Add(new Label
        {
            Text      = $"Теллер: Цонх {RoomId}",
            Font      = new Font("Segoe UI", 9),
            ForeColor = HBSub,
            Location  = new Point(272, 222),
            AutoSize  = true
        });

        card.Controls.Add(btnTransfer);

        // Үр дүнгийн мэдэгдэл
        var lblResult = new Label
        {
            Name      = "lblResult",
            Text      = "",
            Location  = new Point(18, 270),
            Size      = new Size(680, 40),
            Font      = new Font("Segoe UI", 11)
        };
        card.Controls.Add(lblResult);

        pane.Controls.Add(card);
        return pane;
    }

    /// <summary>
    /// Гүйлгээний товч.
    /// ЗАСВАР: btn.Enabled = false — давхар хүсэлт илгээхээс сэргийлнэ.
    /// ЗАСВАР: TellerWindowId = RoomId дамжуулагдана — аудит.
    /// </summary>
    private async void BtnTransfer_Click(object? sender, EventArgs e)
    {
        var btn  = (Button)sender!;
        var card = btn.Parent!;
        var from   = GetText(card, "txtFrom");
        var to     = GetText(card, "txtTo");
        var amtStr = GetText(card, "txtAmount");
        var lbl    = (Label)card.Controls["lblResult"]!;

        // ── Клиент талын validation ────────────────────────────────────
        if (string.IsNullOrEmpty(from))
        { SetLbl(lbl, "Илгээгч дансны дугаар оруулна уу!", HBRed); return; }
        if (string.IsNullOrEmpty(to))
        { SetLbl(lbl, "Хүлээн авагч дансны дугаар оруулна уу!", HBRed); return; }
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        { SetLbl(lbl, "Илгээгч болон хүлээн авагч данс ижил байж болохгүй!", HBRed); return; }
        if (!decimal.TryParse(amtStr, out decimal amount) || amount <= 0)
        { SetLbl(lbl, "Зөв дүн оруулна уу! (тоо, 0-ээс их)", HBRed); return; }

        // Давхар дарахаас сэргийлнэ
        btn.Enabled   = false;
        btn.Text      = "Боловсруулж байна...";
        btn.BackColor = HBNavy;

        try
        {
            var res = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api/account/transfer",
                new TransferRequest
                {
                    FromAccountNumber = from,
                    ToAccountNumber   = to,
                    Amount            = amount,
                    TellerWindowId    = RoomId   // аудитад хэрэгтэй
                });

            var msg = await res.Content
                .ReadFromJsonAsync<TransferResponse>();

            SetLbl(lbl,
                $"{(res.IsSuccessStatusCode ? "✓" : "✕")}  {msg?.Message ?? ""}",
                res.IsSuccessStatusCode ? HBGreen : HBRed);

            if (res.IsSuccessStatusCode)
            {
                // Амжилттай болсны дараа талбаруудыг цэвэрлэнэ
                GetControl<TextBox>(card, "txtFrom").Text   = "";
                GetControl<TextBox>(card, "txtTo").Text     = "";
                GetControl<TextBox>(card, "txtAmount").Text = "";
            }
        }
        catch (Exception ex)
        {
            SetLbl(lbl, $"Сервертэй холбогдох үед алдаа: {ex.Message}", HBRed);
        }
        finally
        {
            btn.Enabled   = true;
            btn.Text      = "Гүйлгээ хийх";
            btn.BackColor = HBBlue;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Таб 3: Ханш
    // ══════════════════════════════════════════════════════════════════════

    private Panel BuildRatePane()
    {
        var pane = new Panel { BackColor = Color.Transparent };
        var card = MakeCard(0, 0, 720, 310);

        AddCardTitle(card, "Валютын ханш шинэчлэх", 18, 18);

        // Валют сонгох
        card.Controls.Add(new Label
        {
            Text     = "Валют:",
            Font     = new Font("Segoe UI", 11),
            Location = new Point(18, 62),
            AutoSize = true
        });

        var cmb = new ComboBox
        {
            Name          = "cmbCurrency",
            Location      = new Point(180, 58),
            Size          = new Size(130, 30),
            Font          = new Font("Segoe UI", 11),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = HBSky
        };
        cmb.Items.AddRange(new[] { "USD", "EUR", "CNY", "RUB" });
        cmb.SelectedIndex = 0;
        card.Controls.Add(cmb);

        AddFormRow(card, "Авах ханш (₮):", "txtBuy",  18, 104);
        AddFormRow(card, "Зарах ханш (₮):", "txtSell", 18, 152);

        var btnUpdate = MakeBtn("Ханш шинэчлэх", 18, 206, 240, 50, HBGold);
        btnUpdate.Name   = "btnUpdateRate";
        btnUpdate.Click += BtnUpdateRate_Click;
        card.Controls.Add(btnUpdate);

        card.Controls.Add(new Label
        {
            Name      = "lblRateResult",
            Location  = new Point(18, 268),
            Size      = new Size(680, 34),
            Font      = new Font("Segoe UI", 11)
        });

        pane.Controls.Add(card);
        return pane;
    }

    /// <summary>
    /// Ханш шинэчлэх.
    /// ЗАСВАР: UpdatedBy = RoomId дамжуулагдана — аудит.
    /// ЗАСВАР: btn.Enabled = false — давхар хүсэлтээс сэргийлнэ.
    /// </summary>
    private async void BtnUpdateRate_Click(object? sender, EventArgs e)
    {
        var btn      = (Button)sender!;
        var card     = btn.Parent!;
        var currency = ((ComboBox)card.Controls["cmbCurrency"]!).SelectedItem!.ToString()!;
        var lbl      = (Label)card.Controls["lblRateResult"]!;

        if (!decimal.TryParse(GetText(card, "txtBuy"),  out decimal buy) ||
            !decimal.TryParse(GetText(card, "txtSell"), out decimal sell))
        { SetLbl(lbl, "Зөв ханш оруулна уу!", HBRed); return; }

        if (sell <= buy)
        { SetLbl(lbl, "Зарах ханш авах ханшаас их байх ёстой!", HBRed); return; }

        btn.Enabled   = false;
        btn.Text      = "Шинэчилж байна...";
        btn.BackColor = Color.FromArgb(160, 120, 10);

        try
        {
            var res = await _http.PutAsJsonAsync(
                $"{ServerUrl}/api/exchangerate/{currency}",
                new UpdateRateRequest
                {
                    BuyRate   = buy,
                    SellRate  = sell,
                    UpdatedBy = RoomId   // хэн өөрчилснийг аудитад бүртгэнэ
                });

            SetLbl(lbl,
                res.IsSuccessStatusCode
                    ? $"✓  {currency}: авах {buy:N0}₮,  зарах {sell:N0}₮ — шинэчлэгдлээ!"
                    : "✕  Серверт алдаа гарлаа",
                res.IsSuccessStatusCode ? HBGreen : HBRed);
        }
        catch (Exception ex)
        {
            SetLbl(lbl, $"✕  Алдаа: {ex.Message}", HBRed);
        }
        finally
        {
            btn.Enabled   = true;
            btn.Text      = "Ханш шинэчлэх";
            btn.BackColor = HBGold;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Дизайн туслах методууд
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Цагаан карт Panel үүсгэнэ.
    /// Зүүн талд 4px хөх зураас — мэргэжлийн банкны харагдал.
    /// </summary>
    private static Panel MakeCard(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Location  = new Point(x, y),
            Size      = new Size(w, h),
            BackColor = Color.White
        };
        p.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Хүрээ
            using var borderPen = new Pen(Color.FromArgb(187, 222, 251), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, p.Width - 1, p.Height - 1);
            // Зүүн accent зураас
            using var accentBrush = new SolidBrush(Color.FromArgb(21, 101, 192));
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, p.Height);
        };
        return p;
    }

    /// <summary>Картын гарчиг нэмнэ — зүүн accent зурааснаас ялгарна.</summary>
    private static void AddCardTitle(Panel card, string text, int x, int y)
    {
        card.Controls.Add(new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 44, 74),
            Location  = new Point(x + 4, y),
            AutoSize  = true
        });
    }

    /// <summary>
    /// Stat box — дугаар, тоог том үсгээр харуулах colored panel.
    /// Plain label-ийн оронд ашиглана.
    /// </summary>
    private static Panel MakeStatBox(string labelText, string valueName,
        int x, int y)
    {
        var box = new Panel
        {
            Location  = new Point(x, y),
            Size      = new Size(210, 76),
            BackColor = Color.FromArgb(227, 240, 255)
        };
        box.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(187, 222, 251), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
        };
        box.Controls.Add(new Label
        {
            Text      = labelText,
            Font      = new Font("Segoe UI", 8, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location  = new Point(12, 10),
            AutoSize  = true
        });
        box.Controls.Add(new Label
        {
            Name      = valueName,
            Text      = "---",
            Font      = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 44, 74),
            Location  = new Point(12, 28),
            AutoSize  = true
        });
        return box;
    }

    /// <summary>
    /// Stat box-ийн утгыг шинэчилнэ.
    /// Box нь _tabPanes[idx] дотор байх тул нэрээр хайна.
    /// </summary>
    private static void UpdateStatBox(Control parent, string valueName, string text)
    {
        var lbl = FindControl<Label>(parent, valueName);
        if (lbl != null) lbl.Text = text;
    }

    /// <summary>Хаан банкны дизайны товч үүсгэнэ.</summary>
    private static Button MakeBtn(string text, int x, int y, int w, int h, Color bg)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(x, y),
            Size      = new Size(w, h),
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor =
            ControlPaint.Light(bg, 0.15f);
        return btn;
    }

    /// <summary>Label + TextBox хос мөр нэмнэ.</summary>
    private static void AddFormRow(Panel parent, string labelText,
        string boxName, int x, int y)
    {
        parent.Controls.Add(new Label
        {
            Text     = labelText,
            Font     = new Font("Segoe UI", 11),
            Location = new Point(x + 4, y + 6),
            Size     = new Size(160, 24),
            ForeColor = Color.FromArgb(26, 26, 46)
        });
        var tb = new TextBox
        {
            Name        = boxName,
            Location    = new Point(x + 166, y + 2),
            Size        = new Size(240, 30),
            Font        = new Font("Segoe UI", 11),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Color.FromArgb(240, 247, 255)
        };
        // Focus үед хөх хүрээ харагдуулна
        tb.Enter += (s, e) => tb.BackColor = Color.White;
        tb.Leave += (s, e) => tb.BackColor = Color.FromArgb(240, 247, 255);
        parent.Controls.Add(tb);
    }

    private static string GetText(Control parent, string name)
    {
        var c = FindControl<TextBox>(parent, name);
        return c?.Text.Trim() ?? "";
    }

    private static T? GetControl<T>(Control parent, string name) where T : Control =>
        FindControl<T>(parent, name);

    private static T? FindControl<T>(Control parent, string name) where T : Control
    {
        foreach (Control c in parent.Controls)
        {
            if (c is T tc && c.Name == name) return tc;
            var found = FindControl<T>(c, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetLbl(Label lbl, string text, Color color)
    {
        lbl.Text      = text;
        lbl.ForeColor = color;
    }

    private static void ShowStatusMsg(string msg, Color color)
        => MessageBox.Show(msg, "Мэдэгдэл",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
}

// ── Local DTOs ────────────────────────────────────────────────────────────
record StatusResponse(int CurrentNumber, int QueueCount);
record MessageResponse(string? Message);
