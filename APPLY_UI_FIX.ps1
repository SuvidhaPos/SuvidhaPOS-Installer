$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$main = Join-Path $root "Installer\MainForm.cs"
$ui = Join-Path $root "Installer\UiPolish.cs"

if (!(Test-Path $main)) { throw "Installer\MainForm.cs not found. Run this script from the repository root." }

$text = [IO.File]::ReadAllText($main)
$text = $text.Replace('AutoScaleMode = AutoScaleMode.Dpi;', 'AutoScaleMode = AutoScaleMode.None;')
$text = $text.Replace(
    'private static readonly string DownloadDir = Path.Combine(DataDir, "Downloads");',
    'private static readonly string DownloadDir = SoftwareFolder;'
)
[IO.File]::WriteAllText($main, $text, [Text.UTF8Encoding]::new($false))

if (!(Test-Path $ui)) { throw "Installer\UiPolish.cs not found." }
[IO.File]::WriteAllText($ui, [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Installer\UiPolish.cs")), [Text.UTF8Encoding]::new($false))

Write-Host "UI/build patch applied."
Write-Host "Download folder: D:\Suvidha Pos\Software"
Write-Host "Now run: .\build.ps1"
