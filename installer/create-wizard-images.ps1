# Create modern wizard images for Inno Setup installer with OMEN branding
# Large image: 164x314 (left sidebar)
# Small image: 55x58 (top-right corner)

Add-Type -AssemblyName System.Drawing

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$logoPath = Join-Path (Split-Path -Parent $scriptPath) "src\OmenCoreApp\Assets\logo.png"

# OMEN brand colors
$omenRed = [System.Drawing.Color]::FromArgb(230, 0, 46)
$darkGray = [System.Drawing.Color]::FromArgb(15, 17, 28)
$mediumGray = [System.Drawing.Color]::FromArgb(30, 35, 50)
$accentCyan = [System.Drawing.Color]::FromArgb(0, 200, 200)
$white = [System.Drawing.Color]::White

# ═══ CREATE LARGE WIZARD IMAGE (164x314) ═══
Write-Host "Creating large wizard image (164x314)..." -ForegroundColor Cyan

$largeBmp = New-Object System.Drawing.Bitmap 164, 314
$largeGraphics = [System.Drawing.Graphics]::FromImage($largeBmp)
$largeGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$largeGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Gradient background (dark blue-gray to almost black)
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    [System.Drawing.Point]::new(0, 0),
    [System.Drawing.Point]::new(0, 314),
    $darkGray,
    [System.Drawing.Color]::FromArgb(8, 10, 20)
)
$largeGraphics.FillRectangle($gradientBrush, 0, 0, 164, 314)

# Add subtle grid pattern for tech aesthetic
$gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(30, 255, 255, 255), 1)
for ($y = 0; $y -lt 314; $y += 20) {
    $largeGraphics.DrawLine($gridPen, 0, $y, 164, $y)
}
for ($x = 0; $x -lt 164; $x += 20) {
    $largeGraphics.DrawLine($gridPen, $x, 0, $x, 314)
}

# Load and draw logo at top
if (Test-Path $logoPath) {
    $logo = [System.Drawing.Image]::FromFile($logoPath)
    $logoSize = 80
    $logoX = (164 - $logoSize) / 2
    $logoY = 30
    
    # Draw glow effect behind logo
    $glowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glowPath.AddEllipse($logoX - 20, $logoY - 20, $logoSize + 40, $logoSize + 40)
    $glowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($glowPath)
    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(80, $omenRed)
    $glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, $omenRed))
    $largeGraphics.FillPath($glowBrush, $glowPath)
    $glowBrush.Dispose()
    $glowPath.Dispose()
    
    $largeGraphics.DrawImage($logo, $logoX, $logoY, $logoSize, $logoSize)
    $logo.Dispose()
}

# Draw "OMEN" text
$omenFont = New-Object System.Drawing.Font("Segoe UI", 20, [System.Drawing.FontStyle]::Bold)
$omenBrush = New-Object System.Drawing.SolidBrush($omenRed)
$omenText = "OMEN"
$omenSize = $largeGraphics.MeasureString($omenText, $omenFont)
$largeGraphics.DrawString($omenText, $omenFont, $omenBrush, (164 - $omenSize.Width) / 2, 130)

# Draw "Core" text
$coreFont = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Regular)
$coreBrush = New-Object System.Drawing.SolidBrush($white)
$coreText = "Core"
$coreSize = $largeGraphics.MeasureString($coreText, $coreFont)
$largeGraphics.DrawString($coreText, $coreFont, $coreBrush, (164 - $coreSize.Width) / 2, 158)

# Draw subtle tagline instead of version (more universal - version shows in installer title bar)
$tagFont = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Italic)
$tagBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(150, 255, 255, 255))
$tagText = "Control Suite"
$tagSize = $largeGraphics.MeasureString($tagText, $tagFont)
$largeGraphics.DrawString($tagText, $tagFont, $tagBrush, (164 - $tagSize.Width) / 2, 185)

