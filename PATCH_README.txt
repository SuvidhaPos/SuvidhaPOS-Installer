Suvidha POS Installer UI + Download Folder Fix
================================================

This package is a drop-in patch for the existing repository.

Changes:
1. Removes the DPI double-scaling effect that was causing the welcome title and cards to overlap.
2. Rebuilds the responsive UI measurements after resize/page changes.
3. Makes the welcome hero, feature cards, sidebar, component cards and buttons responsive.
4. Prevents horizontal scrollbars from clipping the first/last characters.
5. At narrow widths the welcome feature cards and simple 2-card layouts reflow.
6. Keeps the existing Next/Back/install/download logic intact.
7. Changes the download target from CommonAppData\SuvidhaPOS\Installer\Downloads to:
   D:\Suvidha Pos\Software
8. Creates D:\Suvidha Pos\Software automatically before downloading.

Apply:
- Extract this ZIP over your repository.
- Run APPLY_UI_FIX.ps1 from the repository root in PowerShell.
- Then run the existing build.ps1 or GitHub Actions build.
