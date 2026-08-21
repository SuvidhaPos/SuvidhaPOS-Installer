using System.Drawing;
using System.Windows.Forms;
using SuvidhaPOSInstaller.DynamicUI.Controls;
using SuvidhaPOSInstaller.DynamicUI.Helpers;
using SuvidhaPOSInstaller.DynamicUI.Models;

namespace SuvidhaPOSInstaller.DynamicUI.Steps;

public sealed class WelcomeStep : InstallerStepControl
{
    private readonly Label path = new();
    public WelcomeStep()
    {
        var title = Label("Welcome to Suvidha POS Installer", 28, true);
        title.Height = 44;
        var sub = Label("Install the required components safely and step-by-step.", 13);
        sub.Height = 34;
        path.ForeColor = Green; path.Font = new Font("Segoe UI", 11, FontStyle.Bold); path.Height = 30;
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Margin = Padding.Empty };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Absolute, 155)); cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var what = Card(); what.Height = 150; what.Dock = DockStyle.Fill; what.Margin = new Padding(0,0,6,10);
        var req = Card(); req.Height = 150; req.Dock = DockStyle.Fill; req.Margin = new Padding(6,0,0,10);
        what.Controls.Add(Label("What will be installed?",18,true));
        req.Controls.Add(Label("System Requirements",18,true));
        what.Controls.Add(new Label { Text = "• SQL Server 2019\n• SQL Server Management Studio\n• Crystal Reports Runtime\n• Suvidha POS Application\n• Database Backup Restore", ForeColor=White, Font=new Font("Segoe UI",11), Dock=DockStyle.Fill, Padding=new Padding(0,14,0,0), AutoEllipsis=true });
        req.Controls.Add(new Label { Text = "• Windows 10 / 11 (64-bit)\n• 4 GB RAM or more\n• 10 GB free disk space\n• Internet connection for downloads\n• Administrator privileges", ForeColor=White, Font=new Font("Segoe UI",11), Dock=DockStyle.Fill, Padding=new Padding(0,14,0,0), AutoEllipsis=true });
        cards.Controls.Add(what,0,0); cards.SetColumnSpan(what,1); cards.Controls.Add(req,1,0);
        var feature = Card(); feature.Dock=DockStyle.Fill; feature.Margin=new Padding(0,4,0,0); feature.Controls.Add(new Label{Text="Dynamic interface • actual runtime values • resizes with the window",ForeColor=Muted,Font=new Font("Segoe UI",10),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter});
        cards.Controls.Add(feature,0,1); cards.SetColumnSpan(feature,2);
        var host = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=4, BackColor=Color.Transparent, Margin=Padding.Empty, Padding=new Padding(8) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute,54)); host.RowStyles.Add(new RowStyle(SizeType.Absolute,30)); host.RowStyles.Add(new RowStyle(SizeType.Percent,100)); host.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        host.Controls.Add(title,0,0); host.Controls.Add(sub,0,1); host.Controls.Add(path,0,2); host.Controls.Add(cards,0,3);
        Controls.Add(host);
    }
    public void SetState(InstallerState s) => path.Text = $"Files are kept in: {s.InstallPath}";
}

