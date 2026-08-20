$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = '.\Installer\SuvidhaPos-Installer.csproj'
$publish = '.\publish'
$release = '.\release'

Write-Host '=== Suvidha POS Installer V2 - Windows x64 build ==='

if (-not (Test-Path $project)) {
    throw "Project file not found: $project"
}

Write-Host '[1/5] Restore .NET project'
dotnet restore $project --runtime win-x64

Write-Host '[2/5] Build application'
dotnet build $project `
    --configuration Release `
    --framework net8.0-windows `
    --runtime win-x64 `
    --no-restore

Write-Host '[3/5] Publish self-contained x64 application'
if (Test-Path $publish) {
    Remove-Item $publish -Recurse -Force
}

dotnet publish $project `
    --configuration Release `
    --framework net8.0-windows `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $publish `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false

$requiredPublishFiles = @(
    "$publish\SuvidhaPos-Installer.exe",
    "$publish\SuvidhaPos-Installer.dll",
    "$publish\Assets\SuvidhaPOS.ico",
    "$publish\Assets\SuvidhaPOS.png"
)

foreach ($file in $requiredPublishFiles) {
    if (-not (Test-Path $file)) {
        throw "Required publish output was not created: $file"
    }
}

Write-Host '[4/5] Build Inno Setup installer'
$isccCandidates = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw 'Inno Setup 6 is not installed. Install Inno Setup 6 and run this script again.'
}

if (Test-Path $release) {
    Remove-Item $release -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $release | Out-Null

& $iscc '.\Installer\SuvidhaPos-Installer.iss'
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$setup = "$release\SuvidhaPOS-Installer-Setup.exe"
if (-not (Test-Path $setup)) {
    throw "Final installer EXE was not created: $setup"
}

Write-Host '[5/5] Final validation'
$setupInfo = Get-Item $setup
if ($setupInfo.Length -lt 100KB) {
    throw "Final installer is unexpectedly small ($($setupInfo.Length) bytes)."
}

Write-Host ''
Write-Host 'BUILD SUCCESSFUL'
Write-Host "Installer: $setup"
Write-Host "Size: $([math]::Round($setupInfo.Length / 1MB, 2)) MB"
