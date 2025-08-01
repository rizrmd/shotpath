Write-Host "Converting app.png to high-resolution icon.ico..." -ForegroundColor Yellow

if (!(Test-Path "app.png")) {
    Write-Host "Error: app.png not found!" -ForegroundColor Red
    exit 1
}

Add-Type -AssemblyName System.Drawing

try {
    $sourcePng = [System.Drawing.Image]::FromFile((Get-Item "app.png").FullName)
    
    # Define the sizes we want in the ICO file
    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    
    # Create memory stream for ICO
    $icoStream = New-Object System.IO.MemoryStream
    
    # ICO Header
    $icoStream.WriteByte(0) # Reserved
    $icoStream.WriteByte(0) # Reserved
    $icoStream.WriteByte(1) # Type (1 = ICO)
    $icoStream.WriteByte(0) # Type
    $icoStream.WriteByte($sizes.Count) # Number of images (low byte)
    $icoStream.WriteByte(0) # Number of images (high byte)
    
    # Keep track of image data
    $imageDataList = New-Object System.Collections.ArrayList
    
    # Create resized images and store their data
    foreach ($size in $sizes) {
        Write-Host "  Creating $size x $size image..." -ForegroundColor Gray
        
        # Create a new bitmap with the target size
        $bitmap = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        
        # Set high quality rendering
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        
        # Draw the source image scaled to the new size
        $graphics.DrawImage($sourcePng, 0, 0, $size, $size)
        $graphics.Dispose()
        
        # Save as PNG to memory
        $pngStream = New-Object System.IO.MemoryStream
        $bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $pngStream.ToArray()
        $pngStream.Close()
        $bitmap.Dispose()
        
        # Store the PNG data
        [void]$imageDataList.Add($pngBytes)
    }
    
    # Calculate offsets
    $headerSize = 6  # ICO header
    $directorySize = 16 * $sizes.Count  # Each directory entry is 16 bytes
    $currentOffset = $headerSize + $directorySize
    
    # Write directory entries
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $size = $sizes[$i]
        $pngData = $imageDataList[$i]
        
        # Width and height (0 = 256)
        $width = if ($size -eq 256) { 0 } else { $size }
        $height = if ($size -eq 256) { 0 } else { $size }
        
        $icoStream.WriteByte($width)  # Width
        $icoStream.WriteByte($height) # Height
        $icoStream.WriteByte(0)       # Color palette
        $icoStream.WriteByte(0)       # Reserved
        $icoStream.WriteByte(1)       # Color planes (low byte)
        $icoStream.WriteByte(0)       # Color planes (high byte)
        $icoStream.WriteByte(32)      # Bits per pixel (low byte)
        $icoStream.WriteByte(0)       # Bits per pixel (high byte)
        
        # Size of image data
        $dataSize = $pngData.Length
        $icoStream.WriteByte($dataSize -band 0xFF)
        $icoStream.WriteByte(($dataSize -shr 8) -band 0xFF)
        $icoStream.WriteByte(($dataSize -shr 16) -band 0xFF)
        $icoStream.WriteByte(($dataSize -shr 24) -band 0xFF)
        
        # Offset to image data
        $icoStream.WriteByte($currentOffset -band 0xFF)
        $icoStream.WriteByte(($currentOffset -shr 8) -band 0xFF)
        $icoStream.WriteByte(($currentOffset -shr 16) -band 0xFF)
        $icoStream.WriteByte(($currentOffset -shr 24) -band 0xFF)
        
        $currentOffset += $dataSize
    }
    
    # Write all image data
    foreach ($pngData in $imageDataList) {
        $icoStream.Write($pngData, 0, $pngData.Length)
    }
    
    # Save ICO file
    [System.IO.File]::WriteAllBytes("icon.ico", $icoStream.ToArray())
    $icoStream.Close()
    $sourcePng.Dispose()
    
    Write-Host "Successfully created high-resolution icon.ico with $($sizes.Count) sizes" -ForegroundColor Green
    Write-Host "Sizes included: $($sizes -join ', ')px" -ForegroundColor Gray
}
catch {
    Write-Host "Error converting icon: $_" -ForegroundColor Red
    exit 1
}