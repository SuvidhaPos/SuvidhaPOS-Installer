using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuvidhaPosInstaller;

/// <summary>
/// Runtime-only compatibility layer. It does not replace the MainForm UI; it stabilizes
/// the existing responsive shell and makes sure the VC++ prerequisite is present before
/// the Download step is allowed to proceed.
/// </summary>
internal static class RuntimeFix
{
    private const string SoftwareFolder = @"D:\Suvidha Pos\Software";
    private const string UserVcDriveId = "1v90y9MXcOirG_mlev-IsLrlEVuFa3AIK";
    private const string OfficialVcX64Url = "https://aka.ms/vc14/vc_redist.x64.exe";
    private const string VcFileName = "vcredist.x64.exe";

    private static readonly HttpClient Http = CreateHttpClient();
    private static Task<bool>? vcTask;
    private static readonly FieldInfo StepField = Field("step");
    private static readonly FieldInfo BusyField = Field("busy");
    private static readonly FieldInfo NextField = Field("nextButton");
    private static readonly FieldInfo ContentField = Field("content");

    public static void Apply(MainForm form)
    {
        form.MinimumSize = new Size(1024, 768);
        if (form.ClientSize.Width < 1024 || form.ClientSize.Height < 768)
            form.ClientSize = new Size(1366, 768);

        var content = ContentField.GetValue(form) as Panel;
        if (content != null)
        {
            content.ControlAdded += (_, _) =>
            {
                InvokeResponsiveShell(form);
                StartVcPrefetchIfNeeded(form);
            };
        }

        form.Shown += (_, _) =>
        {
            InvokeResponsiveShell(form);
            StartVcPrefetchIfNeeded(form);
        };
        form.Resize += (_, _) => InvokeResponsiveShell(form);
    }

    private static void InvokeResponsiveShell(MainForm form)
    {
        try
        {
            typeof(MainForm).GetMethod("ApplyResponsiveShell", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(form, null);
        }
        catch { }
    }

    private static void StartVcPrefetchIfNeeded(MainForm form)
    {
        if (GetStep(form) != 2) return;
        if (BusyField.GetValue(form) is true) return;
        var next = NextField.GetValue(form) as Button;
        if (next == null) return;

        string target = Path.Combine(SoftwareFolder, VcFileName);
        if (IsGoodInstaller(target) && IsPeFile(target))
        {
            next.Enabled = true;
            return;
        }

        if (vcTask == null)
        {
            vcTask = DownloadVcRedistAsync(target);
            next.Enabled = false;
            _ = vcTask.ContinueWith(t =>
            {
                if (form.IsDisposed || !form.IsHandleCreated) return;
                form.BeginInvoke(new Action(() =>
                {
                    if (GetStep(form) != 2) return;
                    if (NextField.GetValue(form) is Button currentNext)
                    {
                        currentNext.Enabled = t.Status == TaskStatus.RanToCompletion && t.Result;
                        if (!currentNext.Enabled)
                        {
                            MessageBox.Show(form,
                                "Microsoft Visual C++ Redistributable could not be downloaded.\n\nPlease check the Internet connection and try again.",
                                "Download failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }));
            }, TaskScheduler.Default);
        }
        else
        {
            next.Enabled = vcTask.Status == TaskStatus.RanToCompletion && vcTask.Result;
        }
    }

    private static async Task<bool> DownloadVcRedistAsync(string target)
    {
        Directory.CreateDirectory(SoftwareFolder);
        TryDelete(target);

        try
        {
            await DownloadGoogleDriveAsync(UserVcDriveId, target);
            if (IsGoodInstaller(target) && IsPeFile(target)) return true;
        }
        catch { TryDelete(target); }

        try
        {
            await DownloadHttpFileAsync(OfficialVcX64Url, target);
            return IsGoodInstaller(target) && IsPeFile(target);
        }
        catch
        {
            TryDelete(target);
            return false;
        }
    }

    private static async Task DownloadGoogleDriveAsync(string fileId, string target)
    {
        using var first = await Http.GetAsync(
            $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(fileId)}&export=download&confirm=t",
            HttpCompletionOption.ResponseHeadersRead);
        first.EnsureSuccessStatusCode();

        string media = first.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (!media.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            await SaveResponseAsync(first, target);
            return;
        }

        string html = await first.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"confirm=([0-9A-Za-z_-]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new InvalidOperationException("Google Drive returned a download page instead of the VC++ installer.");

        using var second = await Http.GetAsync(
            $"https://drive.usercontent.google.com/download?id={Uri.EscapeDataString(fileId)}&export=download&confirm={Uri.EscapeDataString(match.Groups[1].Value)}",
            HttpCompletionOption.ResponseHeadersRead);
        second.EnsureSuccessStatusCode();
        await SaveResponseAsync(second, target);
    }

    private static async Task DownloadHttpFileAsync(string url, string target)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await SaveResponseAsync(response, target);
    }

    private static async Task SaveResponseAsync(HttpResponseMessage response, string target)
    {
        string media = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (media.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Download endpoint returned HTML instead of an installer file.");

        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);
        await input.CopyToAsync(output);
    }

    private static bool IsGoodInstaller(string path) => File.Exists(path) && new FileInfo(path).Length > 500_000;

    private static bool IsPeFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            return b1 == 'M' && b2 == 'Z';
        }
        catch { return false; }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private static int GetStep(MainForm form) =>
        StepField.GetValue(form) is int value ? Math.Clamp(value, 0, 6) : 0;

    private static FieldInfo Field(string name) =>
        typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(MainForm).FullName, name);

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SuvidhaPOS-Installer/2.3");
        return client;
    }
}
