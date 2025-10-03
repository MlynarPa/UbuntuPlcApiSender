# Script to create a simple application icon
# This creates a basic .ico file for the application

Add-Type -AssemblyName System.Drawing

# Create a 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set high quality rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Fill background with blue gradient
$brush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 41, 128, 185))
$graphics.FillRectangle($brush1, 0, 0, 256, 256)

# Draw PLC-related graphics
$whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 8)
$graphics.DrawRectangle($whitePen, 40, 60, 176, 136)

# Draw connection symbol
$graphics.DrawLine($whitePen, 128, 60, 128, 30)
$graphics.DrawEllipse($whitePen, 118, 15, 20, 20)

# Draw text "PLC"
$font = New-Object System.Drawing.Font("Arial", 48, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.DrawString("PLC", $font, $textBrush, 75, 100)

# Save as PNG first
$pngPath = Join-Path $PSScriptRoot "app_temp.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Convert PNG to ICO
$icoPath = Join-Path $PSScriptRoot "app.ico"

# Create icon from bitmap
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
$icon.Save($stream)
$stream.Close()

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$icon.Dispose()

if (Test-Path $pngPath) {
    Remove-Item $pngPath
}

Write-Host "Icon created successfully at: $icoPath" -ForegroundColor Green
