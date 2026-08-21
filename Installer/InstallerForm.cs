using System.Drawing;
using System.Windows.Forms;
using SuvidhaPOSInstaller.DynamicUI.Controls;
using SuvidhaPOSInstaller.DynamicUI.Models;
using SuvidhaPOSInstaller.DynamicUI.Steps;

namespace SuvidhaPOSInstaller.DynamicUI;

public sealed class InstallerForm : Form
{
    private readonly InstallerEngine engine;
    private readonly Panel content = new();
    private readonly FlowLayoutPanel side = new();
    private readonly StepNavigationControl nav = new();
    private readonly List<InstallerStepControl> steps = new();
    private readonly string[] names = { "Welcome", "Terms & Conditions", "Components", "Download", "Install", "Database Setup", "Finish" };
    private readonly string[] subs = { "Welcome to Installer", "Read important terms", "Select components", "Download installation files", "Install all components", "Database setup", "Installation complete" };
    private int currentStep;

    public InstallerForm()
    {
        Text = "Suvidha POS Installer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 700);
        Size = new Size(1440, 900);
        BackColor = Color.FromArgb(2, 10, 19);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = true;
        MinimizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered = true;

        engine = new InstallerEngine();
        engine.StateChanged += (_, _) => BeginInvokeIfRequired(RefreshCurrentStep);

        BuildShell();
        BuildSteps();
        currentStep = Math.Clamp(engine.State.CurrentStep, 1, 7);
        ShowStep(currentStep);
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.FromArgb(2, 10, 19),
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(4, 16, 34), Padding = new Padding(18, 10, 18, 8), Margin = Padding.Empty };
        var title = new Label { Text = "Suvidha POS  |  Installer", Dock = DockStyle.Left, Width = 460, ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI Semibold", 18F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        var right = new Label { Text = "Dynamic Installer", Dock = DockStyle.Right, Width = 220, ForeColor = Color.FromArgb(0, 190, 255), Font = new Font("Segoe UI Semibold", 11F), TextAlign = ContentAlignment.MiddleRight };
        header.Controls.Add(right); header.Controls.Add(title); root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);

        side.Dock = DockStyle.Fill;
        side.FlowDirection = FlowDirection.TopDown;
        side.WrapContents = false;
        side.AutoScroll = true;
        side.BackColor = Color.FromArgb(3, 16, 28);
        side.Padding = new Padding(10, 12, 10, 12);
        side.Margin = Padding.Empty;
        root.Controls.Add(side, 0, 1);

        for (int i = 0; i < names.Length; i++)
        {
            int target = i + 1;
            var b = new Button
            {
                Text = $"{target}   {names[i]}\r\n{new string(' ', 5)}{subs[i]}",
                Tag = target,
                Width = 245,
                Height = 66,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.FromArgb(5, 24, 42),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 7),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(12, 68, 112);
            b.Click += (_, _) => { if (!engine.Busy && target <= currentStep) ShowStep(target); };
            side.Controls.Add(b);
        }