# Draw decorative line
$linePen = New-Object System.Drawing.Pen($omenRed, 2)
$largeGraphics.DrawLine($linePen, 30, 215, 134, 215)

# Feature highlights (replacing version-specific text)
$featureFont = New-Object System.Drawing.Font("Segoe UI", 7, [System.Drawing.FontStyle]::Regular)
$featureBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(180, 255, 255, 255))
$features = @("✓ Fan Control", "✓ RGB Lighting", "✓ Performance Modes", "✓ Game Profiles")
$featureY = 230
foreach ($feature in $features) {
    $featureSize = $largeGraphics.MeasureString($feature, $featureFont)
    $largeGraphics.DrawString($feature, $featureFont, $featureBrush, (164 - $featureSize.Width) / 2, $featureY)
    $featureY += 14
}

# Add subtle corner accents
$accentPen = New-Object System.Drawing.Pen($accentCyan, 2)
$largeGraphics.DrawLine($accentPen, 0, 0, 15, 0)
$largeGraphics.DrawLine($accentPen, 0, 0, 0, 15)
$largeGraphics.DrawLine($accentPen, 149, 0, 164, 0)
$largeGraphics.DrawLine($accentPen, 164, 0, 164, 15)

$largeGraphics.Dispose()
$largeBmp.Save("$scriptPath\wizard-large.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$largeBmp.Dispose()

Write-Host "✓ Large wizard image saved" -ForegroundColor Green

# ═══ CREATE SMALL WIZARD IMAGE (55x58) ═══
Write-Host "Creating small wizard image..." -ForegroundColor Cyan

$smallBmp = New-Object System.Drawing.Bitmap 55, 58
$smallGraphics = [System.Drawing.Graphics]::FromImage($smallBmp)
$smallGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$smallGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Gradient background matching OMEN dark theme
$smallGradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    [System.Drawing.Point]::new(0, 0),
    [System.Drawing.Point]::new(55, 58),
    $mediumGray,
    $darkGray
)
$smallGraphics.FillRectangle($smallGradient, 0, 0, 55, 58)

# Subtle border accent
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, $omenRed), 1)
$smallGraphics.DrawRectangle($borderPen, 0, 0, 54, 57)

# Load and draw logo centered
if (Test-Path $logoPath) {
    $logo = [System.Drawing.Image]::FromFile($logoPath)
    $logoSize = 42
    $logoX = (55 - $logoSize) / 2
    $logoY = (58 - $logoSize) / 2
    
    # Glow effect
    $glowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glowPath.AddEllipse($logoX - 5, $logoY - 5, $logoSize + 10, $logoSize + 10)
    $glowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($glowPath)
    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(100, $omenRed)
    $glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, $omenRed))
    $smallGraphics.FillPath($glowBrush, $glowPath)
    $glowBrush.Dispose()
    $glowPath.Dispose()
    
    $smallGraphics.DrawImage($logo, $logoX, $logoY, $logoSize, $logoSize)
    $logo.Dispose()
}

# Add corner accent
$smallGraphics.DrawLine($accentPen, 0, 0, 10, 0)
$smallGraphics.DrawLine($accentPen, 0, 0, 0, 10)

$smallGraphics.Dispose()
$smallBmp.Save("$scriptPath\wizard-small.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$smallBmp.Dispose()

Write-Host "✓ Small wizard image saved" -ForegroundColor Green
Write-Host "`n✨ Wizard images created successfully!" -ForegroundColor Green

# Cleanup
$gradientBrush.Dispose()
$gridPen.Dispose()
$omenFont.Dispose()
$omenBrush.Dispose()
$coreFont.Dispose()
$coreBrush.Dispose()
$tagFont.Dispose()
$tagBrush.Dispose()
$linePen.Dispose()
$featureFont.Dispose()
$featureBrush.Dispose()
$accentPen.Dispose()
$borderPen.Dispose()
if ($smallGradient) { $smallGradient.Dispose() }
