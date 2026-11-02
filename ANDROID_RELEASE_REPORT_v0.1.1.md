# RichRich: Android Release & Runtime Diagnosis Report (v0.1.1)

**Date:** November 2, 2026  
**Incident:** v0.1.0 Android Black Screen on Startup  
**Product Name:** RichRich  
**Package Identifier:** `com.bishal.richrich`  
**Version Name:** `0.1.1`  
**Version Code:** `2`  
**Target Platform:** Android ARM64 (`arm64-v8a`)  
**Engine Baseline:** Unity 6 LTS (`6000.0.35f1`)  
**Physical Validation Device:** OnePlus Nord CE4 5G (`CPH2691`), Android 17 (API 37), Serial `3B15B800NRT00000`  
**Automated Tests:** **108 / 108 Tests Passing (100% Pass Rate)**  
**Artifact SHA-256:** `E5FCB038191A504FA15B99D25F83BAC4722E04CDF342F770F01A4DCBAA9269C1`  
**Git Repository:** [https://github.com/arybishal/RichRich](https://github.com/arybishal/RichRich)  

---

## 1. Problem & Incident Summary

When `com.bishal.richrich` v0.1.0 was launched on physical Android hardware:
1. The Unity native splash screen rendered.
2. Immediately following the splash screen, the display turned solid black.
3. The application process remained alive (no crash, no ANR, no native SIGSEGV), but gameplay never became visible.

---

## 2. Root Cause Analysis (Evidence-Based)

Inspection of the v0.1.0 scene YAML (`Assets/Scenes/MainGameplay.unity`) and runtime device telemetry revealed the root cause:

1. **Bare Scene Architecture:**
   The startup scene `Assets/Scenes/MainGameplay.unity` (index 0 in `EditorBuildSettings.asset`) was a bare skeleton containing only two GameObjects:
   - `Main Camera`: configured with `ClearFlags: SolidColor` and `BackGroundColor: {r: 0.05, g: 0.06, b: 0.08, a: 1}` (`#0D0F14` - near pure black).
   - `GameBootstrapper`: a bare GameObject containing **only a Transform component**. The `GameBootstrapper.cs` MonoBehaviour was **not attached** in the serialized scene file.
2. **Missing UI Hierarchy:**
   The scene contained **no Canvas, no EventSystem, no GraphicRaycaster, and none of the 14 UI views**.
3. **Execution Gap:**
   Because `GameBootstrapper` was not attached, `Awake()` was never called on boot. Even if it had been called, all serialized view fields were null, and with no Canvas present, the camera rendered its solid black clear color (`#0D0F14`) at 60 FPS indefinitely.
4. **Data Overwrite Hazard:**
   `GameBootstrapper.cs` was invoking `LegacyCoordinator?.SaveCurrentState()`, which wrote Schema Version 1 data and risked overwriting Schema Version 4 simulation state upon backgrounding.

---

## 3. Implemented Fixes & Architectural Improvements

### 3.1 Deterministic Scene Generator ([`RichRichSceneBuilder.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Editor/RichRichSceneBuilder.cs))
- Authored an automated Editor scene generator that constructs the complete production hierarchy:
  - **Main Camera:** Solid color `#0A0F1A` (Dark FinTech Slate), orthographic size 5.0, depth -1, culling mask = Everything.
  - **EventSystem:** `EventSystem` + `StandaloneInputModule` for Android touch and pointer input.
  - **Screen-Space Canvas:** `RenderMode.ScreenSpaceOverlay`, `CanvasScaler` configured for 1080x1920 portrait reference (0.5 match), `GraphicRaycaster`.
  - **SafeArea & MainHUDView:** Top header with simulation date, clock speed, and controls (Pause, 1x, 5x, Sleep); Financial Summary Card (Cash Balance, Net Worth, Monthly Income, Monthly Expenses, Cash Flow, Solvency Badge); Lifestyle & Career Summary Card (Career, Study, Housing, Energy, Fatigue); and Subsystem Navigation Buttons.
  - **12 Subsystem Modals:** Pre-authored dialog sheets with modal backdrops, headers, close buttons, and bound serialized fields for Career Board, Higher Education, Housing Leases, Lifestyle & Rest, Enterprise Market, Business Dashboard, Auto Repair Workshop, Corner Store, Digital Agency, Staff & HR, Commercial Credit, and Settings.
  - **GameBootstrapper Component:** Attached to a persistent GameObject with all serialized view references linked.
- Resulting scene size: 871 KB (expanded from 6 KB).

### 3.2 Self-Healing Bootstrapper & Error Recovery ([`GameBootstrapper.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/GameBootstrapper.cs))
- **Self-Healing Fallback UI:** If `_hudView` or Canvas is ever missing at runtime, `EnsureRuntimeUI()` dynamically generates a full-screen Canvas and HUD and binds them to the simulation controllers.
- **Startup Diagnostic Recovery Screen:** Wrapped cold boot in try-catch; if an unhandled exception occurs, an emergency high-contrast overlay renders the exception details, stack trace, and action buttons ("Reset Save Data & Reload" and "Retry Normal Startup"). The app never fails silently into a black screen.
- **Persistence Authority:** Removed destructive `LegacyCoordinator` save calls to protect Schema Version 4 user saves.

### 3.3 Automated Build Integration ([`RichRichBuilder.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Editor/RichRichBuilder.cs))
- Automatically invokes `RichRichSceneBuilder.BuildMainScene()` before invoking `BuildPipeline.BuildPlayer()`.
- Automatically sets JDK 17 environment variables and EditorPrefs (`AndroidJdkPath`, `JAVA_HOME`).
- Incremented `VersionName` to `0.1.1` and `VersionCode` to `2`.

---

## 4. Automated Test Results

The test suite was executed against .NET 8 LTS, adding regression tests covering corrupted save recovery, schema backward compatibility, version reporting, and simulation loop stability:

```
Test run for D:\Webapp\RichyRich\tests\RichRich.Tests\bin\Debug\net8.0\RichRich.Tests.dll (.NETCoreApp,Version=v8.0)
Passed! - Failed: 0, Passed: 108, Skipped: 0, Total: 108, Duration: 497 ms
```

- **Total Tests:** 108
- **Passing:** 108 (100% Pass Rate)
- **Exit Code:** 0

---

## 5. Physical Device Runtime Validation

The release APK was sideloaded to physical handset **OnePlus Nord CE4 5G (`CPH2691`)** running **Android 17 Preview (API 37)**:

```
Installing updated APK to device 3B15B800NRT00000...
Performing Streamed Install -> Success
Package update: versionCode=2, versionName=0.1.1
Launching com.bishal.richrich...
PID: 26651
```

### Logcat Evidence
```
11-02 14:26:34.256 I Unity: ApplicationInfo 'com.bishal.richrich', Version '0.1.1', Min API Level '28', Target API Level '35'
11-02 14:26:34.256 I Unity: Device Model 'OnePlus CPH2691', OS 'Android OS 17 (API 37)'
11-02 14:26:36.939 I Unity: [GameBootstrapper] RichRich Life & Enterprise Simulation successfully initialized!
11-02 14:26:36.951 W VRI[UnityPlayerActivity]: updateBlastSurfaceIfNeeded, surfaceSize:Point(1264, 2780)
```

### Physical Verification Criteria
1. **Splash Screen:** Completes cleanly.
2. **Gameplay UI:** Renders immediately at 60 FPS in portrait orientation with `#0A0F1A` dark background.
3. **HUD Readability:** Net Worth ($3,000.00), Liquid Cash ($3,000.00 in bright green), Date (Year 1, Month 1 - Day 1), Speed (1x), Career (Unemployed), Housing (Homeless), Energy (100/100) are fully legible.
4. **Touch Input & Navigation:** Subsystem navigation buttons and speed controls respond to physical touches.
5. **Cold Reload & Persistence:** Backgrounding (HOME button) and resuming preserves simulation state without crash.
6. **Screenshots Captured:**
   - Reproduction on v0.1.0: `docs/screenshots/v0.1.0_blackscreen_reproduction.png`
   - Verified gameplay on v0.1.1: `docs/screenshots/v0.1.1_gameplay_live.png`
   - Interactive button tap: `docs/screenshots/v0.1.1_after_tap.png`

---

## 6. Build Artifact Summary

| Property | Value |
|---|---|
| **File Name** | `RichRich-v0.1.1-android-arm64.apk` |
| **Output Path** | `Builds/Android/RichRich-v0.1.1-android-arm64.apk` |
| **File Size** | `24,105,826 bytes (22.99 MB)` |
| **SHA-256 Checksum** | `E5FCB038191A504FA15B99D25F83BAC4722E04CDF342F770F01A4DCBAA9269C1` |
| **Package** | `com.bishal.richrich` |
| **Version Code** | `2` |
| **Version Name** | `0.1.1` |
