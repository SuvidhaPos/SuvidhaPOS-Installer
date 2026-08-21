$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$path = Join-Path $PSScriptRoot '..\Installer\MainForm.cs'
$text = Get-Content -Raw -LiteralPath $path

function Replace-Regex([string]$pattern, [string]$replacement, [string]$label) {
    $updated = [regex]::Replace($script:text, $pattern, $replacement)
    if ($updated -ne $script:text) {
        $script:text = $updated
        Write-Host "Applied: $label"
    } else {
        Write-Host "No change needed or pattern already satisfied: $label"
    }
}

# ---- Stable window/DPI baseline ----
Replace-Regex 'MinimumSize = new Size\(\d+,\s*\d+\);' 'MinimumSize = new Size(1024, 768);' 'minimum window size'
Replace-Regex 'Size = new Size\(\d+,\s*\d+\);\s*\n\s*AutoScaleMode = AutoScaleMode\.Dpi;' 'Size = new Size(1366, 768);\n        AutoScaleMode = AutoScaleMode.None;' 'default window size + DPI mode'
Replace-Regex 'root\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Absolute,\s*\d+\)\);' 'root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));' 'sidebar base width'
Replace-Regex 'root\.RowStyles\.Add\(new RowStyle\(SizeType\.Absolute,\s*\d+\)\);' 'root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));' 'header base height'

# ---- Sidebar item dimensions ----
Replace-Regex 'Width = 228,\s*\n\s*Height = 70,' 'Width = 248,\n                Height = 82,' 'step item base size'
Replace-Regex 'Width = 228,\s*\n\s*Height = 112,' 'Width = 248,\n            Height = 124,' 'help card base size'
Replace-Regex 'int sidebarWidth = Math\.Clamp\(\(int\)Math\.Round\(w \* 0\.20\),\s*220,\s*250\);' 'int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.20), 250, 290);' 'responsive sidebar width'
Replace-Regex 'int headerHeight = Math\.Clamp\(\(int\)Math\.Round\(h \* 0\.085\),\s*62,\s*72\);' 'int headerHeight = Math.Clamp((int)Math.Round(h * 0.10), 76, 92);' 'responsive header height'
Replace-Regex 'int innerWidth = Math\.Max\(190, sidebarWidth - sidebar\.Padding\.Horizontal\);' 'int innerWidth = Math.Max(210, sidebarWidth - sidebar.Padding.Horizontal);' 'sidebar inner width'
Replace-Regex 'item\.Height = h < 700 \? 64 : 70;' 'item.Height = h < 820 ? 78 : 82;' 'responsive step height'
Replace-Regex 'help\.Height = h < 700 \? 92 : 112;' 'help.Height = h < 820 ? 108 : 124;' 'responsive help height'

# ---- Footer sizing ----
Replace-Regex 'Height = 64,\s*\n\s*ColumnCount = 6,' 'Height = 72,\n            ColumnCount = 6,' 'footer height'

# ---- Required branding ----
Replace-Regex '\+91 70042 52545' '+91 827171 8844' 'support phone'
Replace-Regex '"Setup & Backup"' '"Setup Database"' 'step 6 title'
Replace-Regex '"Database setup & backup"' '"Database setup"' 'step 6 subtitle'

# ---- Download/Install cards: give titles, status and progress their own breathing room ----
Replace-Regex 'card\.Height = 78;\s*\n\s*card\.Margin = new Padding\(0, 0, 0, 8\);' 'card.Height = 96;\n            card.Margin = new Padding(0, 0, 0, 8);' 'download card outer height'
Replace-Regex 'Height = 78,\s*\n\s*Padding = new Padding\(14, 10, 14, 8\),' 'Height = 96,\n            Padding = new Padding(14, 10, 14, 10),' 'download card inner height'
Replace-Regex 'statusPanel\.RowStyles\.Add\(new RowStyle\(SizeType\.Absolute, 22\)\);' 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));' 'download title row'
Replace-Regex 'statusPanel\.RowStyles\.Add\(new RowStyle\(SizeType\.Absolute, 20\)\);' 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));' 'download status row'

Replace-Regex 'card\.Height = 72;\s*\n\s*card\.Margin = new Padding\(0, 0, 0, 8\);' 'card.Height = 92;\n            card.Margin = new Padding(0, 0, 0, 8);' 'install card outer height'
Replace-Regex 'Height = 72,\s*\n\s*Padding = new Padding\(14, 8, 14, 8\),' 'Height = 92,\n            Padding = new Padding(14, 9, 14, 10),' 'install card inner height'
Replace-Regex 'layout\.RowStyles\.Add\(new RowStyle\(SizeType\.Absolute, 21\)\);' 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));' 'install title row'
Replace-Regex 'layout\.RowStyles\.Add\(new RowStyle\(SizeType\.Absolute, 19\)\);' 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));' 'install status row'

# ---- Make StepItem text use all available width ----
Replace-Regex 'Width = 135,\s*\n\s*Height = 22,\s*\n\s*AutoEllipsis = true' 'Width = 170,\n                Height = 24,\n                AutoEllipsis = true' 'step title width'
Replace-Regex 'Width = 135,\s*\n\s*Height = 18,\s*\n\s*AutoEllipsis = true' 'Width = 170,\n                Height = 20,\n                AutoEllipsis = true' 'step subtitle width'

# If the source already contains the responsive flow fix from a previous build pass, do not fail.
# The build must remain deterministic and idempotent.

Set-Content -LiteralPath $path -Value $script:text -Encoding UTF8
Write-Host "Installer UI hardening complete: $path"
