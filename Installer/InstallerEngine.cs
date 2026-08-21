using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using SuvidhaPOSInstaller.DynamicUI.Models;

namespace SuvidhaPOSInstaller.DynamicUI;

public sealed class InstallerEngine : IDisposable
{
    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All });
    private readonly string resumeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SuvidhaPOS", "Installer", "state.json");
    private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
    private DateTime downloadStarted;
    private DateTime installStarted;
    private bool disposed;

    private const string SqlDriveId = "1y5d9D1tkOn81dN6I7aPtkd4DRQJCtmhT";
    private const string SsMsDriveId = "1QFaDEaOb-qGhqLIfqpXrHYhDeCp19gPT";
    private const string CrystalDriveId = "1u3YBZqVdx5tIOPh8RU19FrOjWC-Np05v";
    private const string VcDriveId = "1v90y9MXcOirG_mlev-IsLrlEVuFa3AIK";

    public InstallerState State { get; } = new();
    public bool Busy { get; private set; }
    public event EventHandler? StateChanged;

    public InstallerEngine()
    {
        State.InstallPath = @"D:\Suvidha Pos\Software";
        State.Download.Location = Path.Combine(State.InstallPath, "Downloads");
        State.Installation.Location = State.InstallPath;
        State.Database.ServerName = Environment.MachineName + @"\SQLEXPRESS";
        State.AppVersion = "2.3.1.0";
        LoadState();
        SeedComponents();
        RefreshLocalFiles();
    }

    private void SeedComponents()
    {
        if (State.Components.Count > 0) return;
        State.Components.Add(new InstallerComponent { Name="Microsoft SQL Server 2019", Description="Core database engine required for Suvidha POS", Id=SqlDriveId, Kind="Exe" });
        State.Components.Add(new InstallerComponent { Name="SQL Server Management Studio (SSMS)", Description="Database management and administration tool", Id=SsMsDriveId, Kind="Exe" });
        State.Components.Add(new InstallerComponent { Name="Crystal Reports Runtime (64-bit)", Description="Reports runtime required by Suvidha POS", Id=CrystalDriveId, Kind="Msi" });
        State.Components.Add(new InstallerComponent { Name="Microsoft Visual C++ Redistributable", Description="Required Windows runtime libraries", Id=VcDriveId, Kind="Exe" });
        State.Components.Add(new InstallerComponent { Name="Suvidha POS Application", Description="Main Suvidha POS desktop application", Id="LOCAL_POS", Kind="Local" });
    }

    public void SetStep(int step) { State.CurrentStep = Math.Clamp(step, 1, 7); SaveState(); Raise(); }
    public void SetSelected(string name, bool selected) { var c=Find(name); if(c!=null)c.Selected=selected; SaveState(); Raise(); }

    public async Task DownloadAllAsync()
    {
        if (Busy) return;
        Busy = true; State.Error = null; downloadStarted=DateTime.UtcNow; Raise();
        Directory.CreateDirectory(State.InstallPath); Directory.CreateDirectory(State.Download.Location);
        var selected=State.Components.Where(x=>x.Selected).ToList();
        State.Download.TotalBytes=0; State.Download.DownloadedBytes=0;
        foreach(var c in selected){ if(c.SizeBytes>0) State.Download.TotalBytes += c.SizeBytes; }
        foreach (var c in selected)
        {
            try
            {
                if (c.Kind.Equals("Local", StringComparison.OrdinalIgnoreCase))
                {
                    var local = c.Name == "Suvidha POS Application" ? FindLocalPosMsi() : FindLocalFile(c.Name);
                    if (local == null) throw new FileNotFoundException($"{c.Name} was not found in {State.InstallPath}.");
                    files[c.Name]=local; c.SizeBytes=new FileInfo(local).Length; c.Progress=100; c.Status="Ready"; RecalculateDownloadTotal(); Raise(); continue;
                }
                var ext = c.Kind.Equals("Msi",StringComparison.OrdinalIgnoreCase)?".msi":".exe";
                var target=Path.Combine(State.InstallPath,SafeFileName(c.Name)+ext);
                if(File.Exists(target) && new FileInfo(target).Length>100*1024){ files[c.Name]=target; c.SizeBytes=new FileInfo(target).Length;c.Progress=100;c.Status="Downloaded"; RecalculateDownloadTotal();Raise();continue; }
                c.Status="Downloading..."; c.Progress=0; Raise();
                await DownloadDriveFileAsync(c,target);
                files[c.Name]=target; c.SizeBytes=new FileInfo(target).Length;c.Progress=100;c.Status="Downloaded"; RecalculateDownloadTotal();Raise();
            }
            catch(Exception ex){ c.Status="Failed"; c.Error=ex.Message; State.Error=$"Failed: {c.Name} — {ex.Message}"; Raise(); Busy=false; return; }
        }
        State.Download.DownloadedBytes=State.Download.TotalBytes; State.Download.RemainingTime=TimeSpan.Zero; Raise(); SaveState(); Busy=false;
    }

    private async Task DownloadDriveFileAsync(InstallerComponent c,string target)
    {
        var url=$"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(c.Id)}&export=download&confirm=t";
        using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead); response.EnsureSuccessStatusCode();
        var media=response.Content.Headers.ContentType?.MediaType;
        if(!string.IsNullOrWhiteSpace(media)&&media.Contains("text/html",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Google Drive returned a confirmation page instead of the installer file.");
        var total=response.Content.Headers.ContentLength ?? -1L;
        if(total>0){ c.SizeBytes=total; RecalculateDownloadTotal(); }
        await using var input=await response.Content.ReadAsStreamAsync();
        await using var output=new FileStream(target,FileMode.Create,FileAccess.Write,FileShare.None,131072,true);
        var buffer=new byte[131072]; long done=0; int read;
        while((read=await input.ReadAsync(buffer.AsMemory(0,buffer.Length)))>0)
        {
            await output.WriteAsync(buffer.AsMemory(0,read)); done+=read;
            c.Progress=total>0?Math.Clamp((int)(done*100L/total),0,100):0; c.Status="Downloading...";
            State.Download.DownloadedBytes=State.Components.Where(x=>x.Selected).Sum(x=>x.SizeBytes*x.Progress/100L);
            if(State.Download.TotalBytes>0)State.Download.DownloadedBytes=Math.Clamp(State.Download.DownloadedBytes,0,State.Download.TotalBytes);
            var elapsed=DateTime.UtcNow-downloadStarted; if(State.Download.DownloadedBytes>0&&elapsed.TotalSeconds>1){var rate=State.Download.DownloadedBytes/elapsed.TotalSeconds; var remain=State.Download.TotalBytes-State.Download.DownloadedBytes;State.Download.RemainingTime=rate>0?TimeSpan.FromSeconds(remain/rate):TimeSpan.Zero;}
            Raise();
        }
    }

    private void RecalculateDownloadTotal(){State.Download.TotalBytes=State.Components.Where(x=>x.Selected).Sum(x=>x.SizeBytes);}

    public async Task InstallAllAsync()
    {
        if(Busy)return; Busy=true; State.Error=null; installStarted=DateTime.UtcNow; State.Installation.TotalBytes=State.Components.Where(x=>x.Selected).Sum(x=>x.SizeBytes); State.Installation.InstalledBytes=0; Raise();
        var selected=State.Components.Where(x=>x.Selected).ToList(); int completed=0;
        foreach(var c in selected)
        {
            if(!files.TryGetValue(c.Name,out var path)||!File.Exists(path)){c.Status="Failed";c.Error="Installer file not found.";State.Error=$"Failed: {c.Name} — installer file not found.";Raise();Busy=false;return;}
            c.Status="Installing..."; c.Progress=5; Raise();
            try
            {
                await RunInstallerAsync(path,c.Kind);
                c.Progress=100;c.Status="Installed";completed++;State.Installation.InstalledBytes=selected.Where(x=>x.Status=="Installed").Sum(x=>x.SizeBytes);
                if(State.Installation.TotalBytes>0) State.Installation.RemainingTime=EstimateRemaining(State.Installation.InstalledBytes,State.Installation.TotalBytes,installStarted);
                Raise();
            }
            catch(Exception ex){c.Status="Failed";c.Error=ex.Message;State.Error=$"Failed: {c.Name} — {ex.Message}";Raise();Busy=false;return;}
        }
        State.Installation.InstalledBytes=State.Installation.TotalBytes;State.Installation.RemainingTime=TimeSpan.Zero;State.TotalInstalledBytes=State.Components.Where(x=>x.Status=="Installed"||x.Status=="Ready").Sum(x=>x.SizeBytes);State.InstallationDate=DateTime.Now;Raise();SaveState();Busy=false;
    }

    private static TimeSpan EstimateRemaining(long done,long total,DateTime started){if(done<=0||total<=done)return TimeSpan.Zero;var elapsed=DateTime.UtcNow-started;var rate=done/Math.Max(1,elapsed.TotalSeconds);return rate<=0?TimeSpan.Zero:TimeSpan.FromSeconds((total-done)/rate);}

    private static async Task RunInstallerAsync(string path,string kind)
    {
        ProcessStartInfo psi;
        if(kind.Equals("Msi",StringComparison.OrdinalIgnoreCase))
        {
            psi=new ProcessStartInfo("msiexec.exe"){UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(path)!};
            psi.ArgumentList.Add("/i");psi.ArgumentList.Add(path);psi.ArgumentList.Add("/norestart");
        }
        else
        {
            psi=new ProcessStartInfo(path){UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(path)!};
        }
        using var p=Process.Start(psi)??throw new InvalidOperationException("Windows could not start the installer."); await p.WaitForExitAsync();
        if(p.ExitCode!=0&&p.ExitCode!=3010&&p.ExitCode!=1641)throw new InvalidOperationException($"Installer exited with code {p.ExitCode}.");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try{var cs=new SqlConnectionStringBuilder{DataSource=State.Database.ServerName,InitialCatalog="master",IntegratedSecurity=true,TrustServerCertificate=true,ConnectTimeout=10}.ConnectionString;await using var cn=new SqlConnection(cs);await cn.OpenAsync();State.Database.ConnectionSuccessful=true;State.Database.RestoreStatus="SQL Server connection successful ✓";Raise();return true;}
        catch(Exception ex){State.Database.ConnectionSuccessful=false;State.Database.RestoreStatus="Connection failed: "+ex.Message;Raise();return false;}
    }

    public async Task<bool> RestoreAsync()
    {
        var backup=State.BackupPath;var server=State.Database.ServerName;var db=State.Database.DatabaseName;
        if(string.IsNullOrWhiteSpace(backup)||!File.Exists(backup)){State.Database.RestoreStatus="Backup file not found.";Raise();return false;}
        if(string.IsNullOrWhiteSpace(server)||string.IsNullOrWhiteSpace(db)){State.Database.RestoreStatus="Server Name and Database Name are required.";Raise();return false;}
        Busy=true;State.Database.RestoreStatus="Restoring database...";Raise();
        try{var cs=new SqlConnectionStringBuilder{DataSource=server,IntegratedSecurity=true,TrustServerCertificate=true,ConnectTimeout=15}.ConnectionString;await using var cn=new SqlConnection(cs);await cn.OpenAsync();var escapedDb=db.Replace("]", "]]", StringComparison.Ordinal);var escapedPath=backup.Replace("'","''",StringComparison.Ordinal);var sql=$"IF DB_ID(N'{db.Replace("'","''",StringComparison.Ordinal)}') IS NOT NULL BEGIN ALTER DATABASE [{escapedDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; END; RESTORE DATABASE [{escapedDb}] FROM DISK = N'{escapedPath}' WITH REPLACE; ALTER DATABASE [{escapedDb}] SET MULTI_USER;";await using var cmd=new SqlCommand(sql,cn){CommandTimeout=1800};await cmd.ExecuteNonQueryAsync();State.Database.RestoreStatus="Database restore completed successfully ✓";Raise();SaveState();return true;}
        catch(Exception ex){State.Database.RestoreStatus="Restore failed: "+ex.Message;Raise();return false;}
        finally{Busy=false;}
    }

    public bool SaveConfiguration()
    {
        var server=State.Database.ServerName?.Trim();var db=State.Database.DatabaseName?.Trim();if(string.IsNullOrWhiteSpace(server)||string.IsNullOrWhiteSpace(db)){State.Error="Server Name and Database Name are required.";Raise();return false;}
        var found=FindInstalledPos();if(found==null){State.Error="SuvidhaPos.exe.config / RetailPos.exe.config was not found.";Raise();return false;}
        try{var doc=XDocument.Load(found.Value.ConfigPath,LoadOptions.PreserveWhitespace);var add=doc.Descendants("add").FirstOrDefault(x=>string.Equals((string?)x.Attribute("key"),"sqlKey",StringComparison.OrdinalIgnoreCase));if(add==null){State.Error="sqlKey entry was not found in the configuration file.";Raise();return false;}add.SetAttributeValue("value",$"Data Source={server};Initial Catalog={db};Integrated Security=True");doc.Save(found.Value.ConfigPath);State.Error=null;State.SetupCompleted=true;SaveState();Raise();return true;}catch(Exception ex){State.Error="Could not save configuration: "+ex.Message;Raise();return false;}
    }

    public bool LaunchPos(){var found=FindInstalledPos();if(found==null){State.Error="SuvidhaPos.exe / RetailPos.exe was not found.";Raise();return false;}try{Process.Start(new ProcessStartInfo(found.Value.ExePath){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(found.Value.ExePath)!});return true;}catch(Exception ex){State.Error=ex.Message;Raise();return false;}}

    private (string ExePath,string ConfigPath)? FindInstalledPos(){var roots=new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"SuvidhaPOS"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"SuvidhaPOS")}.Where(Directory.Exists);foreach(var root in roots){try{foreach(var exeName in new[]{"SuvidhaPos.exe","RetailPos.exe"}){var exe=Directory.EnumerateFiles(root,exeName,SearchOption.AllDirectories).FirstOrDefault();if(exe!=null&&File.Exists(exe+".config"))return(exe,exe+".config");}}catch{}}return null;}
    private string? FindLocalPosMsi(){if(!Directory.Exists(State.InstallPath))return null;var msis=Directory.EnumerateFiles(State.InstallPath,"*.msi",SearchOption.TopDirectoryOnly);return msis.FirstOrDefault(x=>Path.GetFileName(x).Contains("suvidha",StringComparison.OrdinalIgnoreCase))??msis.FirstOrDefault();}
    private string? FindLocalFile(string name){if(!Directory.Exists(State.InstallPath))return null;var exe=Directory.EnumerateFiles(State.InstallPath,"*.exe",SearchOption.TopDirectoryOnly).FirstOrDefault(x=>Path.GetFileName(x).Contains("vcredist",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(x).Contains("vc_redist",StringComparison.OrdinalIgnoreCase));return name.Contains("Visual",StringComparison.OrdinalIgnoreCase)?exe:null;}
    private void RefreshLocalFiles(){var pos=FindLocalPosMsi();if(pos!=null){var c=Find("Suvidha POS Application")!;c.SizeBytes=new FileInfo(pos).Length;c.Status="Ready";}var vc=FindLocalFile("Microsoft Visual C++");if(vc!=null){var c=Find("Microsoft Visual C++ Redistributable")!;c.SizeBytes=new FileInfo(vc).Length;c.Status="Ready";}}
    private InstallerComponent? Find(string name)=>State.Components.FirstOrDefault(x=>string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase));
    private static string SafeFileName(string s){foreach(var c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s;}
    private void Raise()=>StateChanged?.Invoke(this,EventArgs.Empty);
    private void SaveState(){try{Directory.CreateDirectory(Path.GetDirectoryName(resumeFile)!);File.WriteAllText(resumeFile,JsonSerializer.Serialize(State,new JsonSerializerOptions{WriteIndented=true}));}catch{}}
    private void LoadState(){try{if(File.Exists(resumeFile)){var saved=JsonSerializer.Deserialize<InstallerState>(File.ReadAllText(resumeFile));if(saved!=null){State.CurrentStep=saved.CurrentStep;State.InstallPath=saved.InstallPath;State.AppVersion=saved.AppVersion;State.InstallationDate=saved.InstallationDate;State.BackupPath=saved.BackupPath;State.SetupCompleted=saved.SetupCompleted;State.Database=saved.Database;}}}catch{}}
    public void Dispose(){if(disposed)return;disposed=true;http.Dispose();}
}