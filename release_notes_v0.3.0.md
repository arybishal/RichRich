# RichRich v0.3.0 Release Notes: Visual Design, Art Direction & Metropolis Update

**Release Tag:** `v0.3.0`  
**Android Package:** `com.bishal.richrich`  
**Target Platform:** Android ARM64 (Portrait)  
**Target Architecture:** IL2CPP ARM64 (`arm64-v8a`, API 28 - 35)  
**Version Code:** `6`  
**Application Version:** `0.3.0`  
**APK Filename:** `RichRich-v0.3.0-android-arm64.apk`  
**APK Size:** 24,449,146 bytes (~23.31 MB)  
**APK SHA-256:** `16A5F37C566E957AAA7F698300961122ADC534D8DB8AFB92A64404742EE4620C`  

---

## Executive Summary

RichRich v0.3.0 delivers a complete visual transformation and presentation overhaul, evolving the game from a functional simulation MVP into a visually distinctive, polished metropolitan life and wealth management experience.

Every subsystem, business venture, real estate asset, and market mechanism remains 100% functional, auditable, and accessible while enjoying a cohesive, modern dark FinTech interface and an expanded 3D world.

---

## Key Features & Visual Design Systems Delivered

### 1. 4-Category Bottom Navigation Dock
- **Replaced 16-Button Grid:** Replaced the legacy 16-button vertical scrolling grid with an ergonomic, bottom-anchored 4-category dock:
  - 🏙️ **Metropolis:** Instant focus on the 3D metropolitan plaza and buildings.
  - 💼 **Life & Career:** Category portal hub for Career Board, Higher Education, Housing Leases, and Lifestyle Wellness.
  - 🏢 **Enterprises:** Category portal hub for Commercial Dashboard, Acquisitions Marketplace, Auto Repair, Convenience Store, Digital Agency, Staffing HR, and Commercial Credit.
  - 📈 **Wealth:** Category portal hub for Equities Exchange, Real Estate Portfolio, Life Progression Goals, and Settings.
- **Ergonomic Touch Targets:** High-DPI category vector icons accompanied by crisp labels, styled with rounded 9-slice textures (`dock_bg_9slice.png`).
- **Seamless Modal History:** Uses LIFO modal stack navigation where child modals seamlessly pop back to their parent category portal upon Android Back or Back button press.

### 2. Floating Translucent Financial Overview Card
- **Dark FinTech Glass Aesthetic:** Translucent slate card (`#070B13` at 90% alpha, `card_bg_9slice.png`) floating on the top of the HUD.
- **Hierarchical Financial Information:**
  - Hero Liquid Cash balance (`$3,000.00` in emerald `#10B981`) and Net Worth.
  - Real-time monthly metrics strip: Income (`+$X/mo`), Expenses (`-$X/mo`), and Net Cash Flow (`+$X/mo`).
  - Simulation calendar clock with speed controls (⏸ Pause, 1x Normal, 5x Fast, 💤 Sleep).
  - Career, Housing, Study, Energy, and Fatigue status chips.
- **Domain Ledger Grounding:** Every metric is bound strictly to `FinancialLedger` and `Wallet` domain events. Zero independently calculated or fake UI numbers.

### 3. 3D Low-Poly Metropolis & Player Character Avatar
- **Metropolitan City Environment:**
  - Asphalt boulevards and avenues with dashed white center lane markings.
  - 6 procedural low-poly street trees with dark bark trunks and tiered emerald foliage.
  - 4 procedural streetlamps with warm emissive yellow lamp heads.
  - 4 distant silhouette skyscrapers for metropolitan skyline depth.
- **5 Distinct Interactive Landmark Towers:**
  - *Stock Exchange Tower* (Center): Tiered glass-blue crystalline spire.
  - *Metropolitan Commercial Bank* (Left Front): Stepped vault building with golden architectural trim.
  - *Skyline Real Estate Plaza* (Right Front): Emerald high-rise with rooftop garden terrace.
  - *Apex Corporate HQ* (Left Rear): Staggered corporate block with violet accents.
  - *Residential District* (Right Rear): Multi-tier apartment tower with coral balconies.
- **Stylized Player Character Avatar:**
  - Fully modeled 3D low-poly character at the center plaza.
  - Animated limb movement: idle breathing bob and walking leg/arm swing animations.
  - Responsive movement: character walks toward tapped buildings before opening subsystem modals, equipped with a 0.5s safety timeout guarantee so navigation is never blocked.

### 4. Restrained Floating Currency Feedback
- **Non-Blocking Feedback:** Object-pooled `FloatingCurrencyFeedback` system listening to `FinancialService.OnTransactionExecuted`.
- **Visual Signals:**
  - Rising emerald indicators (`+$X.XX`) for salaries, business revenues, dividends, and property rents.
  - Sinking crimson indicators (`-$X.XX`) for living expenses, tuition, supplies, stock purchases, and loan repayments.
- **Accessibility:** Reduced-motion toggle supported for instantaneous feedback without floating animations.

### 5. Standardized 9-Slice Visual Language
- Pixel-perfect 9-slice textures generated and imported with 16px sliced borders:
  - `card_bg_9slice.png` (translucent dark container)
  - `dock_bg_9slice.png` (bottom dock container)
  - `btn_dark_9slice.png` (secondary and card action buttons)
  - `btn_primary_9slice.png` (primary affirmative actions)
  - `btn_danger_9slice.png` (destructive and back buttons)

---

## Automated Test Results

```
Test Run Summary:
Total Tests: 157
Passed: 157
Failed: 0
Skipped: 0
Duration: 867 ms
Test Assembly: tests/RichRich.Tests/bin/Debug/net8.0/RichRich.Tests.dll (.NET 8.0)
```

New automated test coverage added:
- `ModalStackControllerTests.CategoryPortal_Career_PushesAndPopsChildModalsCleanly`
- `ModalStackControllerTests.CategoryPortal_Enterprise_NestedSubsystemNavigation`
- `ModalStackControllerTests.CategoryDock_SwitchingRoots_ClearsPreviousCategoryStack`
- Updated `StartupAndRegressionTests.SettingsModalController_Reports_CorrectVersionAndPackageId` for v0.3.0

---

## Android Build Details

- **Package Name:** `com.bishal.richrich`
- **Version Code:** `6`
- **Version Name:** `0.3.0`
- **Min SDK:** 28 (Android 9.0 Pie)
- **Target SDK:** 35 (Android 15)
- **Target Architecture:** `arm64-v8a` (ARM64)
- **Scripting Backend:** IL2CPP
- **Build Output:** `Builds/Android/RichRich-v0.3.0-android-arm64.apk`
- **File Size:** 24,449,146 bytes (~23.31 MB)
- **SHA-256:** `16A5F37C566E957AAA7F698300961122ADC534D8DB8AFB92A64404742EE4620C`