public sealed class TermsStep : InstallerStepControl
{
    public CheckBox AcceptTerms { get; } = new();
    public TermsStep()
    {
        var host = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3, BackColor=Color.Transparent, Margin=Padding.Empty, Padding=new Padding(8) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute,54)); host.RowStyles.Add(new RowStyle(SizeType.Percent,100)); host.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
        host.Controls.Add(Label("Terms & Conditions",28,true),0,0);
        var box = new RichTextBox { Dock=DockStyle.Fill, ReadOnly=true, BackColor=Color.FromArgb(4,17,29), ForeColor=White, BorderStyle=BorderStyle.FixedSingle, Font=new Font("Segoe UI",10.5F), Text=TermsText(), DetectUrls=true };
        host.Controls.Add(box,0,1);
        AcceptTerms.Text="I accept the terms and conditions"; AcceptTerms.ForeColor=White; AcceptTerms.Font=new Font("Segoe UI",10,FontStyle.Bold); AcceptTerms.AutoSize=true; AcceptTerms.Anchor=AnchorStyles.Left;
        host.Controls.Add(AcceptTerms,0,2); Controls.Add(host);
    }
    private static string TermsText() => string.Join(Environment.NewLine,
        "Please read the following terms and conditions carefully before installing Suvidha POS.","",
        "1. License", "Suvidha POS is licensed, not sold. Your use of this software is subject to this license agreement.","",
        "2. Use of Software", "You may use this software only for lawful purposes and in accordance with the terms of this agreement.","",
        "3. Restrictions", "You may not copy, modify, distribute, sell, or lease any part of this software without written permission.","",
        "4. Data & Privacy", "Application data is stored locally on your system unless the application configuration states otherwise.","",
        "5. Warranty Disclaimer", "This software is provided as is without warranties of any kind.","",
        "6. Limitation of Liability", "Suvidha POS shall not be liable for damages arising from use or inability to use this software.");
    public void SetState(InstallerState s) { AcceptTerms.Checked = s.CurrentStep > 1 || s.SetupCompleted; }
}

public sealed class ComponentsStep : InstallerStepControl
{
    private readonly FlowLayoutPanel list = new();
    private readonly Label total = new();
    public ComponentsStep()
    {
        var host = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3, BackColor=Color.Transparent, Margin=Padding.Empty, Padding=new Padding(8) };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute,54)); host.RowStyles.Add(new RowStyle(SizeType.Percent,100)); host.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        host.Controls.Add(Label("Select Components",28,true),0,0);
        list.FlowDirection=FlowDirection.TopDown; list.WrapContents=false; list.AutoScroll=true; list.Dock=DockStyle.Fill; list.BackColor=Color.Transparent; list.Margin=Padding.Empty;
        host.Controls.Add(list,0,1);
        total.Font=new Font("Segoe UI",10,FontStyle.Bold); total.ForeColor=Blue; total.Dock=DockStyle.Fill; total.TextAlign=ContentAlignment.MiddleLeft; host.Controls.Add(total,0,2); Controls.Add(host);
    }
    public void SetState(InstallerState s)
    {
        list.SuspendLayout(); list.Controls.Clear(); long selected=0;
        foreach (var c in s.Components)
        {
            var row=new Panel{Width=Math.Max(650,list.ClientSize.Width-18),Height=70,BackColor=Color.FromArgb(4,17,29),Margin=new Padding(0,0,0,7),Padding=new Padding(12)};
            var cb=new CheckBox{Checked=c.Selected,Location=new Point(10,20),AutoSize=true};
            cb.CheckedChanged+=(_,_)=>{c.Selected=cb.Checked; total.Text=$"Total selected size: {FormatHelper.Size(s.Components.Where(x=>x.Selected).Sum(x=>x.SizeBytes))}";};
            var name=new Label{Text=c.Name,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=White,Location=new Point(44,8),Width=Math.Max(250,row.Width-180),Height=22,AutoEllipsis=true};
            var desc=new Label{Text=c.Description,Font=new Font("Segoe UI",8.5F),ForeColor=Muted,Location=new Point(44,34),Width=Math.Max(250,row.Width-180),Height=20,AutoEllipsis=true};
            var size=new Label{Text=FormatHelper.Size(c.SizeBytes),Font=new Font("Segoe UI",9,FontStyle.Bold),ForeColor=White,Location=new Point(Math.Max(420,row.Width-110),23),Width=90,Height=22,TextAlign=ContentAlignment.MiddleRight,AutoEllipsis=true};
            row.Controls.AddRange(new Control[]{cb,name,desc,size}); list.Controls.Add(row); if(c.Selected) selected+=c.SizeBytes;
        }
        total.Text=$"Total selected size: {FormatHelper.Size(selected)}"; list.ResumeLayout();
    }
}

