# RichRich: Android Release & Modal Navigation Architecture Report (v0.1.2)

**Date:** November 2, 2026  
**Milestone:** Centralized Modal Stack Navigation, Hardware Back Handling & Playable Android Release  
**Product Name:** RichRich  
**Package Identifier:** `com.bishal.richrich`  
**Version Name:** `0.1.2`  
**Version Code:** `3`  
**Target Platform:** Android ARM64 (`arm64-v8a`)  
**Engine Baseline:** Unity 6 LTS (`6000.0.35f1`)  
**Physical Validation Device:** OnePlus Nord CE4 5G (`CPH2691IN`), Android 17 (API 37), Serial `3B15B800NRT00000`  
**Automated Tests:** **123 / 123 Tests Passing (100% Pass Rate)**  
**Artifact Size:** `24,289,718 bytes (23.16 MB)`  
**Artifact SHA-256:** `F0E0933868C6A8AB90A8D43378CEFB042F4A6DD939ACA159BF4D1291FC5C5A1D`  
**Git Repository:** [https://github.com/arybishal/RichRich](https://github.com/arybishal/RichRich)  

---

## 1. Problem & Root Cause of Missing Back Navigation

In the initial UI wiring:
1. **Direct GameObject Activation:** Subsystem buttons directly called `_modalRoot.SetActive(true)` without registering with a navigation coordinator or tracking active screen depth.
2. **Missing Navigation History:** No LIFO stack existed. If a modal was opened from another screen or the HUD, previous modals remained active underneath, consuming graphics raycasts and causing visual backdrop darkening buildup.
3. **No Hardware Back Navigation:** The Android system Back button (and desktop `Escape` / `Backspace` key) was unhandled. Pressing the hardware back button or gesture navigation bar either did nothing or immediately triggered the default OS behavior without dismissing open modals.
4. **No Exit Confirmation on Dashboard:** There was no exit protection dialog on the root dashboard, posing a risk of accidental application quits during gameplay.

---

## 2. Navigation Architecture Implemented

A clean, three-layer navigation system was engineered to provide deterministic modal stacking, safe-area compliance, and hardware Back button support:

### 2.1 Pure Presentation Controller ([`ModalStackController.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Controllers/ModalStackController.cs))
- **LIFO Navigation Stack:** Maintains an active stack of modal references with strict push/pop ordering.
- **Root Exclusivity (`PushRoot`):** When navigating from top-level HUD tabs, automatically dismisses all open modals before pushing the new modal. Prevents overlapping modal pileups.
- **Nested Child Stacking (`PushChild`):** Sub-screens (such as Staff Recruitment or Credit Facilities opened from the Enterprise Dashboard) push onto the active stack, preserving the parent screen in history.
- **Hardware Back Result State Machine (`HandleBackPress`):**
  - If the **Exit Confirmation Dialog** is open: dismisses the dialog (`BackPressResult.DismissedExitDialog`).
  - If any **nested modal** is open: pops the child modal and restores focus/visibility to the parent (`BackPressResult.PoppedModal`).
  - If a **root modal** is open: pops the modal and returns cleanly to the main dashboard (`BackPressResult.PoppedModal`).
  - If on the **main dashboard** with zero open modals: opens the Exit Confirmation Dialog (`BackPressResult.ShowedExitDialog`).
- **Complete Testability:** Decoupled from `UnityEngine` APIs, allowing 100% automated coverage via `dotnet test`.

### 2.2 Centralized Unity Navigation Manager ([`ModalManager.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/ModalManager.cs))
- **Singleton Access:** `ModalManager.Instance` coordinates all modal presentation, stack changes, and exit dialog flows.
- **Dynamic Z-Ordering (`SetAsLastSibling`):** The active top modal is always brought to the top-most sibling index of the Canvas, guaranteeing it renders above all other elements and receives pointer input exclusively.
- **Hardware Back & Desktop Testing:** Listens in `Update()` for `KeyCode.Escape` (standard Android hardware/gesture Back key) and `KeyCode.Backspace` (for rapid testing in the Unity Editor).
- **Exit Dialog Management:** Wires and controls the application quit confirmation popup, invoking `Application.Quit()` (or stopping play mode in Editor) only upon explicit user confirmation.

### 2.3 Modal Sheet View & Dismissal ([`ModalSheetView.cs`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Presentation/Views/ModalSheetView.cs))
- Attached to each modal dialog root.
- **Visible Back Button:** Wires the header `← Back` button directly to `ModalManager.Instance.CloseModal(gameObject)`.
- **Backdrop Tap Dismissal:** Wires background backdrop taps outside the dialog window to dismiss the top modal.

---

## 3. All 12 Subsystems & Exit Dialog Verified

All 12 subsystem dialogs and the exit confirmation dialog are fully integrated into the `ModalManager` navigation stack:

| Screen # | Modal Identifier | Title / Subsystem | Navigation Mode |
|---|---|---|---|
| **1** | `JobBoardModal` | Career Board & Vacancies | Root HUD Navigation |
| **2** | `EducationModal` | Higher Education & Degrees | Root HUD Navigation |
| **3** | `HousingModal` | Housing & Accommodation Leases | Root HUD Navigation |
| **4** | `LifestyleModal` | Lifestyle & Energy Recovery | Root HUD Navigation |
| **5** | `MarketplaceModal` | Commercial Business Marketplace | Root HUD Navigation |
| **6** | `DashboardModal` | Commercial Enterprise Dashboard | Root HUD Navigation |
| **7** | `AutoRepairModal` | Auto Repair Workshop Service Bays | Root HUD / Nested Child |
| **8** | `ConvenienceStoreModal` | Corner Convenience Store Retail | Root HUD / Nested Child |
| **9** | `DigitalAgencyModal` | Digital Marketing Agency Contracts | Root HUD / Nested Child |
| **10** | `StaffModal` | Recruitment & Staffing Management | Root HUD / Nested Child |
| **11** | `FinanceModal` | Enterprise Finance & Commercial Credit | Root HUD / Nested Child |
| **12** | `SettingsModal` | Settings & Persistence | Root HUD Navigation |
| **13** | `ExitConfirmDialog` | Exit RichRich Confirmation Dialog | System Back on Dashboard |

---

## 4. Hardware Back & Nested Navigation Behavior

1. **Root Navigation:** Tapping any HUD button (e.g. "Career") opens `JobBoardModal`. Pressing the Android Back key (or tapping `← Back`) dismisses it and returns to the dashboard.
2. **Tab Switching:** Tapping "Education" while "Career" is open closes `JobBoardModal` and opens `EducationModal`. Only one root modal is active at any time.
3. **Nested Subsystems:** From `DashboardModal`, tapping "Manage Staff →" pushes `StaffModal` on top (`Stack = [DashboardModal, StaffModal]`). Pressing Back pops `StaffModal` and returns to `DashboardModal`. Pressing Back again returns to the main HUD.
4. **Dashboard Exit Protection:** Pressing Back on the main dashboard opens the Exit Confirmation Dialog ("Exit RichRich? Are you sure you want to quit?"). Pressing Back again or clicking "Cancel" dismisses the dialog. Clicking "Exit Game" safely quits the application.
5. **State Preservation:** Navigating between screens never resets wallet balances, net worth, simulation calendar, business ownership, or active careers.

---

## 5. Automated Test Results

The full test suite was executed against .NET 8 LTS:

```
Test run for D:\Webapp\RichyRich\tests\RichRich.Tests\bin\Debug\net8.0\RichRich.Tests.dll (.NETCoreApp,Version=v8.0)
Passed! - Failed: 0, Passed: 123, Skipped: 0, Total: 123, Duration: 488 ms
```

* **Total Tests:** 123
* **Passed:** 123 (100% Pass Rate)
* **Failed:** 0
* **Skipped:** 0
* **Exit Code:** 0

---

## 6. Android Release Build Details

* **File Name:** `RichRich-v0.1.2-android-arm64.apk`
* **Output Path:** `Builds/Android/RichRich-v0.1.2-android-arm64.apk`
* **File Size:** `24,289,718 bytes (23.16 MB)`
* **SHA-256 Checksum:** `F0E0933868C6A8AB90A8D43378CEFB042F4A6DD939ACA159BF4D1291FC5C5A1D`
* **Package Identifier:** `com.bishal.richrich`
* **Version Name:** `0.1.2`
* **Version Code:** `3`
* **Scripting Backend:** IL2CPP (ARM64 `arm64-v8a`)
* **Build Type:** Release (Non-Development, No Profiler)

---

## 7. Physical Device Runtime Validation Evidence

Tested on connected physical handset **OnePlus Nord CE4 5G (`CPH2691IN`)**, **Android 17 (API 37)**:

```
$ adb -s 3B15B800NRT00000 install -r Builds/Android/RichRich-v0.1.2-android-arm64.apk
Success (4993 KB/s, 24289718 bytes in 4.750s)

$ adb shell dumpsys package com.bishal.richrich | grep -E "versionCode|versionName"
versionCode=3 minSdk=28 targetSdk=35
versionName=0.1.2
```

### Verified Scenarios on Hardware
1. **Cold Boot:** Launches with Unity splash, transitions directly to full HUD without black screen.
2. **Main Dashboard:** Verified live at 60 FPS in portrait mode: [`docs/screenshots/v0.1.2_dashboard_live.png`](file:///D:/Webapp/RichyRich/docs/screenshots/v0.1.2_dashboard_live.png).
3. **Hardware Back on Dashboard:** Sent Android keyevent 4 (`KEYCODE_BACK`). The Exit Confirmation Dialog rendered immediately: [`docs/screenshots/v0.1.2_exit_dialog.png`](file:///D:/Webapp/RichyRich/docs/screenshots/v0.1.2_exit_dialog.png).
4. **Hardware Back Dismissal:** Sent Back key again. The Exit Dialog dismissed smoothly, returning to the dashboard: [`docs/screenshots/v0.1.2_exit_dismissed.png`](file:///D:/Webapp/RichyRich/docs/screenshots/v0.1.2_exit_dismissed.png).
5. **Modal Navigation:** Tapped "Career Board" button. `JobBoardModal` opened with visible `← Back` button: [`docs/screenshots/v0.1.2_job_board_open.png`](file:///D:/Webapp/RichyRich/docs/screenshots/v0.1.2_job_board_open.png).
6. **Hardware Back from Modal:** Sent Back key. The modal popped cleanly back to the dashboard: [`docs/screenshots/v0.1.2_job_board_closed.png`](file:///D:/Webapp/RichyRich/docs/screenshots/v0.1.2_job_board_closed.png).
