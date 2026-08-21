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
## Fixed UI Frame

The installer intentionally uses a fixed 1366 x 768 client area. The window is not resizable or maximizable, and WinForms automatic layout/DPI scaling is disabled so the seven reference screens keep the same composition and readable text. Build-time PowerShell source-rewriter scripts are not used.

The installer flow remains:
1. Welcome
2. Terms & Conditions
3. Components
4. Download
5. Install
6. Database Setup
7. Finish
