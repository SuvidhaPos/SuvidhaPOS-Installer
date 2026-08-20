$ErrorActionPreference = 'Stop'

dotnet restore .\Installer\SuvidhaPos-Installer.csproj --runtime win-x64
dotnet build .\Installer\SuvidhaPos-Installer.csproj -c Release --no-restore --runtime win-x64
dotnet publish .\Installer\SuvidhaPos-Installer.csproj -c Release -r win-x64 --self-contained true --no-restore -o .\publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if (-not (Test-Path .\publish\SuvidhaPos-Installer.exe)) { throw 'Published application EXE was not created.' }

$iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
if (-not (Test-Path $iscc)) { throw 'Inno Setup 6 is not installed.' }

if (Test-Path .\release) { Remove-Item .\release -Recurse -Force }
New-Item -ItemType Directory -Force -Path .\release | Out-Null
& $iscc .\Installer\SuvidhaPos-Installer.iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }
if (-not (Test-Path .\release\SuvidhaPos-Installer-Setup.exe)) { throw 'Final installer EXE was not created.' }

Write-Host 'BUILD SUCCESSFUL: release\SuvidhaPos-Installer-Setup.exe'
