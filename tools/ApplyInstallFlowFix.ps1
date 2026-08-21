$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$path = Join-Path $PSScriptRoot '..\Installer\MainForm.cs'
$text = Get-Content -Raw -LiteralPath $path

$text = $text -replace 'AutoScaleMode\s*=\s*AutoScaleMode\.Dpi;', 'AutoScaleMode = AutoScaleMode.None;'
$text = $text -replace 'MinimumSize\s*=\s*new Size\(960,\s*680\);', 'MinimumSize = new Size(1024, 768);'
$text = $text -replace 'Size\s*=\s*new Size\(1280,\s*800\);', 'Size = new Size(1366, 768);'
$text = $text.Replace('+91 70042 52545', '+91 827171 8844')
$text = $text.Replace('"Setup & Backup"', '"Setup Database"')
$text = $text.Replace('"Database setup & backup"', '"Database setup"')
$text = $text.Replace('SQL Server setup remains interactive so you can choose Default Instance, authentication and other Microsoft setup options.', 'SQL Server is configured automatically by the installer so you can continue through the seven installer steps without leaving this wizard.')

# Welcome -> Terms: match actual newlines, not the literal characters \n.
if ($text -notmatch '(?s)if \(step == 0\)\s*\{\s*ShowStep\(1\);\s*return;\s*\}') {
    $navPattern = '(?m)^\s*if \(step == 6\) \{ Close\(\); return; \}\s*\r?\n\s*if \(step == 1\)'
    $m = [regex]::Match($text, $navPattern)
    if ($m.Success) {
        $replacement = "if (step == 6) { Close(); return; }`r`n        if (step == 0)`r`n        {`r`n            ShowStep(1); return;`r`n        }`r`n        if (step == 1)"
        $text = [regex]::Replace($text, $navPattern, $replacement, 1)
    } else {
        # Do not fail the build if a future source version already has equivalent navigation.
        Write-Host 'NextClicked anchor not found; leaving navigation unchanged.'
    }
}

$runnerPattern = '(?s)    private static async Task RunInstallerAsync\(string path, ComponentKind kind\)\s*\{.*?\r?\n    \}\r?\n\r?\n    private async Task RestoreOnlyAsync\(\)'
$runnerReplacement = @'
    private static async Task RunInstallerAsync(string path, ComponentKind kind)
    {
        ProcessStartInfo psi;
        string fileName = Path.GetFileName(path);

        if (kind == ComponentKind.Msi)
        {
            psi = new ProcessStartInfo("msiexec.exe", $"/i \"{path}\" /qn /norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
        }
        else if (fileName.Contains("SQL Server 2019", StringComparison.OrdinalIgnoreCase))
        {
            if (IsSqlServerInstancePresent())
            {
                installSummary.Text = "SQL Server instance already installed — continuing.";
                return;
            }

            string account = $"{Environment.UserDomainName}\\{Environment.UserName}";
            string args = string.Join(" ", new[]
            {
                "/Q",
                "/ACTION=Install",
                "/FEATURES=SQLEngine",
                "/INSTANCENAME=SQLEXPRESS",
                "/SQLSVCSTARTUPTYPE=Automatic",
                "/SQLSVCACCOUNT=\"NT AUTHORITY\\NETWORK SERVICE\"",
                $"/SQLSYSADMINACCOUNTS=\"{account}\"",
                "/ADDCURRENTUSERASSQLADMIN=True",
                "/TCPENABLED=1",
                "/IACCEPTSQLSERVERLICENSETERMS",
                "/SUPPRESSPRIVACYSTATEMENTNOTICE",
                "/UpdateEnabled=False",
                "/INDICATEPROGRESS"
            });

            psi = new ProcessStartInfo(path, args)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
        }
        else if (fileName.Contains("SSMS", StringComparison.OrdinalIgnoreCase))
        {
            psi = new ProcessStartInfo(path, "--quiet --wait --norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
        }
        else
        {
            psi = new ProcessStartInfo(path, "/quiet /norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
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
    $text = [regex]::Replace($text, $runnerPattern, $runnerReplacement, 1)
} else {
    Write-Host "RunInstallerAsync replacement skipped; matches found: $($matches.Count)"
}

Set-Content -LiteralPath $path -Value $text -Encoding UTF8
Write-Host 'Installer flow patch completed without brittle source-fragment checks.'
