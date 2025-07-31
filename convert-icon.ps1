Write-Host "Converting app.png to icon.ico..." -ForegroundColor Yellow

if (!(Test-Path "app.png")) {
    Write-Host "Error: app.png not found!" -ForegroundColor Red
    exit 1
}

Add-Type -AssemblyName System.Drawing

try {
    $png = [System.Drawing.Image]::FromFile((Get-Item "app.png").FullName)
    
    # Create memory stream for ICO
    $stream = New-Object System.IO.MemoryStream
    
    # ICO Header
    $stream.WriteByte(0) # Reserved
    $stream.WriteByte(0) # Reserved
    $stream.WriteByte(1) # Type (1 = ICO)
    $stream.WriteByte(0) # Type
    $stream.WriteByte(1) # Number of images
    $stream.WriteByte(0) # Number of images
    
    # Image directory entry
    $width = if ($png.Width -ge 256) { 0 } else { $png.Width }
    $height = if ($png.Height -ge 256) { 0 } else { $png.Height }
    $stream.WriteByte($width) # Width (0 = 256)
    $stream.WriteByte($height) # Height (0 = 256)
    $stream.WriteByte(0) # Color palette
    $stream.WriteByte(0) # Reserved
    $stream.WriteByte(1) # Color planes
    $stream.WriteByte(0) # Color planes
    $stream.WriteByte(32) # Bits per pixel
    $stream.WriteByte(0) # Bits per pixel
    
    # Save PNG to memory
    $pngStream = New-Object System.IO.MemoryStream
    $png.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $pngStream.ToArray()
    $pngStream.Close()
    
    # Size of PNG data
    $size = $pngBytes.Length
    $stream.WriteByte($size -band 0xFF)
    $stream.WriteByte(($size -shr 8) -band 0xFF)
    $stream.WriteByte(($size -shr 16) -band 0xFF)
    $stream.WriteByte(($size -shr 24) -band 0xFF)
    
    # Offset to PNG data (after header)
    $stream.WriteByte(22) # 6 (header) + 16 (directory entry)
    $stream.WriteByte(0)
    $stream.WriteByte(0)
    $stream.WriteByte(0)
    
    # Write PNG data
    $stream.Write($pngBytes, 0, $pngBytes.Length)
    
    # Save ICO file
    [System.IO.File]::WriteAllBytes("icon.ico", $stream.ToArray())
    $stream.Close()
    $png.Dispose()
    
    Write-Host "Successfully created icon.ico" -ForegroundColor Green
}
catch {
    Write-Host "Error converting icon: $_" -ForegroundColor Red
    exit 1
}