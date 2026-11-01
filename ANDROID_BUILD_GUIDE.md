# RichRich: Android Build & Release Engineering Guide

## 1. Overview & Architecture Standards

This guide documents the standardized, repeatable Android release engineering pipeline for **RichRich** (`com.bishal.richrich`). All builds adhere strictly to the project's technical architecture:
- **Engine Baseline:** Unity 6 LTS (`6000.0.35f1`)
- **Scripting Backend:** IL2CPP (ahead-of-time C++ compilation)
- **Target Architecture:** `ARM64` (`arm64-v8a`)
- **Orientation:** Portrait
- **Minimum Android API:** 28 (Android 9.0 Pie)
- **Target Android API:** 35 (Android 15 Vanilla Ice Cream)
- **Managed Code Stripping:** Low (preserves dynamic reflection and JSON contracts)
- **Build Configuration:** Release (Development Build disabled, Script Debugging disabled)

---

## 2. Environment Prerequisites

### 2.1 Software & Toolchain Matrix
| Component | Minimum Required Version | Host Path / Command |
|---|---|---|
| **Unity Editor** | `6000.0.35f1` (Unity 6 LTS) | `C:\Program Files\Unity\Hub\Editor\6000.0.35f1\Editor\Unity.exe` |
| **Android Build Support** | Unity Android IL2CPP Target | Embedded module in Unity Editor |
| **Android SDK** | API 28 - 35 | `C:\Android\Sdk` (`ANDROID_HOME`, `ANDROID_SDK_ROOT`) |
| **Android NDK** | NDK r23+ (`28.2.13676358`) | `C:\Android\Sdk\ndk\28.2.13676358` |
| **Java JDK** | OpenJDK 17 or 21 | `C:\Program Files\Microsoft\jdk-21.0.13.21-hotspot` (`JAVA_HOME`) |
| **ADB CLI** | Android Platform Tools | `C:\Android\Sdk\platform-tools\adb.exe` or `C:\adb\adb.exe` |
| **.NET SDK** | .NET 8.0+ | `dotnet` on PATH (`C:\Users\<user>\.dotnet`) |
| **GitHub CLI** | `gh` CLI | `C:\Program Files\GitHub CLI\gh.exe` |

---

## 3. Automated Command-Line Build Pipeline

RichRich includes an automated build pipeline script located at:
`RichRich_Unity/Assets/Scripts/Editor/RichRichBuilder.cs`

### 3.1 One-Line Headless Release Build Command
To generate the release APK via Unity batch mode from PowerShell:

```powershell
# Set environment variables
$env:ANDROID_HOME = "C:\Android\Sdk"
$env:ANDROID_SDK_ROOT = "C:\Android\Sdk"
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21.0.13.21-hotspot"

# Execute Unity batchmode release build
$unityExe = "D:\unity6\Editor\Unity.exe"
$projectPath = "D:\Webapp\RichyRich\RichRich_Unity"
$outputPath = "D:\Webapp\RichyRich\Builds\Android\RichRich-v0.1.0-android-arm64.apk"
$logPath = "D:\Webapp\RichyRich\Builds\Android\build.log"

& $unityExe -batchmode -quit `
  -projectPath $projectPath `
  -executeMethod RichRich.Editor.RichRichBuilder.BuildAndroidRelease `
  -buildOutput $outputPath `
  -logFile $logPath
```

### 3.2 Output Artifact Specification
- **Destination:** `Builds/Android/RichRich-v0.1.0-android-arm64.apk`
- **Package ID:** `com.bishal.richrich`
- **Version Name:** `0.1.0`
- **Version Code:** `1`

---

## 4. Device Deployment & Verification via ADB

Once the APK is produced, deploy to a connected Android handset:

```powershell
# 1. Verify device connection and authorization
adb devices -l

# 2. Install APK with replace flag (-r)
adb install -r "D:\Webapp\RichyRich\Builds\Android\RichRich-v0.1.0-android-arm64.apk"

# 3. Launch application main activity
adb shell monkey -p com.bishal.richrich -c android.intent.category.LAUNCHER 1

# 4. Stream real-time Android logcat filtering for Unity & RichRich
adb logcat -s Unity:V RichRich:V AndroidRuntime:E
```

---

## 5. GitHub Release Automation

Release artifacts are published using the GitHub CLI:

```powershell
gh release create v0.1.0 "Builds/Android/RichRich-v0.1.0-android-arm64.apk" `
  --title "RichRich v0.1.0 - First Playable Android Build" `
  --notes-file "RELEASE_NOTES.md"
```
