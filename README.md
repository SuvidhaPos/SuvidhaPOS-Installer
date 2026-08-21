# Suvidha POS Installer V2.3

Clean .NET 8 WinForms x64 installer source.

## UI architecture

- Fixed header.
- Fixed left navigation.
- Only the page body scrolls.
- Footer is outside the scrollable page body.
- `Next`, `Back` and `Cancel` are created once per page and wired directly.
- No generic `Control.HorizontalScroll`.
- Per-monitor DPI scaling is enabled.
- Layout uses WinForms `TableLayoutPanel`, `FlowLayoutPanel` and dock/anchor rules instead of page-wide absolute positioning.

## Seven steps

1. Welcome
2. Terms & Conditions
3. Components
4. Download
5. Install
6. Setup & Backup
7. Finish

## File locations

The installer always checks/downloads/reuses files directly in:

`D:\Suvidha Pos\Software`

No component subfolders are created there.

After installation, the installer application is installed in:

`C:\Program Files\Suvidha Soft Installer`

## Build

Requires Windows, .NET 8 SDK and Inno Setup 6.

Run:

```powershell
.\build.ps1
```

The authoritative GitHub Actions workflow performs the same folder-based, self-contained `win-x64` publish.

## Release verification

The workflow rejects the old UI patch files and checks the source for obsolete UI/download constructs before building.

## UI build safety

The reference UI skin does not access protected WinForms members such as `Control.DoubleBuffered`.


## Reference-locked UI
The installer uses a fixed 1448x1086 borderless frame and renders the seven supplied reference screens from `Assets/ReferenceUI` without responsive reflow. Functional click hotspots forward to the existing wizard logic.