public abstract class ProgressStepBase : InstallerStepControl
{
    protected readonly Label summary = new(); protected readonly ProgressBar overall = new(); protected readonly Label overallPct = new(); protected readonly FlowLayoutPanel rows = new();
    protected void BuildProgress(string titleText, string subtitle, string sectionText)
    {
        var host=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(8)};
        host.RowStyles.Add(new RowStyle(SizeType.Absolute,54)); host.RowStyles.Add(new RowStyle(SizeType.Absolute,118)); host.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        host.Controls.Add(Label(titleText,28,true),0,0);
        var top=Card(); top.Dock=DockStyle.Fill; top.Margin=new Padding(0,0,0,10); top.Height=112;
        var topGrid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent}; topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72)); topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));
        topGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,24)); topGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,32)); topGrid.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        topGrid.Controls.Add(new Label{Text=subtitle,ForeColor=Muted,Dock=DockStyle.Fill,AutoEllipsis=true},0,0); overall.Minimum=0; overall.Maximum=100; overall.Dock=DockStyle.Fill; overall.Margin=new Padding(0,4,6,4); topGrid.Controls.Add(overall,0,1); overallPct.ForeColor=Blue; overallPct.Font=new Font("Segoe UI",11,FontStyle.Bold); overallPct.Dock=DockStyle.Fill; overallPct.TextAlign=ContentAlignment.MiddleLeft; topGrid.Controls.Add(overallPct,1,1); summary.ForeColor=White; summary.Dock=DockStyle.Fill; summary.AutoEllipsis=true; topGrid.Controls.Add(summary,0,2); top.Controls.Add(topGrid); host.Controls.Add(top,0,1);
        var comp=Card(); comp.Dock=DockStyle.Fill; comp.Margin=Padding.Empty; comp.Controls.Add(new Label{Text=sectionText,Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=White,Dock=DockStyle.Top,Height=36}); rows.FlowDirection=FlowDirection.TopDown; rows.WrapContents=false; rows.AutoScroll=true; rows.Dock=DockStyle.Fill; rows.BackColor=Color.Transparent; rows.Margin=Padding.Empty; comp.Controls.Add(rows); host.Controls.Add(comp,0,2); Controls.Add(host);
    }
    protected void FillRows(InstallerState s){rows.SuspendLayout();rows.Controls.Clear();foreach(var c in s.Components.Where(x=>x.Selected)){var row=new ComponentProgressControl{Width=Math.Max(700,rows.ClientSize.Width-22)};row.SetData(c);rows.Controls.Add(row);}rows.ResumeLayout();}
}

public sealed class DownloadStep : ProgressStepBase
{
    private readonly Label location=new();
    public DownloadStep(){BuildProgress("Download Installer Files","Please wait while we download the required files.","Download Components");location.ForeColor=Green;location.Font=new Font("Segoe UI",10,FontStyle.Bold);location.Dock=DockStyle.Top;location.Height=26;location.AutoEllipsis=true;}
    public void SetState(InstallerState s){overall.Value=s.Download.Percentage;overallPct.Text=$"{s.Download.Percentage}%";summary.Text=$"{s.Download.Location} • {FormatHelper.Size(s.Download.DownloadedBytes)} of {FormatHelper.Size(s.Download.TotalBytes)}";location.Text=$"Download location: {s.Download.Location}";FillRows(s);}
}

public sealed class InstallStep : ProgressStepBase
{
    public InstallStep(){BuildProgress("Install Components","Please wait while we install the selected components.","Installation Components");}
    public void SetState(InstallerState s){overall.Value=s.Installation.Percentage;overallPct.Text=$"{s.Installation.Percentage}%";summary.Text=$"{s.Installation.Location} • {FormatHelper.Size(s.Installation.InstalledBytes)} of {FormatHelper.Size(s.Installation.TotalBytes)}";FillRows(s);}
}

