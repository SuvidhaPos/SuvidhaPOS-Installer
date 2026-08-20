# SuvidhaPOS Installer V2

Premium .NET 8 x64 Windows installer for Suvidha POS.

## Exact 7-step flow

1. Welcome
2. Terms & Conditions
3. Components
4. Download
5. Install
6. Setup & Backup
7. Finish

The UI is the premium dark-blue Suvidha POS design: branded header, 7-step sidebar, feature cards, component cards, progress bars, gradient action buttons and installation summary.

## Installation behavior

- SQL Server 2019 and SSMS are launched as their normal Microsoft setup programs, so the user can select **Default Instance**, authentication mode and other setup options manually.
- Crystal Reports Runtime is installed through MSI.
- Suvidha POS MSI and VC++ Redistributable are detected from `D:\Suvidha Pos\Software`.
- Existing downloaded installers are reused.
- Progress is saved to `%ProgramData%\SuvidhaPOS\Installer\resume.json`.
- A Windows Scheduled Task named `SuvidhaPOS Installer Resume` relaunches the installer at logon while setup is incomplete.
- If Windows restarts or the PC is powered off during setup, the wizard returns to the saved step after login.
- Database restore uses Windows authentication and `Microsoft.Data.SqlClient`.
- The setup page detects `SuvidhaPos.exe.config` or `RetailPos.exe.config` and updates the `sqlKey` value to `Data Source=<server>;Initial Catalog=<database>;Integrated Security=True`.
- Finish provides **Launch Suvidha POS**.

## Required local files

Put the Suvidha POS MSI, optional VC++ redistributable and `.bak` backup under `D:\Suvidha Pos\Software` when those files are needed.

## GitHub build

The repository contains one workflow only: `.github/workflows/build.yml`. It verifies the required source/assets, restores and builds .NET 8, publishes self-contained `win-x64`, installs Inno Setup 6, builds `release/SuvidhaPOS-Installer-Setup.exe`, and uploads the final installer artifact.

Do not commit customer database backups or private installer binaries.

## Side-by-side error 14001 fix
The GitHub workflow now publishes the installer as a self-contained `win-x64` folder rather than a single-file executable. Inno Setup packages the full publish output. This avoids the native single-file startup path that can produce `CreateProcess failed; code 14001` on some Windows machines.


## If GitHub Actions does not start

1. Open the repository's **Actions** tab.
2. Confirm Actions are enabled for the repository.
3. Open **Build SuvidhaPos-Installer** and use **Run workflow** on `main` for a manual build.
4. If a run exists, open the failed job and use the first red step/log as the build error.
5. The workflow itself verifies all required source and asset files before publishing.

There is only one workflow in this package: `.github/workflows/build.yml`.
