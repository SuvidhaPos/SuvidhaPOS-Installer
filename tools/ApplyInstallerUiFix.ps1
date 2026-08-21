$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$path = Join-Path $PSScriptRoot '..\Installer\MainForm.cs'
$text = Get-Content -Raw -LiteralPath $path

function Replace-Once([string]$old, [string]$new, [string]$label) {
    if ($script:text.Contains($new)) { Write-Host "Already applied: $label"; return }
    if (-not $script:text.Contains($old)) { throw "Expected source fragment not found for: $label" }
    $script:text = $script:text.Replace($old, $new)
    Write-Host "Applied: $label"
}

Replace-Once 'MinimumSize = new Size(960, 680);' 'MinimumSize = new Size(1024, 768);' 'minimum window size'
Replace-Once 'Size = new Size(1280, 800);' 'Size = new Size(1366, 768);' 'default window size'
Replace-Once 'AutoScaleMode = AutoScaleMode.Dpi;' 'AutoScaleMode = AutoScaleMode.None;' 'disable WinForms auto-DPI scaling'
Replace-Once 'root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));' 'root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));' 'sidebar width'
Replace-Once 'root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));' 'root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));' 'header height'
Replace-Once 'Width = 228,\n                Height = 70,' 'Width = 248,\n                Height = 82,' 'step card size'
Replace-Once 'Width = 228,\n            Height = 112,' 'Width = 248,\n            Height = 124,' 'help card size'
Replace-Once 'int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.20), 220, 250);' 'int sidebarWidth = Math.Clamp((int)Math.Round(w * 0.20), 250, 290);' 'responsive sidebar sizing'
Replace-Once 'int headerHeight = Math.Clamp((int)Math.Round(h * 0.085), 62, 72);' 'int headerHeight = Math.Clamp((int)Math.Round(h * 0.10), 76, 92);' 'responsive header sizing'
Replace-Once 'int innerWidth = Math.Max(190, sidebarWidth - sidebar.Padding.Horizontal);' 'int innerWidth = Math.Max(210, sidebarWidth - sidebar.Padding.Horizontal);' 'responsive sidebar inner width'
Replace-Once 'item.Height = h < 700 ? 64 : 70;' 'item.Height = h < 820 ? 78 : 82;' 'responsive step height'
Replace-Once 'help.Height = h < 700 ? 92 : 112;' 'help.Height = h < 820 ? 108 : 124;' 'responsive help height'
Replace-Once 'Height = 64,\n            ColumnCount = 6,' 'Height = 72,\n            ColumnCount = 6,' 'footer height'

$anchor = 'if (step == 6) { Close(); return; }\n        if (step == 1)'
if ($script:text.Contains('if (step == 0)\n        {')) { Write-Host 'Already applied: Step 1 -> Step 2 navigation' }
elseif ($script:text.Contains($anchor)) {
    $replacement = 'if (step == 6) { Close(); return; }\n        if (step == 0)\n        {\n            ShowStep(1); return;\n        }\n        if (step == 1)'
    $script:text = $script:text.Replace($anchor, $replacement)
    Write-Host 'Applied: Step 1 -> Step 2 navigation'
} else { throw 'Expected NextClicked navigation anchor not found.' }

Replace-Once 'Text = "+91 70042 52545",' 'Text = "+91 827171 8844",' 'support phone'

Replace-Once 'card.Height = 78;\n            card.Margin = new Padding(0, 0, 0, 8);' 'card.Height = 96;\n            card.Margin = new Padding(0, 0, 0, 8);' 'download card height'
Replace-Once 'Height = 78,\n            Padding = new Padding(14, 10, 14, 8),' 'Height = 96,\n            Padding = new Padding(14, 10, 14, 10),' 'download card internal height'
Replace-Once 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));' 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));' 'download title row'
Replace-Once 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));' 'statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));' 'download status row'
Replace-Once 'Font = new Font("Segoe UI Semibold", 9.5F),' 'Font = new Font("Segoe UI Semibold", 9F),' 'compact card title font'
Replace-Once 'Font = new Font("Segoe UI", 8F),' 'Font = new Font("Segoe UI", 8.5F),' 'compact card status font'
Replace-Once 'card.Height = 72;\n            card.Margin = new Padding(0, 0, 0, 8);' 'card.Height = 92;\n            card.Margin = new Padding(0, 0, 0, 8);' 'install card height'
Replace-Once 'Height = 72,\n            Padding = new Padding(14, 8, 14, 8),' 'Height = 92,\n            Padding = new Padding(14, 9, 14, 10),' 'install card internal height'
Replace-Once 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));' 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));' 'install title row'
Replace-Once 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));' 'layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));' 'install status row'

Set-Content -LiteralPath $path -Value $script:text -Encoding UTF8
Write-Host "Installer UI/flow hardening complete: $path"
