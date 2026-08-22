# Suvidha POS Installer

Clean rebuild of the installer with a responsive .NET 8 WinForms UI.

## UI

- Seven-step guided installation flow
- Welcome
- Terms
- Components
- Download
- Install
- Database
- Finish
- Per-monitor DPI awareness
- Responsive sidebar that collapses to numbered steps on narrower windows
- No horizontal content scrolling
- Resizable content area with vertical scrolling only where required

## Build

```powershell
dotnet restore Installer/SuvidhaPos-Installer.csproj
dotnet build Installer/SuvidhaPos-Installer.csproj -c Release
dotnet publish Installer/SuvidhaPos-Installer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The GitHub Actions workflow packages the published application with Inno Setup and uploads the final Windows x64 installer as an artifact.
