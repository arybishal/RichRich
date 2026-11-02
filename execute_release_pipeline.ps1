# RichRich v0.1.1 Release Pipeline Execution Script
$ErrorActionPreference = "Continue"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " RichRich v0.1.1: Android Build & Release Pipeline " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$unityExe = "D:\unity6\Editor\Unity.exe"
$projectDir = "D:\Webapp\RichyRich\RichRich_Unity"
$outputDir = "D:\Webapp\RichyRich\Builds\Android"
$outputApk = "$outputDir\RichRich-v0.1.1-android-arm64.apk"
$buildLog = "$outputDir\build_v0.1.1.log"
$licenseLog = "$outputDir\license_test.log"
$repo = "arybishal/RichRich"
$releaseTag = "v0.1.1"
$deviceSerial = "3B15B800NRT00000"

# Configure environment variables for Android toolchain
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot"
$env:ANDROID_HOME = "C:\Android\Sdk"
$env:ANDROID_SDK_ROOT = "C:\Android\Sdk"
$env:ANDROID_NDK_ROOT = "C:\Android\Sdk\ndk\23.1.7779620"
$env:PATH = "C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot\bin;C:\Android\Sdk\platform-tools;" + $env:PATH

# Ensure output directory exists
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# Step 1: Check Unity License
Write-Host "`n[1/6] Checking Unity License..." -ForegroundColor Yellow
if (Test-Path $licenseLog) { Remove-Item $licenseLog -Force }

# First try activating personal license via CLI if signed in
$authStatus = & unity auth status 2>&1
if ($authStatus -notmatch "not signed in") {
    Write-Host "Unity CLI is authenticated. Ensuring Personal license is active..." -ForegroundColor Green
    & unity license activate --personal --accept-eula 2>&1 | Out-Null
}

$licProc = Start-Process -FilePath $unityExe -ArgumentList "-batchmode", "-quit", "-projectPath", $projectDir, "-logFile", $licenseLog -PassThru -Wait
if ($licProc.ExitCode -ne 0) {
    Write-Host "[-] Unity License check failed with exit code $($licProc.ExitCode)." -ForegroundColor Red
    if (Test-Path $licenseLog) {
        Get-Content $licenseLog -Tail 15
    }
    Write-Host "`nPlease ensure Unity Hub is signed in and has an active Personal License under Preferences > Licenses." -ForegroundColor Yellow
    exit 1
}
Write-Host "[+] Unity License validated successfully (Exit code: 0)." -ForegroundColor Green

# Step 2: Run Unity Headless Build
Write-Host "`n[2/6] Building Android ARM64 Release APK..." -ForegroundColor Yellow
if (Test-Path $outputApk) { Remove-Item $outputApk -Force }
if (Test-Path $buildLog) { Remove-Item $buildLog -Force }

$buildArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $projectDir,
    "-executeMethod", "RichRich.Editor.RichRichBuilder.BuildAndroidRelease",
    "-buildOutput", $outputApk,
    "-logFile", $buildLog
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$buildProc = Start-Process -FilePath $unityExe -ArgumentList $buildArgs -PassThru -Wait
$stopwatch.Stop()

Write-Host "Unity build finished in $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) seconds with exit code: $($buildProc.ExitCode)"

if (-not (Test-Path $outputApk)) {
    Write-Host "[-] Build failed: APK was not generated at $outputApk" -ForegroundColor Red
    if (Test-Path $buildLog) {
        Write-Host "--- Tail of build log ---"
        Get-Content $buildLog -Tail 40
    }
    exit 1
}

# Step 3: Verify APK
Write-Host "`n[3/6] Verifying APK artifact..." -ForegroundColor Yellow
$apkItem = Get-Item $outputApk
$apkSizeMB = ($apkItem.Length / 1MB).ToString("F2")
$apkHash = (Get-FileHash -Path $outputApk -Algorithm SHA256).Hash

Write-Host "[+] APK generated successfully!" -ForegroundColor Green
Write-Host "    Path:   $outputApk"
Write-Host "    Size:   $apkSizeMB MB ($($apkItem.Length) bytes)"
Write-Host "    SHA256: $apkHash"

# Step 4: Sideload & Launch on Device (if available)
Write-Host "`n[4/6] Checking connected Android devices..." -ForegroundColor Yellow
$adbLines = (& adb devices) | Out-String
Write-Host "$adbLines"

$matchedSerial = $null
if ($adbLines -match "($deviceSerial)\s+device") {
    $matchedSerial = $matches[1]
} elseif ($adbLines -match "([a-zA-Z0-9_\:\.]+)\s+device") {
    $matchedSerial = $matches[1]
}

if ($matchedSerial) {
    Write-Host "[+] Target device '$matchedSerial' is authorized and connected. Installing APK..." -ForegroundColor Green
    & adb -s $matchedSerial install -r $outputApk
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[+] Installation succeeded! Launching com.bishal.richrich..." -ForegroundColor Green
        & adb -s $matchedSerial shell monkey -p com.bishal.richrich -c android.intent.category.LAUNCHER 1
        Start-Sleep -Seconds 3
        Write-Host "`n--- Device Logcat (Unity / Runtime) ---"
        & adb -s $matchedSerial logcat -d -s Unity:V AndroidRuntime:E
    } else {
        Write-Host "[-] Device installation failed." -ForegroundColor Red
    }
} else {
    Write-Host "[*] No authorized Android device detected. Skipping physical deployment." -ForegroundColor Yellow
}

# Step 5: Upload to GitHub Release
Write-Host "`n[5/6] Creating / Uploading APK to GitHub Release $releaseTag..." -ForegroundColor Yellow
$releaseExists = & gh release view $releaseTag --repo $repo 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Release $releaseTag already exists. Uploading artifact..." -ForegroundColor Yellow
    & gh release upload $releaseTag $outputApk --repo $repo --clobber
} else {
    Write-Host "Creating new GitHub Release $releaseTag..." -ForegroundColor Green
    & gh release create $releaseTag $outputApk --repo $repo --title "RichRich v0.1.1: Android Black Screen Fix & Playable Release" --notes "Release v0.1.1 resolves the Android black screen issue by fully authoring and binding the MainGameplay scene hierarchy (Camera, Canvas, HUD, and 12 subsystem modals) and adding self-healing fallback UI and startup diagnostic recovery to GameBootstrapper."
}
if ($LASTEXITCODE -eq 0) {
    Write-Host "[+] GitHub Release successful!" -ForegroundColor Green
} else {
    Write-Host "[-] GitHub Release failed." -ForegroundColor Red
    exit 1
}

# Step 6: Verify GitHub Release Asset
Write-Host "`n[6/6] Verifying GitHub Release asset..." -ForegroundColor Yellow
& gh release view $releaseTag --repo $repo --json tagName,name,assets,url
Write-Host "`n[SUCCESS] Pipeline execution finished!" -ForegroundColor Green
