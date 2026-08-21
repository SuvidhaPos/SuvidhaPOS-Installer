$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$path = Join-Path $PSScriptRoot '..\Installer\MainForm.cs'
$text = Get-Content -Raw -LiteralPath $path

# Make the source itself DPI-stable before controls are created.
$text = $text -replace 'AutoScaleMode = AutoScaleMode\.Dpi;', 'AutoScaleMode = AutoScaleMode.None;'
$text = $text -replace '\bMinimumSize = new Size\(960, 680\);', 'MinimumSize = new Size(1024, 768);'
$text = $text -replace '\bSize = new Size\(1280, 800\);', 'Size = new Size(1366, 768);'
$text = $text.Replace('+91 70042 52545', '+91 827171 8844')
$text = $text.Replace('"Setup & Backup"', '"Setup Database"')
$text = $text.Replace('"Database setup & backup"', '"Database setup"')
$text = $text.Replace('SQL Server setup remains interactive so you can choose Default Instance, authentication and other Microsoft setup options.', 'SQL Server is configured automatically by the installer so you can continue through the seven installer steps without leaving this wizard.')

# Welcome -> Terms must work from the first screen.
$nextAnchor = 'if (step == 6) { Close(); return; }\n        if (step == 1)'
if (-not $text.Contains('if (step == 0)\n        {')) {
    if (-not $text.Contains($nextAnchor)) { throw 'NextClicked navigation anchor not found.' }
    $text = $text.Replace($nextAnchor, 'if (step == 6) { Close(); return; }\n        if (step == 0)\n        {\n            ShowStep(1); return;\n        }\n        if (step == 1)')
}

$pattern = '(?s)    private static async Task RunInstallerAsync\(string path, ComponentKind kind\)\s*\{.*?\r?\n    \}\r?\n\r?\n    private async Task RestoreOnlyAsync\(\)'
$replacement = @'
    private async Task RunInstallerAsync(string path, ComponentKind kind)
    {
        ProcessStartInfo psi;
        string fileName = Path.GetFileName(path);

        if (kind == ComponentKind.Msi)
        {
            // MSI packages run silently so Step 5 remains the only visible wizard.
            psi = new ProcessStartInfo("msiexec.exe", $"/i \"{path}\" /qn /norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
        }
        else if (fileName.Contains("SQL Server 2019", StringComparison.OrdinalIgnoreCase))
        {
            // SQL Server 2019 Express is installed unattended inside Step 5.
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
            // SSMS uses the Visual Studio bootstrapper command-line syntax.
            // --quiet prevents a second wizard window; --wait keeps Step 5 blocked until completion.
            string args = "--quiet --wait --norestart";
            psi = new ProcessStartInfo(path, args)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(path)!
            };
        }
        else
        {
            // VC++ and other supported EXE prerequisites.
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

$match = [regex]::Matches($text, $pattern)
if ($match.Count -ne 1) { throw "Could not locate RunInstallerAsync. Matches found: $($match.Count)" }
$text = [regex]::Replace($text, $pattern, $replacement, 1)

Set-Content -LiteralPath $path -Value $text -Encoding UTF8
Write-Host "Applied silent seven-step install flow and DPI/navigation fixes."
