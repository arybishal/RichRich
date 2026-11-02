$adb = "C:\Android\Sdk\platform-tools\adb.exe"
$device = "3B15B800NRT00000"
$outDir = "D:\Webapp\RichyRich\Builds\audit"

if (!(Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force
}

$screens = @(
    @{ Name = "02_education"; X = 960; Y = 1030 },
    @{ Name = "03_housing"; X = 300; Y = 1120 },
    @{ Name = "04_lifestyle"; X = 960; Y = 1120 },
    @{ Name = "05_marketplace"; X = 300; Y = 1210 },
    @{ Name = "06_dashboard"; X = 960; Y = 1210 },
    @{ Name = "07_autorepair"; X = 300; Y = 1300 },
    @{ Name = "08_convenience"; X = 960; Y = 1300 },
    @{ Name = "09_agency"; X = 300; Y = 1390 },
    @{ Name = "10_staffing"; X = 960; Y = 1390 },
    @{ Name = "11_finance"; X = 300; Y = 1480 },
    @{ Name = "12_realestate"; X = 960; Y = 1480 },
    @{ Name = "13_settings"; X = 960; Y = 1660 }
)

# Return to dashboard first
& $adb -s $device shell input keyevent 4
Start-Sleep -Milliseconds 400
& $adb -s $device shell input keyevent 4
Start-Sleep -Milliseconds 400

foreach ($s in $screens) {
    Write-Host "Capturing $($s.Name)..."
    & $adb -s $device shell input tap $s.X $s.Y
    Start-Sleep -Milliseconds 800
    $remotePath = "/sdcard/$($s.Name).png"
    $localPath = "$outDir/$($s.Name).png"
    & $adb -s $device shell screencap -p $remotePath
    & $adb -s $device pull $remotePath $localPath
    Start-Sleep -Milliseconds 400
    # Dismiss back to dashboard
    & $adb -s $device shell input keyevent 4
    Start-Sleep -Milliseconds 500
}

# Capture Exit Confirmation
Write-Host "Capturing 14_exit_dialog..."
& $adb -s $device shell input keyevent 4
Start-Sleep -Milliseconds 600
& $adb -s $device shell screencap -p /sdcard/14_exit_dialog.png
& $adb -s $device pull /sdcard/14_exit_dialog.png "$outDir/14_exit_dialog.png"

# Dismiss exit dialog (Cancel is around 350, 1370)
& $adb -s $device shell input keyevent 4

Write-Host "Capture complete!"
