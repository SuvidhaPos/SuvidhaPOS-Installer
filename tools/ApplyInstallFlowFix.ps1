$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$path = Join-Path $PSScriptRoot '..\Installer\MainForm.cs'
$text = Get-Content -Raw -LiteralPath $path

function Replace-Regex([string]$pattern, [string]$replacement, [string]$label) {
    $updated = [regex]::Replace($script:text, $pattern, $replacement)
    if ($updated -ne $script:text) {
        $script:text = $updated
        Write-Host "Applied: $label"
    } else {
        Write-Host "No change needed: $label"
    }
}

Replace-Regex 'AutoScaleMode\s*=\s*AutoScaleMode\.Dpi;' 'AutoScaleMode = AutoScaleMode.None;' 'disable WinForms automatic DPI scaling'
Replace-Regex 'MinimumSize\s*=\s*new Size\(960,\s*680\);' 'MinimumSize = new Size(1024, 768);' 'minimum window size'
Replace-Regex 'Size\s*=\s*new Size\(1280,\s*800\);' 'Size = new Size(1366, 768);' 'default window size'
Replace-Regex '"Setup & Backup"' '"Setup Database"' 'step 6 title'
Replace-Regex '"Database setup & backup"' '"Database setup"' 'step 6 subtitle'
Replace-Regex '\+91 70042 52545' '+91 827171 8844' 'support phone'

# Welcome -> Terms navigation. This is optional and idempotent.
if ($text -notmatch '(?s)if \(step == 0\)\s*\{\s*ShowStep\(1\);\s*return;\s*\}') {
    $navPattern = '(?m)^\s*if \(step == 6\) \{ Close\(\); return; \}\s*\r?\n\s*if \(step == 1\)'
    if ([regex]::IsMatch($text, $navPattern)) {
        $replacement = @'
        if (step == 6) { Close(); return; }
        if (step == 0)
        {
            ShowStep(1); return;
        }
        if (step == 1)
'@
        $text = [regex]::Replace($text, $navPattern, $replacement.TrimEnd("`r", "`n"), 1)
        Write-Host 'Applied: Welcome -> Terms navigation'
    }
}

# Important: the replacement below is valid C# source. The single-quoted PowerShell
# here-string preserves normal C# quotes and does not inject literal backslashes before them.
$runnerPattern = '(?s)    private static async Task RunInstallerAsync\(string path, ComponentKind kind\)\s*\{.*?\r?\n    \}\r?\n\r?\n    private async Task RestoreOnlyAsync\(\)'
$runnerReplacement = @'
    private static async Task RunInstallerAsync(string path, ComponentKind kind)
    {
        ProcessStartInfo psi;
        string fileName = Path.GetFileName(path);

        if (kind == ComponentKind.Msi)
        {
            psi = new ProcessStartInfo("msiexec.exe")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("/i");
            psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("/qn");
            psi.ArgumentList.Add("/norestart");
        }
        else if (fileName.Contains("SQL Server 2019", StringComparison.OrdinalIgnoreCase))
        {
            if (IsSqlServerInstancePresent())
            {
                installSummary.Text = "SQL Server instance already installed — continuing.";
                return;
            }

            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };

            foreach (var arg in new[]
            {
                "/Q",
                "/ACTION=Install",
                "/FEATURES=SQLEngine",
                "/INSTANCENAME=SQLEXPRESS",
                "/SQLSVCSTARTUPTYPE=Automatic",
                "/ADDCURRENTUSERASSQLADMIN=True",
                "/TCPENABLED=1",
                "/IACCEPTSQLSERVERLICENSETERMS",
                "/SUPPRESSPRIVACYSTATEMENTNOTICE",
                "/UpdateEnabled=False",
                "/INDICATEPROGRESS"
            })
            {
                psi.ArgumentList.Add(arg);
            }
        }
        else if (fileName.Contains("SSMS", StringComparison.OrdinalIgnoreCase))
        {
            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("--quiet");
            psi.ArgumentList.Add("--wait");
            psi.ArgumentList.Add("--norestart");
        }
        else
        {
            psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
            psi.ArgumentList.Add("/quiet");
            psi.ArgumentList.Add("/norestart");
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Windows could not start the installer.");
        await p.WaitForExitAsync();
        if (p.ExitCode != 0 && p.ExitCode != 3010 && p.ExitCode != 1641)
            throw new InvalidOperationException($"Installer exited with code {p.ExitCode}.");
    }

    private static bool IsSqlServerInstancePresent()
    {
        foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (key == null) continue;
                if (key.GetValue("SQLEXPRESS") != null || key.GetValue("MSSQLSERVER") != null)
                    return true;
            }
            catch { }
        }
        return false;
    }

    private async Task RestoreOnlyAsync()
'@

$matches = [regex]::Matches($text, $runnerPattern)
if ($matches.Count -eq 1) {
    $text = [regex]::Replace($text, $runnerPattern, $runnerReplacement.TrimEnd("`r", "`n"), 1)
    Write-Host 'Applied: valid silent installer runner'
} else {
    Write-Host "RunInstallerAsync replacement skipped; matches found: $($matches.Count)"
}

Set-Content -LiteralPath $path -Value $text -Encoding UTF8
Write-Host 'Installer flow patch completed successfully.'