public sealed class DatabaseStep : InstallerStepControl
{
    public readonly ComboBox Server=new(); public readonly ComboBox Authentication=new(); public readonly TextBox UserName=new(); public readonly TextBox Password=new(); public readonly TextBox DatabaseName=new(); public readonly ComboBox Collation=new(); public readonly CheckBox Restore=new(); public readonly TextBox BackupPath=new(); public readonly Button RestoreButton=new(); public readonly Label ConnectionStatus=new(); public readonly Label RestoreStatus=new();
    public event EventHandler? TestConnectionRequested; public event EventHandler? RestoreRequested;
    public DatabaseStep()
    {
        var host=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(8)}; host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));host.RowStyles.Add(new RowStyle(SizeType.Absolute,54));host.RowStyles.Add(new RowStyle(SizeType.Percent,100));host.RowStyles.Add(new RowStyle(SizeType.Absolute,56));host.Controls.Add(Label("Database Setup",28,true),0,0);host.SetColumnSpan(host.GetControlFromPosition(0,0),2);
        var left=Card();left.Dock=DockStyle.Fill;left.Margin=new Padding(0,0,6,10);left.Controls.Add(Label("Connection Settings",18,true));AddField(left,"Server Name",Server,40);AddField(left,"Authentication",Authentication,105);AddField(left,"User Name",UserName,170);AddField(left,"Password",Password,235);AddField(left,"Database Name",DatabaseName,300);AddField(left,"Collation",Collation,365);Authentication.Items.AddRange(new object[]{"Windows Authentication","SQL Server Authentication"});Collation.Items.AddRange(new object[]{"SQL_Latin1_General_CP1_CI_AS","Latin1_General_100_CI_AS"});Password.PasswordChar='•';var test=new Button{Text="⚙  Test Connection",Location=new Point(22,440),Size=new Size(170,42)};StyleButton(test,false);test.Click+=(_,_)=>TestConnectionRequested?.Invoke(this,EventArgs.Empty);ConnectionStatus.Text="Connection status: Not tested";ConnectionStatus.ForeColor=Color.Gold;ConnectionStatus.Location=new Point(205,452);ConnectionStatus.AutoSize=true;left.Controls.AddRange(new Control[]{test,ConnectionStatus});
        var right=Card();right.Dock=DockStyle.Fill;right.Margin=new Padding(6,0,0,10);right.Controls.Add(Label("Database Information",18,true));var info=new Label{Text="Suvidha POS requires a SQL Server database to store application data.\n\nThe fields here are live runtime values.",ForeColor=White,Font=new Font("Segoe UI",10),Dock=DockStyle.Top,Height=110,Padding=new Padding(0,14,0,0),AutoEllipsis=true};right.Controls.Add(info);var backupLabel=Label("Backup / Restore",13,true);backupLabel.Location=new Point(18,140);backupLabel.Width=150;backupLabel.Height=24;right.Controls.Add(backupLabel);BackupPath.Location=new Point(18,172);BackupPath.Size=new Size(360,34);right.Controls.Add(BackupPath);var browse=new Button{Text="Browse",Location=new Point(388,172),Size=new Size(90,34)};StyleButton(browse,false);browse.Click+=(_,_)=>BrowseBackup();right.Controls.Add(browse);Restore.Text="Restore database after installation";Restore.ForeColor=White;Restore.Location=new Point(18,218);Restore.AutoSize=true;right.Controls.Add(Restore);RestoreButton.Text="Restore Database";RestoreButton.Location=new Point(18,260);RestoreButton.Size=new Size(170,42);StyleButton(RestoreButton,true);RestoreButton.Click+=(_,_)=>RestoreRequested?.Invoke(this,EventArgs.Empty);right.Controls.Add(RestoreButton);RestoreStatus.Text="";RestoreStatus.ForeColor=Muted;RestoreStatus.Location=new Point(18,316);RestoreStatus.Size=new Size(480,70);RestoreStatus.AutoEllipsis=true;right.Controls.Add(RestoreStatus);host.Controls.Add(left,0,1);host.Controls.Add(right,1,1);
        var note=new Label{Text="Database name and server are validated against the live SQL Server connection when you continue.",ForeColor=Muted,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};host.Controls.Add(note,0,2);host.SetColumnSpan(note,2);Controls.Add(host);
    }
    private void AddField(Control p,string caption,Control c,int y){var l=Label(caption,9,false);l.Location=new Point(22,y);l.Height=20;l.Width=200;p.Controls.Add(l);c.Location=new Point(22,y+22);c.Size=new Size(500,34);c.BackColor=Color.FromArgb(8,25,42);c.ForeColor=White;p.Controls.Add(c);}
    private void BrowseBackup(){using var d=new OpenFileDialog{Filter="SQL Backup (*.bak;*.backup)|*.bak;*.backup|All files (*.*)|*.*"};if(d.ShowDialog(this)==DialogResult.OK)BackupPath.Text=d.FileName;}
    public void SetState(InstallerState s){Server.Text=s.Database.ServerName;Authentication.Text=s.Database.Authentication;UserName.Text=s.Database.UserName;Password.Text=s.Database.Password;DatabaseName.Text=s.Database.DatabaseName;Collation.Text=s.Database.Collation;BackupPath.Text=s.BackupPath??"";ConnectionStatus.Text=s.Database.ConnectionSuccessful?"✓ Connection Successful":"Connection status: Not tested";ConnectionStatus.ForeColor=s.Database.ConnectionSuccessful?Color.LimeGreen:Color.Gold;RestoreStatus.Text=s.Database.RestoreStatus;RestoreStatus.ForeColor=s.Database.RestoreStatus.Contains("success",StringComparison.OrdinalIgnoreCase)?Green:(s.Database.RestoreStatus.Contains("failed",StringComparison.OrdinalIgnoreCase)?Color.OrangeRed:Muted);}
}

