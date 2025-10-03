# Script to create PNG icon for Linux from the existing ico design
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

# Save as PNG
$pngPath = Join-Path $PSScriptRoot "app.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "PNG icon created successfully at: $pngPath" -ForegroundColor Green
