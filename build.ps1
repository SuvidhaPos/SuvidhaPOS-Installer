$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = '.\Installer\SuvidhaPos-Installer.csproj'
$publish = '.\publish'
$release = '.\release'
$iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
if (Test-Path $release) { Remove-Item $release -Recurse -Force }

dotnet restore $project --runtime win-x64

dotnet build $project `
  --configuration Release `
  --framework net8.0-windows `
  --runtime win-x64 `
  --no-restore

dotnet publish $project `
  --configuration Release `
  --framework net8.0-windows `
  --runtime win-x64 `
  --self-contained true `
  --no-restore `
  --output $publish `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=false `
  -p:IncludeNativeLibrariesForSelfExtract=false `
  -p:EnableCompressionInSingleFile=false

$exe = Join-Path $publish 'SuvidhaPos-Installer.exe'
if (-not (Test-Path $exe)) { throw 'Published application EXE was not created.' }

foreach ($asset in @('Assets\SuvidhaPOS.png','Assets\SuvidhaPOS.ico')) {
    if (-not (Test-Path (Join-Path $publish $asset))) {
        throw "Published asset missing: $asset"
    }
}

if (-not (Test-Path $iscc)) { throw 'Inno Setup 6 is not installed.' }

New-Item -ItemType Directory -Force -Path $release | Out-Null
& $iscc '.\Installer\SuvidhaPos-Installer.iss'
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

$setup = Join-Path $release 'SuvidhaPOS-Installer-Setup.exe'
if (-not (Test-Path $setup)) { throw 'Final installer EXE was not created.' }

Write-Host "BUILD SUCCESSFUL: $setup"
