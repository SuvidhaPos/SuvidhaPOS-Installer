SuvidhaPOS Installer V2 - UI BUILD FIX

This patch fixes the compile errors shown in GitHub Actions:

CS1061:
  Control does not contain HorizontalScroll
CS0413:
  generic T cannot be used with 'as' without a class constraint

Root cause:
  UiPolish.cs was calling HorizontalScroll on a Control reference and GetField<T>
  did not constrain T as a reference type.

The corrected UiPolish.cs:
- safely handles ScrollableControl before touching HorizontalScroll
- adds `where T : class` to GetField<T>
- keeps the responsive layout
- removes an incorrect duplicate RowStyle insertion in the narrow table layout

MainForm.cs patch:
- AutoScaleMode Dpi -> None so DPI scaling does not fight the responsive pixel layout
- DownloadDir -> D:\Suvidha Pos\Software

Apply:
1. Extract/overwrite Installer\UiPolish.cs from this ZIP.
2. Run APPLY_UI_FIX.ps1 from repository root.
3. Run .\build.ps1.

The WFA010 high-DPI message is a warning, not the reason the build failed.
