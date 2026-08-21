using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

/// <summary>
/// Fixed-frame reference UI. The supplied seven screenshots remain the exact visual canvas,
/// while small opaque overlays replace only fields whose values are runtime data.
/// No automatic layout/resize is applied to the reference frame.
/// </summary>
internal sealed class ReferenceUi : Panel
{
    private static readonly Size FrameSize = new(1448, 1086);

    private readonly MainForm form;
    private readonly PictureBox canvas;
    private readonly Panel dataLayer;
    private readonly Panel[] stepHotspots = new Panel[7];
    private readonly Panel nextHotspot;
    private readonly Panel backHotspot;
    private readonly Panel cancelHotspot;
    private readonly Panel minimizeHotspot;
    private readonly Panel closeHotspot;
    private readonly MethodInfo showStepMethod;
    private readonly FieldInfo nextButtonField;
    private readonly FieldInfo backButtonField;
    private readonly FieldInfo cancelButtonField;
    private readonly FieldInfo busyField;
    private readonly FieldInfo componentsField;
    private readonly FieldInfo filesField;
    private readonly FieldInfo stateField;
    private readonly FieldInfo termsField;
    private readonly FieldInfo serverBoxField;
    private readonly FieldInfo databaseBoxField;
    private readonly FieldInfo restoreBoxField;
    private readonly FieldInfo restoreStatusField;
    private readonly FieldInfo configStatusField;
    private readonly FieldInfo downloadOverallField;
    private readonly FieldInfo installOverallField;
    private readonly FieldInfo downloadSummaryField;
    private readonly FieldInfo installSummaryField;
    private readonly System.Windows.Forms.Timer refreshTimer;

    private int currentStep;
    private bool dragging;
    private Point dragStart;

    private const int TextPad = 4;
    private static readonly Color PageBg = Color.FromArgb(4, 12, 25);
    private static readonly Color CardBg = Color.FromArgb(5, 22, 44);
    private static readonly Color TextColor = Color.FromArgb(244, 247, 252);
    private static readonly Color Muted = Color.FromArgb(175, 190, 210);
    private static readonly Color Blue = Color.FromArgb(0, 166, 255);
    private static readonly Color Green = Color.FromArgb(48, 224, 119);
    private static readonly Color DarkInput = Color.FromArgb(8, 29, 54);

    private readonly string[] imageNames = { "Step1.png", "Step2.png", "Step3.png", "Step4.png", "Step5.png", "Step6.png", "Step7.png" };

    public ReferenceUi(MainForm form)
    {
        this.form = form;

        showStepMethod = GetPrivateMethod("ShowStep");
        nextButtonField = GetPrivateField("nextButton");
        backButtonField = GetPrivateField("backButton");
        cancelButtonField = GetPrivateField("cancelButton");
        busyField = GetPrivateField("busy");
        componentsField = GetPrivateField("components");
        filesField = GetPrivateField("files");
        stateField = GetPrivateField("state");
        termsField = GetPrivateField("terms");
        serverBoxField = GetPrivateField("serverBox");
        databaseBoxField = GetPrivateField("databaseBox");
        restoreBoxField = GetPrivateField("restoreBox");
        restoreStatusField = GetPrivateField("restoreStatus");
        configStatusField = GetPrivateField("configStatus");
        downloadOverallField = GetPrivateField("downloadOverall");
        installOverallField = GetPrivateField("installOverall");
        downloadSummaryField = GetPrivateField("downloadSummary");
        installSummaryField = GetPrivateField("installSummary");

        Name = "ReferenceUiSurface";
        Dock = DockStyle.Fill;
        BackColor = PageBg;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        TabStop = false;

        canvas = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = PageBg,
            Margin = Padding.Empty,
            TabStop = false
        };
        Controls.Add(canvas);

