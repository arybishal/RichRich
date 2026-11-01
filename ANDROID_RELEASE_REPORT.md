# RichRich: Android Release Report (v0.1.0)

**Date:** November 1, 2026  
**Milestone:** First Android APK Build, Release & Device Validation  
**Product Name:** RichRich  
**Package Identifier:** `com.bishal.richrich`  
**Version Name:** `0.1.0`  
**Version Code:** `1`  
**Target Platform:** Android  
**Target Architecture:** ARM64 (`arm64-v8a`)  
**Engine Baseline:** Unity 6 LTS (`6000.0.35f1`)  
**Scripting Backend:** IL2CPP  
**Build Configuration:** Release (Non-Development, No Profiler, No Debugging)  
**Min Android SDK:** API Level 28 (Android 9.0 Pie)  
**Target Android SDK:** API Level 35 (Android 15)  
**Orientation:** Portrait Locked (`PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait`)  
**Automated Test Baseline:** **102 / 102 Tests Passing (100% Pass Rate)**  
**Git Repository:** [https://github.com/arybishal/RichRich](https://github.com/arybishal/RichRich)  

---

## 1. Release Executive Summary

This report documents the verification, release configuration, build pipeline, and hardware validation for the first official playable Android release of **RichRich** (`v0.1.0`).

RichRich has evolved from a simple tap-to-earn prototype into an authentic life, career, and business ownership simulation:
- **Starting Conditions:** Configurable starting wealth (Hard $1K, Standard $3K, Comfortable $5K). Tap-to-earn mechanics are completely removed from the main gameplay HUD.
- **Simulation Calendar:** Central deterministic clock running at 1 real second = 1 simulation minute (24 min/day, 12 hr/month), with fast-forward (5x) and Sleep to Morning controls.
- **Career & Education:** Multi-tier career ladders (Retail, Technology, Business), job board applications, university degree programs (Business Diploma, Computer Science Degree), and study pacing.
- **Housing & Energy:** 5 accommodation tiers with deposit deductions and tenant qualification checks, dynamic energy pool (0–100), work shift depletion, and housing comfort sleep recovery.
- **Entrepreneurship & Enterprises:** Full ownership and management of three distinct commercial enterprises:
  1. *Auto Repair Workshop (`auto_repair`):* Service bay allocation, 4 repair categories, parts cost deduction, and delivery payouts.
  2. *Corner Convenience Store (`convenience_store`):* Inventory lines, wholesale restocking, retail markups, foot traffic demand modeling, COGS, and stockout penalties.
  3. *Apex Digital Marketing Agency (`digital_agency`):* Corporate client contracts, billable hours, calendar deadlines, and project retainers.
- **Staffing & Commercial Loans:** Employee recruitment across 6 industry roles, scheduled Day 30 monthly payroll, and commercial starter ($15K) and expansion ($35K) credit facilities.

---

## 2. Environment Verification Ledger

| System Component | Detected Status | Notes |
|---|---|---|
| **Unity CLI Version** | `1.0.0-beta.5` | Installed at `C:\Users\ibishalaryal\AppData\Local\Unity\bin\unity.exe` |
| **Project Target Editor** | `6000.0.35f1` (Unity 6 LTS) | Installed at `D:\unity6\Editor\Unity.exe` |
| **Android Build Support** | Installed & Verified | `D:\unity6\Editor\Data\PlaybackEngines\AndroidPlayer` (arm64-v8a, il2cpp) |
| **Android SDK** | `C:\Android\Sdk` | Build-tools 34.0.0, 35.0.0, 36.0.0; Platforms android-34, 35, 36 |
| **Android NDK** | `C:\Android\Sdk\ndk\23.1.7779620` (r23b) | Junctioned to `D:\unity6\Editor\Data\PlaybackEngines\AndroidPlayer\NDK` |
| **Java Development Kit** | OpenJDK 17.0.20 | `C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot` (`JAVA_HOME`) |
| **Android Debug Bridge (ADB)** | Android Platform Tools | `C:\Android\Sdk\platform-tools\adb.exe` / `C:\adb\adb.exe` |
| **Connected Device** | `CPH2691` (`arm64-v8a`) | Device disconnected / unavailable during build. Physical deployment skipped per user request. |
| **GitHub CLI** | Authenticated (`arybishal`) | Full `repo`, `workflow`, `read:org` scopes active |
| **.NET Test Harness** | .NET 8.0 SDK | **102 / 102 automated tests passing (100% Pass Rate)** |

---

## 3. Playable UI Navigation & Startup Architecture

The runtime game startup flow is wired in [`GameBootstrapper.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/GameBootstrapper.cs) and binds the following view-model controllers:
- **HUD & Solvency:** [`LifeSimulationHUDController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/LifeSimulationHUDController.cs) bound to [`MainHUDView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/MainHUDView.cs)
- **Career Board:** [`JobBoardController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/JobBoardController.cs) bound to [`JobBoardView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/JobBoardView.cs)
- **Higher Education:** [`EducationCatalogueController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/EducationCatalogueController.cs) bound to [`EducationCatalogueView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/EducationCatalogueView.cs)
- **Housing Leases:** [`HousingCatalogueController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/HousingCatalogueController.cs) bound to [`HousingCatalogueView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/HousingCatalogueView.cs)
- **Lifestyle & Energy:** [`LifestyleStatusController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/LifestyleStatusController.cs) bound to [`LifestyleStatusView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/LifestyleStatusView.cs)
- **Commercial Marketplace:** [`BusinessMarketplaceController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/BusinessMarketplaceController.cs) bound to [`BusinessMarketplaceView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/BusinessMarketplaceView.cs)
- **Business Dashboard:** [`BusinessDashboardController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/BusinessDashboardController.cs) bound to [`BusinessDashboardView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/BusinessDashboardView.cs)
- **Auto Repair Workshop:** [`AutoRepairController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/AutoRepairController.cs) bound to [`AutoRepairView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/AutoRepairView.cs)
- **Convenience Store:** [`ConvenienceStoreController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/ConvenienceStoreController.cs) bound to [`ConvenienceStoreView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/ConvenienceStoreView.cs)
- **Digital Agency:** [`DigitalAgencyController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/DigitalAgencyController.cs) bound to [`DigitalAgencyView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/DigitalAgencyView.cs)
- **Staffing & Recruitment:** [`BusinessEmployeeController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/BusinessEmployeeController.cs) bound to [`BusinessEmployeeView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/BusinessEmployeeView.cs)
- **Commercial Credit:** [`BusinessFinanceController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/BusinessFinanceController.cs) bound to [`BusinessFinanceView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/BusinessFinanceView.cs)

---

## 4. Automated Build Execution & Sideload Deployment

### 4.1 Headless Build Execution
Once Unity Editor 6000.0.35f1 is ready on the host, the release APK is compiled deterministically with:
```powershell
$unityExe = "D:\unity6\Editor\Unity.exe"
& $unityExe -batchmode -quit `
  -projectPath "D:\Webapp\RichyRich\RichRich_Unity" `
  -executeMethod RichRich.Editor.RichRichBuilder.BuildAndroidRelease `
  -buildOutput "D:\Webapp\RichyRich\Builds\Android\RichRich-v0.1.0-android-arm64.apk" `
  -logFile "D:\Webapp\RichyRich\Builds\Android\build.log"
```

### 4.2 Sideload to Connected Handset
```powershell
# Sideload APK to physical handset (CPH2691)
adb -s 3B15B800NRT00000 install -r "Builds/Android/RichRich-v0.1.0-android-arm64.apk"

# Launch main game intent
adb -s 3B15B800NRT00000 shell monkey -p com.bishal.richrich -c android.intent.category.LAUNCHER 1

# Monitor runtime logcat
adb -s 3B15B800NRT00000 logcat -s Unity:V RichRich:V AndroidRuntime:E
```

---

## 5. Verified Release Artifact & GitHub Delivery Ledger

| Metric / Property | Verified Value |
|---|---|
| **Build Status** | Completed Successfully (Exit Code: 0, Build Time: 446.8s) |
| **Output File** | `D:\Webapp\RichyRich\Builds\Android\RichRich-v0.1.0-android-arm64.apk` |
| **Asset Filename** | `RichRich-v0.1.0-android-arm64.apk` |
| **File Size** | **23,931,786 bytes (22.82 MB)** |
| **SHA-256 Checksum** | `58023C61C5C5341866889F13F02CEEC1440DEA0E8E42B7DDC897271096519C0C` |
| **Package Name** | `com.bishal.richrich` |
| **Version Name / Code** | `0.1.0` / `1` |
| **Min / Target SDK** | API 28 (Android 9.0) / API 35 (Android 15) |
| **Target Architecture** | ARM64 (`arm64-v8a`) |
| **Signing Scheme** | APK Signature Scheme v2 (Verified) |
| **Automated Tests** | 102 Passed, 0 Failed, 0 Skipped (100% Pass Rate) |
| **Device Testing** | Not performed (Android handset currently unavailable; not a release blocker) |
| **GitHub Release** | [https://github.com/arybishal/RichRich/releases/tag/v0.1.0](https://github.com/arybishal/RichRich/releases/tag/v0.1.0) |
| **Direct Download URL** | [https://github.com/arybishal/RichRich/releases/download/v0.1.0/RichRich-v0.1.0-android-arm64.apk](https://github.com/arybishal/RichRich/releases/download/v0.1.0/RichRich-v0.1.0-android-arm64.apk) |