        var help = new Panel { Width = 245, Height = 108, BackColor = Color.FromArgb(6, 24, 50), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 8, 0, 0), Padding = new Padding(10) };
        help.Controls.Add(new Label { Text = "Need Help?", ForeColor = Color.FromArgb(0,190,255), Font = new Font("Segoe UI Semibold", 12F), Dock = DockStyle.Top, Height = 28 });
        help.Controls.Add(new Label { Text = "Support is available if you need help.\r\n+91 827171 8844", ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 9F), Dock = DockStyle.Fill });
        side.Controls.Add(help);

        content.Dock = DockStyle.Fill;
        content.BackColor = Color.FromArgb(3, 13, 24);
        content.Padding = new Padding(12);
        content.Margin = Padding.Empty;
        root.Controls.Add(content, 1, 1);

        nav.SetStep(1, 7);
        nav.NextClicked += async (_, _) => await HandleNextAsync();
        nav.BackClicked += (_, _) => { if (!engine.Busy && currentStep > 1) ShowStep(currentStep - 1); };
        nav.CancelClicked += (_, _) => Close();
        Controls.Add(nav);
    }

    private void BuildSteps()
    {
        var welcome = new WelcomeStep();
        var terms = new TermsStep();
        var components = new ComponentsStep();
        var download = new DownloadStep();
        var install = new InstallStep();
        var database = new DatabaseStep();
        var finish = new FinishStep();

        database.TestConnectionRequested += async (_, _) => { await engine.TestConnectionAsync(); RefreshCurrentStep(); };
        database.RestoreRequested += async (_, _) => { ReadDatabaseInputs(database); await engine.RestoreAsync(); RefreshCurrentStep(); };
        finish.LaunchRequested += (_, _) => engine.LaunchPos();

        steps.AddRange(new InstallerStepControl[] { welcome, terms, components, download, install, database, finish });
    }

    private async Task HandleNextAsync()
    {
        if (engine.Busy) return;
        switch (currentStep)
        {
            case 1:
                ShowStep(2); break;
            case 2:
                var terms = (TermsStep)steps[1];
                if (!terms.AcceptTerms.Checked) { MessageBox.Show(this, "Please accept the Terms & Conditions first.", "Suvidha POS Installer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                ShowStep(3); break;
            case 3:
                if (!engine.State.Components.Any(x => x.Selected)) { MessageBox.Show(this, "Select at least one component.", "Components", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                ShowStep(4); await engine.DownloadAllAsync(); RefreshCurrentStep(); break;
            case 4:
                if (!engine.State.Components.Where(x => x.Selected).All(x => x.Status is "Downloaded" or "Ready" or "Installed")) { MessageBox.Show(this, "Please wait until all selected downloads are ready.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                ShowStep(5); await engine.InstallAllAsync(); RefreshCurrentStep(); break;
            case 5:
                var db = (DatabaseStep)steps[5]; ReadDatabaseInputs(db);
                if (!string.IsNullOrWhiteSpace(engine.State.BackupPath) && db.Restore.Checked) { await engine.RestoreAsync(); RefreshCurrentStep(); if (!engine.State.Database.RestoreStatus.Contains("success", StringComparison.OrdinalIgnoreCase)) return; }
                if (!engine.SaveConfiguration()) { RefreshCurrentStep(); MessageBox.Show(this, engine.State.Error ?? "Could not save database configuration.", "Database Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                ShowStep(7); break;
            case 6:
                ShowStep(7); break;
            case 7:
                Close(); break;
        }
    }

    private void ReadDatabaseInputs(DatabaseStep db)
    {
        engine.State.Database.ServerName = db.Server.Text.Trim();
        engine.State.Database.Authentication = db.Authentication.Text;
        engine.State.Database.UserName = db.UserName.Text;
        engine.State.Database.Password = db.Password.Text;
        engine.State.Database.DatabaseName = db.DatabaseName.Text.Trim();
        engine.State.Database.Collation = db.Collation.Text;
        engine.State.Database.RestoreRequested = db.Restore.Checked;
        engine.State.BackupPath = string.IsNullOrWhiteSpace(db.BackupPath.Text) ? null : db.BackupPath.Text.Trim();
    }

    private void ShowStep(int number)
    {
        currentStep = Math.Clamp(number, 1, 7);
        engine.SetStep(currentStep);
        content.SuspendLayout();
        foreach (Control c in content.Controls.OfType<Control>().ToList()) c.Dispose();
        var step = steps[currentStep - 1];
        content.Controls.Add(step);
        step.Dock = DockStyle.Fill;
        nav.SetStep(currentStep, 7);
        RefreshCurrentStep();
        UpdateSidebar();
        content.ResumeLayout(true);
    }

    private void RefreshCurrentStep()
    {
        if (IsDisposed) return;
        var s = engine.State;
        switch (steps[currentStep - 1])
        {
            case WelcomeStep x: x.SetState(s); break;
            case TermsStep x: x.SetState(s); break;
            case ComponentsStep x: x.SetState(s); break;
            case DownloadStep x: x.SetState(s); break;
            case InstallStep x: x.SetState(s); break;
            case DatabaseStep x: x.SetState(s); break;
            case FinishStep x: x.SetState(s); break;
        }
        nav.SetBusy(engine.Busy);
    }

    private void UpdateSidebar()
    {
        foreach (Control c in side.Controls)
        {
            if (c is Button b && b.Tag is int n)
            {
                b.BackColor = n == currentStep ? Color.FromArgb(12, 119, 225) : Color.FromArgb(5, 24, 42);
                b.FlatAppearance.BorderColor = n == currentStep ? Color.FromArgb(0, 180, 255) : Color.FromArgb(12, 68, 112);
            }
        }
    }

    private void BeginInvokeIfRequired(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        engine.Dispose();
        base.OnFormClosed(e);
    }
}