        dataLayer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TabStop = false
        };
        Controls.Add(dataLayer);
        dataLayer.BringToFront();

        for (int i = 0; i < 7; i++)
        {
            int target = i;
            stepHotspots[i] = CreateHotspot(GetStepBounds(i), true);
            stepHotspots[i].Click += (_, _) =>
            {
                if (!IsBusy() && target <= currentStep)
                    showStepMethod.Invoke(form, new object[] { target });
            };
        }

        nextHotspot = CreateHotspot(new Rectangle(1234, 938, 155, 74), true);
        nextHotspot.Click += (_, _) => ClickButton(nextButtonField);
        backHotspot = CreateHotspot(new Rectangle(1072, 938, 150, 74), true);
        backHotspot.Click += (_, _) => ClickButton(backButtonField);
        cancelHotspot = CreateHotspot(new Rectangle(924, 938, 145, 74), true);
        cancelHotspot.Click += (_, _) => ClickButton(cancelButtonField);
        minimizeHotspot = CreateHotspot(new Rectangle(1180, 0, 90, 64), true);
        minimizeHotspot.Click += (_, _) => form.WindowState = FormWindowState.Minimized;
        closeHotspot = CreateHotspot(new Rectangle(1360, 0, 88, 64), true);
        closeHotspot.Click += (_, _) => form.Close();

        HookDrag(canvas);

        refreshTimer = new System.Windows.Forms.Timer { Interval = 250 };
        refreshTimer.Tick += (_, _) => RefreshRuntimeData();
        refreshTimer.Start();

        form.Controls.Add(this);
        BringToFront();
    }

    public void SetStep(int step)
    {
        currentStep = Math.Clamp(step, 0, 6);
        LoadCanvas();
        BuildRuntimeOverlays();

        for (int i = 0; i < stepHotspots.Length; i++)
            stepHotspots[i].Visible = i <= currentStep;

        nextHotspot.Visible = true;
        backHotspot.Visible = currentStep > 0;
        cancelHotspot.Visible = true;
        BringToFront();
        nextHotspot.BringToFront();
        backHotspot.BringToFront();
        cancelHotspot.BringToFront();
        closeHotspot.BringToFront();
        minimizeHotspot.BringToFront();
    }

    private void LoadCanvas()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "ReferenceUI", imageNames[currentStep]);
        try
        {
            Image? old = canvas.Image;
            canvas.Image = null;
            old?.Dispose();
            using var source = Image.FromFile(path);
            canvas.Image = new Bitmap(source, FrameSize);
        }
        catch
        {
            canvas.Image = null;
        }
    }

    private void BuildRuntimeOverlays()
    {
        foreach (Control c in dataLayer.Controls.Cast<Control>().ToList()) c.Dispose();
        dataLayer.Controls.Clear();

        switch (currentStep)
        {
            case 0: BuildStep1Data(); break;
            case 1: BuildStep2Data(); break;
            case 2: BuildStep3Data(); break;
            case 3: BuildStep4Data(); break;
            case 4: BuildStep5Data(); break;
            case 5: BuildStep6Data(); break;
            case 6: BuildStep7Data(); break;
        }
        dataLayer.BringToFront();
        foreach (var hp in stepHotspots) hp.BringToFront();
        nextHotspot.BringToFront();
        backHotspot.BringToFront();
        cancelHotspot.BringToFront();
        closeHotspot.BringToFront();
        minimizeHotspot.BringToFront();
    }

    private void BuildStep1Data()
    {
        AddText(new Rectangle(577, 243, 420, 28), $"Files are kept in: {GetSoftwareFolder()}", 16, Green, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
    }

    private void BuildStep2Data()
    {
        bool accepted = (termsField.GetValue(form) as CheckBox)?.Checked == true;
        var box = new Panel { Bounds = new Rectangle(350, 781, 32, 32), BackColor = Color.FromArgb(3, 14, 28), Margin = Padding.Empty };
        box.Paint += (_, e) =>
        {
            using var pen = new Pen(Blue, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, 28, 28);
            if (accepted)
            {
                using var brush = new SolidBrush(Blue);
                e.Graphics.FillRectangle(brush, 3, 3, 24, 24);
                using var checkPen = new Pen(Color.White, 3);
                e.Graphics.DrawLines(checkPen, new[] { new Point(8, 15), new Point(13, 21), new Point(23, 9) });
            }
        };
        dataLayer.Controls.Add(box);
    }

    private void BuildStep3Data()
    {
        var items = GetComponents();
        for (int i = 0; i < 5; i++)
        {
            int y = 417 + i * 81;
            var item = i < items.Count ? items[i] : null;
            string name = item?.Name ?? "";
            bool selected = item?.Selected ?? false;
            string size = GetFileSizeDisplay(name);
            AddText(new Rectangle(466, y, 470, 28), name, 15, TextColor, Color.FromArgb(4, 18, 36), ContentAlignment.MiddleLeft, "Segoe UI Semibold");
            AddText(new Rectangle(801, y, 92, 28), size, 14, TextColor, Color.FromArgb(4, 18, 36), ContentAlignment.MiddleRight, "Segoe UI");
            AddCheck(new Rectangle(364, y + 1, 28, 28), selected);
        }
        long total = items.Where(x => x.Selected).Sum(x => GetActualBytes(x.Name));
        AddText(new Rectangle(800, 852, 120, 30), total > 0 ? FormatBytes(total) : "Pending", 16, Blue, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
    }

    private void BuildStep4Data()
    {
        int overall = GetProgress(downloadOverallField);
        AddProgress(new Rectangle(348, 329, 687, 14), overall);
        AddText(new Rectangle(1050, 321, 70, 28), $"{overall}%", 16, Blue, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
        AddText(new Rectangle(345, 365, 610, 26), GetLabelText(downloadSummaryField, "Downloading files..."), 14, TextColor, Color.FromArgb(5, 22, 44), ContentAlignment.MiddleLeft, "Segoe UI");
        DrawComponentRows(530, true);
    }

    private void BuildStep5Data()
    {
        int overall = GetProgress(installOverallField);
        AddProgress(new Rectangle(347, 334, 685, 14), overall);
        AddText(new Rectangle(1045, 326, 70, 28), $"{overall}%", 16, Blue, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
        AddText(new Rectangle(347, 365, 640, 26), GetLabelText(installSummaryField, "Installing files..."), 14, TextColor, Color.FromArgb(5, 22, 44), ContentAlignment.MiddleLeft, "Segoe UI");
        DrawComponentRows(538, false);
    }

    private void BuildStep6Data()
    {
        string server = GetText(serverBoxField, "localhost");
        string db = GetText(databaseBoxField, "SuvidhaPOS");
        string restoreStatus = GetText(restoreStatusField, "");
        string configStatus = GetText(configStatusField, "");

        AddText(new Rectangle(365, 359, 520, 44), server, 15, TextColor, DarkInput, ContentAlignment.MiddleLeft, "Segoe UI");
        AddText(new Rectangle(365, 684, 520, 44), db, 15, TextColor, DarkInput, ContentAlignment.MiddleLeft, "Segoe UI");
        AddText(new Rectangle(1106, 497, 240, 28), server, 13, Blue, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
        AddText(new Rectangle(1106, 546, 240, 28), "Windows Authentication", 13, Blue, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
        AddText(new Rectangle(1106, 594, 240, 28), db, 13, Blue, CardBg, ContentAlignment.MiddleLeft, "Segoe UI Semibold");
        if (!string.IsNullOrWhiteSpace(restoreStatus))
            AddText(new Rectangle(964, 871, 375, 26), restoreStatus, 11, restoreStatus.Contains("success", StringComparison.OrdinalIgnoreCase) ? Green : Muted, CardBg, ContentAlignment.MiddleLeft, "Segoe UI");
        if (!string.IsNullOrWhiteSpace(configStatus))
            AddText(new Rectangle(964, 899, 375, 26), configStatus, 11, configStatus.Contains("success", StringComparison.OrdinalIgnoreCase) ? Green : Muted, CardBg, ContentAlignment.MiddleLeft, "Segoe UI");
    }

    private void BuildStep7Data()
    {
        var items = GetComponents();
        int installed = items.Count(x => string.Equals(x.Status, "Installed", StringComparison.OrdinalIgnoreCase));
        long totalDownload = GetFilesBytes();
        long totalInstall = GetInstalledBytes(items);
        string version = DetectInstalledVersion();
        string location = GetSoftwareFolder();
        string date = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
        string status = installed == items.Count && installed > 0 ? "Completed Successfully" : $"{installed} of {items.Count} completed";

        AddText(new Rectangle(411, 409, 410, 28), $"{installed} of {items.Count} components installed successfully", 14, TextColor, CardBg, ContentAlignment.MiddleLeft, "Segoe UI");
        AddText(new Rectangle(1208, 408, 156, 30), location, 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
        AddText(new Rectangle(1208, 454, 156, 30), version, 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
        AddText(new Rectangle(1240, 500, 124, 30), items.Count.ToString(), 13, TextColor, CardBg, ContentAlignment.MiddleRight, "Segoe UI");
        AddText(new Rectangle(1196, 547, 168, 30), FormatBytes(totalDownload), 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
        AddText(new Rectangle(1196, 593, 168, 30), FormatBytes(totalInstall), 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
        AddText(new Rectangle(1145, 640, 219, 30), date, 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
        AddText(new Rectangle(1140, 687, 224, 30), status, 13, Green, CardBg, ContentAlignment.MiddleRight, "Segoe UI Semibold");
    }

    private void DrawComponentRows(int startY, bool download)
    {
        var items = GetComponents();
        for (int i = 0; i < items.Count && i < 5; i++)
        {
            var item = items[i];
            int y = startY + i * 72;
            string status = item.Status;
            int pct = item.Progress;
            string size = GetFileSizeDisplay(item.Name);
            AddText(new Rectangle(405, y + 7, 360, 28), item.Name, 14, TextColor, Color.FromArgb(3, 16, 31), ContentAlignment.MiddleLeft, "Segoe UI Semibold");
            AddText(new Rectangle(744, y + 7, 105, 28), size, 13, TextColor, Color.FromArgb(3, 16, 31), ContentAlignment.MiddleRight, "Segoe UI");
            AddText(new Rectangle(889, y + 7, 156, 28), status, 12, status.Contains("Failed", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(255, 83, 98) : status.Contains("Installed", StringComparison.OrdinalIgnoreCase) || status.Contains("Ready", StringComparison.OrdinalIgnoreCase) ? Green : Blue, Color.FromArgb(3, 16, 31), ContentAlignment.MiddleLeft, "Segoe UI");
            AddText(new Rectangle(1050, y + 7, 50, 28), $"{pct}%", 12, Blue, Color.FromArgb(3, 16, 31), ContentAlignment.MiddleRight, "Segoe UI Semibold");
            AddProgress(new Rectangle(1105, y + 14, 220, 10), pct);
            AddIconPlaceholder(new Rectangle(350, y + 2, 44, 38));
        }
    }

    private void RefreshRuntimeData()
    {
        if (!IsHandleCreated || IsDisposed) return;
        try
        {
            if (currentStep is 3 or 4)
            {
                int overall = currentStep == 3 ? GetProgress(downloadOverallField) : GetProgress(installOverallField);
                BuildRuntimeOverlays();
            }
            else if (currentStep == 5)
            {
                BuildRuntimeOverlays();
            }
            else if (currentStep == 6)
            {
                BuildRuntimeOverlays();
            }
        }
        catch { }
    }

    private Label AddText(Rectangle bounds, string value, float size, Color fore, Color back, ContentAlignment align, string family)
    {
        var label = new Label
        {
            Bounds = bounds,
            Text = value,
            Font = new Font(family, size, FontStyle.Regular),
            ForeColor = fore,
            BackColor = back,
            TextAlign = align,
            AutoEllipsis = true,
            Margin = Padding.Empty,
            Padding = new Padding(TextPad, 0, TextPad, 0)
        };
        dataLayer.Controls.Add(label);
        return label;
    }

    private void AddCheck(Rectangle bounds, bool checkedState)
    {
        var box = new Panel { Bounds = bounds, BackColor = Color.FromArgb(5, 22, 44), Margin = Padding.Empty };
        box.Paint += (_, e) =>
        {
            using var pen = new Pen(Blue, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, bounds.Width - 3, bounds.Height - 3);
            if (checkedState)
            {
                using var brush = new SolidBrush(Blue);
                e.Graphics.FillRectangle(brush, 3, 3, bounds.Width - 6, bounds.Height - 6);
                using var cp = new Pen(Color.White, 3);
                e.Graphics.DrawLines(cp, new[] { new Point(6, 14), new Point(11, 19), new Point(20, 8) });
            }
        };
        dataLayer.Controls.Add(box);
    }

    private void AddProgress(Rectangle bounds, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        var p = new Panel { Bounds = bounds, BackColor = Color.FromArgb(2, 14, 29), Margin = Padding.Empty };
        p.Paint += (_, e) =>
        {
            Rectangle track = new(0, 1, p.Width - 1, p.Height - 3);
            using var trackBrush = new SolidBrush(Color.FromArgb(29, 45, 68));
            using var fillBrush = new SolidBrush(Blue);
            e.Graphics.FillRectangle(trackBrush, track);
            int fillWidth = (int)Math.Round(track.Width * percent / 100.0);
            if (fillWidth > 0) e.Graphics.FillRectangle(fillBrush, new Rectangle(0, 1, Math.Min(fillWidth, track.Width), track.Height));
        };
        dataLayer.Controls.Add(p);
    }

    private void AddIconPlaceholder(Rectangle bounds)
    {
        var p = new Panel { Bounds = bounds, BackColor = Color.Transparent, Enabled = false };
        dataLayer.Controls.Add(p);
    }

    private IReadOnlyList<ComponentSnapshot> GetComponents()
    {
        var result = new List<ComponentSnapshot>();
        if (componentsField.GetValue(form) is not System.Collections.IEnumerable enumerable) return result;
        foreach (object? item in enumerable)
        {
            if (item == null) continue;
            Type t = item.GetType();
            string name = Convert.ToString(t.GetProperty("Name")?.GetValue(item)) ?? "";
            string status = Convert.ToString(t.GetProperty("Status")?.GetValue(item)) ?? "Waiting";
            bool selected = Convert.ToBoolean(t.GetProperty("Selected")?.GetValue(item) ?? false);
            int progress = 0;
            var pb = t.GetProperty("Progress")?.GetValue(item) as ProgressBar;
            if (pb != null) progress = pb.Value;
            result.Add(new ComponentSnapshot(name, status, selected, progress));
        }
        return result;
    }

    private long GetActualBytes(string componentName)
    {
        if (filesField.GetValue(form) is Dictionary<string, string> files && files.TryGetValue(componentName, out string? path) && File.Exists(path))
            return new FileInfo(path).Length;
        string safe = Path.Combine(GetSoftwareFolder(), SafeFileName(componentName));
        foreach (string ext in new[] { ".exe", ".msi" })
        {
            string p = safe + ext;
            if (File.Exists(p)) return new FileInfo(p).Length;
        }
        return 0;
    }

    private long GetFilesBytes()
    {
        long total = 0;
        foreach (var c in GetComponents().Where(x => x.Selected)) total += GetActualBytes(c.Name);
        return total;
    }

    private long GetInstalledBytes(IReadOnlyList<ComponentSnapshot> items) => items.Where(x => string.Equals(x.Status, "Installed", StringComparison.OrdinalIgnoreCase)).Sum(x => GetActualBytes(x.Name));

    private string GetFileSizeDisplay(string componentName)
    {
        long bytes = GetActualBytes(componentName);
        return bytes > 0 ? FormatBytes(bytes) : "Pending";
    }

    private string DetectInstalledVersion()
    {
        try
        {
            string exe = Path.Combine(GetSoftwareFolder(), "SuvidhaPOS.exe");
            if (!File.Exists(exe)) exe = Path.Combine(GetSoftwareFolder(), "RetailPos.exe");
            if (File.Exists(exe)) return FileVersionInfo.GetVersionInfo(exe).FileVersion ?? "Unknown";
        }
        catch { }
        return Application.ProductVersion;
    }

    private string GetSoftwareFolder()
    {
        var f = typeof(MainForm).GetField("SoftwareFolder", BindingFlags.Static | BindingFlags.NonPublic);
        return Convert.ToString(f?.GetValue(null)) ?? @"D:\Suvidha Pos\Software";
    }

    private static string SafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int i = 0;
        while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
        return value >= 100 || i == 0 ? $"{value:0} {units[i]}" : $"{value:0.##} {units[i]}";
    }

    private int GetProgress(FieldInfo field) => (field.GetValue(form) as ProgressBar)?.Value ?? 0;

    private string GetText(FieldInfo field, string fallback) => Convert.ToString((field.GetValue(form) as TextBox)?.Text) ?? fallback;
    private string GetLabelText(FieldInfo field, string fallback) => Convert.ToString((field.GetValue(form) as Label)?.Text) ?? fallback;

    private Rectangle GetStepBounds(int index) => new(14, 82 + index * 102, 292, 88);

    private Panel CreateHotspot(Rectangle bounds, bool hand)
    {
        var p = new Panel { Bounds = bounds, BackColor = Color.Transparent, Margin = Padding.Empty, Padding = Padding.Empty, Cursor = hand ? Cursors.Hand : Cursors.Default, TabStop = false };
        Controls.Add(p);
        p.BringToFront();
        return p;
    }

    private void HookDrag(Control control)
    {
        control.MouseDown += DragDown;
        control.MouseMove += DragMove;
        control.MouseUp += DragUp;
    }

    private void DragDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (e.Y > 64 || e.X >= 1180) return;
        dragging = true;
        dragStart = Cursor.Position;
    }

    private void DragMove(object? sender, MouseEventArgs e)
    {
        if (!dragging) return;
        Point now = Cursor.Position;
        int dx = now.X - dragStart.X;
        int dy = now.Y - dragStart.Y;
        form.Location = new Point(form.Left + dx, form.Top + dy);
        dragStart = now;
    }

    private void DragUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) dragging = false;
    }

    private bool IsBusy() => busyField.GetValue(form) is true;

    private void ClickButton(FieldInfo field)
    {
        if (field.GetValue(form) is Button button && button.Enabled)
            button.PerformClick();
    }

    private static MethodInfo GetPrivateMethod(string name) => typeof(MainForm).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(MainForm).FullName, name);
    private static FieldInfo GetPrivateField(string name) => typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(MainForm).FullName, name);

    private sealed record ComponentSnapshot(string Name, string Status, bool Selected, int Progress);
}
