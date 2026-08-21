$ErrorActionPreference = "Stop"
$main = Join-Path $PSScriptRoot "Installer\MainForm.cs"

if (!(Test-Path $main)) { throw "Installer\MainForm.cs not found." }

$text = [IO.File]::ReadAllText($main)

# Prevent WinForms DPI auto-scaling from multiplying the fixed card/header geometry.
$text = $text.Replace('AutoScaleMode = AutoScaleMode.Dpi;', 'AutoScaleMode = AutoScaleMode.Font;')

# Keep the navigation button above the scrollable page and make the click path explicit.
$text = $text.Replace(
    'nextButton.Click += NextClicked;\r\n\r\n        AcceptButton = nextButton;',
    'nextButton.Click += NextClicked;\r\n\r\n        nextButton.Enabled = true;\r\n        nextButton.BringToFront();\r\n        footer.BringToFront();\r\n        AcceptButton = nextButton;')
$text = $text.Replace(
    'nextButton.Click += NextClicked;\n\n        AcceptButton = nextButton;',
    'nextButton.Click += NextClicked;\n\n        nextButton.Enabled = true;\n        nextButton.BringToFront();\n        footer.BringToFront();\n        AcceptButton = nextButton;')

# Reduce the welcome hero typography so it remains readable at 125%-150% Windows scaling.
$text = $text.Replace('Font = new Font("Segoe UI Semibold", 25F)', 'Font = new Font("Segoe UI Semibold", 22F)')
$text = $text.Replace('Font = new Font("Segoe UI", 11F)', 'Font = new Font("Segoe UI", 10F)')
$text = $text.Replace('heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));', 'heroGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));')

[IO.File]::WriteAllText($main, $text, [Text.UTF8Encoding]::new($false))

Write-Host "Applied DPI/navigation UI fix to Installer/MainForm.cs"
