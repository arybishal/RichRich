$adb = "C:\Android\Sdk\platform-tools\adb.exe"
$device = "3B15B800NRT00000"
$outDir = "D:\Webapp\RichyRich\Builds\audit"

if (!(Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force
}

$screens = @(
    @{ Name = "01_education"; X = 960; Y = 1030 },
    @{ Name = "02_housing"; X = 300; Y = 1120 },
    @{ Name = "03_lifestyle"; X = 960; Y = 1120 },
    @{ Name = "04_marketplace"; X = 300; Y = 1210 },
    @{ Name = "05_dashboard"; X = 960; Y = 1210 },
    @{ Name = "06_autorepair"; X = 300; Y = 1300 },
    @{ Name = "07_convenience"; X = 960; Y = 1300 },
    @{ Name = "08_agency"; X = 300; Y = 1390 },
    @{ Name = "09_staffing"; X = 960; Y = 1390 },
    @{ Name = "10_finance"; X = 300; Y = 1480 },
    @{ Name = "11_realestate"; X = 960; Y = 1480 },
    @{ Name = "12_settings"; X = 960; Y = 1660 }
)

foreach ($s in $screens) {
    Write-Host "Opening $($s.Name)..."
    & $adb -s $device shell input tap $s.X $s.Y
    Start-Sleep -Milliseconds 1200
    
    $remote = "/sdcard/$($s.Name).png"
    $local = "$outDir/$($s.Name).png"
    & $adb -s $device shell screencap -p $remote
    & $adb -s $device pull $remote $local
    
    # Tap the visible Back button on the top right of the modal header (x=1000, y=200)
    & $adb -s $device shell input tap 1000 200
    Start-Sleep -Milliseconds 600
}

Write-Host "Capturing 13_exit_confirmation..."
# Press back on HUD to trigger exit dialog
& $adb -s $device shell input keyevent 4
Start-Sleep -Milliseconds 800
& $adb -s $device shell screencap -p /sdcard/13_exit_dialog.png
& $adb -s $device pull /sdcard/13_exit_dialog.png "$outDir/13_exit_dialog.png"

# Cancel exit dialog (Cancel is around 350, 1500)
& $adb -s $device shell input tap 350 1500

Write-Host "Audit screen capture finished successfully!"
