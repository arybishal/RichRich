$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot"
$env:ANDROID_HOME = "C:\Android\Sdk"
$env:ANDROID_SDK_ROOT = "C:\Android\Sdk"
$env:ANDROID_NDK_ROOT = "C:\Android\Sdk\ndk\23.1.7779620"
$env:PATH = "C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot\bin;C:\Android\Sdk\platform-tools;" + $env:PATH

$unityExe = "D:\unity6\Editor\Unity.exe"
$projectDir = "D:\Webapp\RichyRich\RichRich_Unity"
$outputApk = "D:\Webapp\RichyRich\Builds\Android\RichRich-v0.3.0-android-arm64.apk"
$buildLog = "D:\Webapp\RichyRich\Builds\Android\build_v0.3.0.log"

if (Test-Path $buildLog) {
    Remove-Item -Force $buildLog
}

Write-Host "Running Unity Android ARM64 Release build for v0.3.0..."
$p = Start-Process -FilePath $unityExe -ArgumentList "-batchmode", "-quit", "-projectPath", $projectDir, "-executeMethod", "RichRich.Editor.RichRichBuilder.BuildAndroidRelease", "-buildOutput", $outputApk, "-logFile", $buildLog -PassThru -Wait
Write-Host "Build finished with exit code $($p.ExitCode)"
if ($p.ExitCode -ne 0) {
    Get-Content $buildLog -Tail 60
}
exit $p.ExitCode