public sealed class FinishStep : InstallerStepControl
{
    private readonly Label location=new(), version=new(), components=new(), download=new(), installed=new(), date=new(), status=new();
    public CheckBox LaunchWhenFinish {get;}=new(); public event EventHandler? LaunchRequested;
    public FinishStep()
    {
        var host=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(8)};host.RowStyles.Add(new RowStyle(SizeType.Absolute,80));host.RowStyles.Add(new RowStyle(SizeType.Percent,100));host.RowStyles.Add(new RowStyle(SizeType.Absolute,50));
        host.Controls.Add(Label("✓  Installation Completed Successfully!",28,true),0,0);
        var card=Card();card.Dock=DockStyle.Fill;card.Margin=new Padding(0,0,0,10);card.Controls.Add(Label("Installation Details",18,true));var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=7,BackColor=Color.Transparent,Margin=new Padding(0,44,0,0)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));string[] caps={"Installation Location:","Application Version:","Total Components:","Total Download Size:","Total Installed Size:","Installation Date:","Status:"};var vals=new[]{location,version,components,download,installed,date,status};for(int i=0;i<7;i++){grid.RowStyles.Add(new RowStyle(SizeType.Absolute,34));grid.Controls.Add(new Label{Text=caps[i],ForeColor=Muted,Dock=DockStyle.Fill},0,i);vals[i].ForeColor=i==6?Green:(i<2?Green:White);vals[i].Font=new Font("Segoe UI",10,FontStyle.Bold);vals[i].Dock=DockStyle.Fill;vals[i].AutoEllipsis=true;vals[i].TextAlign=ContentAlignment.MiddleLeft;grid.Controls.Add(vals[i],1,i);}card.Controls.Add(grid);host.Controls.Add(card,0,1);
        var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Color.Transparent};bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));LaunchWhenFinish.Text="Launch Suvidha POS when I click Finish";LaunchWhenFinish.ForeColor=White;LaunchWhenFinish.Font=new Font("Segoe UI",10,FontStyle.Bold);LaunchWhenFinish.Dock=DockStyle.Fill;LaunchWhenFinish.TextAlign=ContentAlignment.MiddleLeft;var launch=new Button{Text="▶  Launch Suvidha POS",Dock=DockStyle.Fill};StyleButton(launch,true);launch.Click+=(_,_)=>LaunchRequested?.Invoke(this,EventArgs.Empty);bottom.Controls.Add(LaunchWhenFinish,0,0);bottom.Controls.Add(launch,1,0);host.Controls.Add(bottom,0,2);Controls.Add(host);
    }
    public void SetState(InstallerState s){location.Text=s.InstallPath;version.Text=s.AppVersion;components.Text=s.Components.Count(x=>x.Selected).ToString();download.Text=FormatHelper.Size(s.TotalDownloadBytes);installed.Text=FormatHelper.Size(s.TotalInstalledBytes);date.Text=(s.InstallationDate??DateTime.Now).ToString("dd MMM yyyy hh:mm tt");status.Text=s.SetupCompleted?"Completed Successfully":"Ready";LaunchWhenFinish.Checked=true;}
}