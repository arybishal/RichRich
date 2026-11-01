# Reference Game Analysis: RichLife (Business Billionaire Empire)

## 1. Executive Summary & Reconnaissance Scope

This document details the reconnaissance and technical teardown of the commercial mobile title **RichLife: Money Challenge** (Package: `com.extbel.BusinessBillionaireEmpire`), conducted under Phase 1 (Reconnaissance Only).

The objective is to establish an empirical, evidence-backed foundation for engineering an **original, clean-room business and wealth simulation game** with comparable, engaging depth while introducing original branding, architecture, UI, and economy design. No proprietary source code, art assets, or audio were copied or extracted.

---

## 2. Target Device & Environment Telemetry

The live reconnaissance was executed on a physical Android test device connected via Android Debug Bridge (ADB) with USB debugging enabled.

| Attribute | Telemetry Value | Evidence Source |
| :--- | :--- | :--- |
| **Device Serial** | `3B15B800NRT00000` | `adb devices -l` / REA `list_adb_devices` (`ev_7bfa384b...`) |
| **Product / Model** | `CPH2691IN` / `CPH2691` (OnePlus Nord 4) | `dumpsys display` |
| **Physical Resolution** | `1264 x 2780` pixels | `wm size` / REA `inspect_adb_display` (`ev_e0c4e78d...`) |
| **Physical / Override Density** | `560` dpi (Physical) / `476` dpi (Override) | `wm density` |
| **Target Architecture** | `arm64-v8a` (64-bit ARM) | `split_config.arm64_v8a.apk` inspect |
| **Android Package Name** | `com.extbel.BusinessBillionaireEmpire` | REA `resolve_adb_packages` (`ev_7f45ac0b...`) |
| **Installed Version** | Version Name `1.9.13`, Version Code `95` | REA `inspect_adb_package` (`ev_2c0acfa0...`) |
| **Launcher Activity** | `com.unity3d.player.UnityPlayerActivity` | `am start -W` / REA `start_adb_app` (`ev_3cb34ed5...`) |
| **Installation Source** | `com.android.vending` (Google Play Store) | `dumpsys package` |

---

## 3. Reference Binary Architecture & Engine Breakdown

Inspection of the package manifest, binary headers, and extracted configuration confirmed the underlying engine stack:

- **Engine:** Unity Engine `6000.2.14f1` (Unity 6 LTS release).
  - *Evidence:* Read from `assets/bin/Data/data.unity3d` and `assets/bin/Data/Resources/unity default resources`.
- **Scripting Backend:** IL2CPP (Ahead-Of-Time compiled native C++ binary).
  - *Evidence:* Presence of `lib/arm64-v8a/libil2cpp.so` and `assets/bin/Data/Managed/Metadata/global-metadata.dat`.
- **Core Frameworks & Plugins:**
  - **Extenject / Zenject (`Zenject.dll`):** High-performance dependency injection framework used to decouple managers, controllers, and UI views.
  - **R3 Reactive Extensions (`R3.dll`, `R3.Unity.dll`):** Cysharp's high-efficiency asynchronous/reactive framework for event pub/sub.
  - **DOTween (`DOTween.dll`):** Demigiant tween engine powering procedural UI transitions, number animations, and tap punch-scales.
  - **XCharts (`XCharts.Runtime.dll`):** Real-time procedural data-visualization engine rendering multi-point financial sparklines and filled area graphs for stocks and crypto.
  - **Newtonsoft Json.NET (`Newtonsoft.Json.dll`):** Serialization framework driving the local multi-file state persistence.
  - **Google Swappy (`libswappywrapper.so`):** Android Frame Pacing library ensuring smooth 60/120 Hz render pacing.

---

## 4. Visual Evidence Ledger

All screens were systematically navigated and captured directly from the physical hardware framebuffer via ADB.

| File Reference | Captured Screen / Interaction | Verified State / Details |
| :--- | :--- | :--- |
| `docs/screenshots/01_initial_screen.png` | Main Earnings Screen (Cold Launch) | Verified Level 1 start: $4.00 per tap, $0.00 profit/hr, credit card UI, gift icon with badge, 5 bottom tabs. |
| `docs/screenshots/02_tap_interaction.png` | Active Fingerprint Tap Interaction | Verified tap response: balance changes from $4.00 to $8.00, floating `+$4.00` text emerges, radial touch ripple occurs. |
| `docs/screenshots/03_business_tab.png` | Business Tab Root | Verified income breakdown (Base vs Bonus), Mergers CTA, Start Business CTA, 9/9 Free Slots indicator. |
| `docs/screenshots/04_start_a_business_modal.png` | Start Business Grid (Top) | Verified business costs: Store ($5.49K), Taxi ($14.99K), Salon ($29.99K), Delivery ($29.99K), Plant ($35K), Hospital ($500K). |
| `docs/screenshots/05_start_a_business_scroll.png` | Start Business Grid (Scrolled) | Verified higher tier businesses: MMA ($25M), Energy Company ($990M). |
| `docs/screenshots/08_business_mergers.png` | Business Mergers Screen | Verified merger prerequisites: Plastic Surgery requires $700K opening + Hospital Stage 3 + Salon Stage 5. |
| `docs/screenshots/09_business_mergers_scroll.png` | Business Mergers (Scrolled) | Verified corporate conglomerate requirements: International Corp requires $4.88B + $38B shares + $400M vault. |
| `docs/screenshots/10_business_mergers_scroll2.png` | Business Mergers (Space Agency) | Verified end-game merger: Space Company requires $450M opening + Construction earnings >$75M + Factory Stage 45 + Fleet. |
| `docs/screenshots/13_after_ad_close.png` | Interstitial Ad Dismissal | Verified ad triggers upon navigating out of modal screens; timer dismiss button. |
| `docs/screenshots/15_investments_tab.png` | Investments Main Tab | Verified 3 subtabs: Share, Crypto, Real Estate; stock portfolio summary and yield trackers. |
| `docs/screenshots/16_stock_market.png` | Stock Market List | Verified 15 parody equities with live pricing, sparkline graphs, % change, and filter chips (Highest/Lowest dividend, Cheap first). |
| `docs/screenshots/17_stock_detail.png` | Stock Detail Screen (Amason) | Verified real-time area chart with 24-min timestamp intervals, share capitalization ($80B+), yield per 3-hour period (1.20%). |
| `docs/screenshots/18_buy_shares_modal.png` | Stock Market Live Tick | Verified real-time price fluctuation while observing screen ($107.15 -> $110.33). |
| `docs/screenshots/21_investments_crypto_attempt2.png` | Crypto Exchange Tab | Verified Crypto wallet view, Buy flow, empty state with guidance to buy from exchange. |
| `docs/screenshots/23_real_estate_attempt2.png` | Real Estate Hub | Verified Rental income per hour tracker, "My property" map launcher, "Real estate offers" launcher. |
| `docs/screenshots/24_real_estate_offers.png` | Real Estate Offers Catalog | Verified property cards with photos, city/country location, base purchase price ($35K Stavropol, $43K Fes), hourly rental yield. |
| `docs/screenshots/28_luxe_tab.png` | Luxe Main Hub | Verified 3 showrooms (Cars, Aircraft, Yachts), transport hubs (Garage, Hangar, Harbor), and Collections catalog. |
| `docs/screenshots/31_account_tab.png` | Account Tab (Upper) | Verified General Expenses 7-segment donut chart, 56 Achievements card, summary statistics. |
| `docs/screenshots/32_account_scroll.png` | Account Tab (Income Ledger) | Verified 6-point income source ledger: Rent, Businesses, Crypto trading, Tapping, Dividends, Trading. |
| `docs/screenshots/33_wealth_ranking.png` | Forbes Wealth Rankings | Verified 5 tiers of wealth milestones: Aspiring Entrepreneur ($50K), Business Owner ($1M), Tycoon ($50M), Oligarch ($500M), Multibillionaire ($5B+). |
| `docs/screenshots/37_achievements_screen.png` to `40_achievements_scroll3.png` | Achievements System (56 Total) | Verified achievements revealing custom business mechanics: IT projects, car repairs, clinic surgeries, MMA championship, clicker boost tiers. |
| `docs/screenshots/41_earnings_tab.png` | Earnings Tab Multiplier Ad Banner | Verified temporary multiplier ad prompt: "$24.00 per tap For 30 sec." (6x tap multiplier). |
| `docs/screenshots/42_shop_screen.png` to `44_shop_scroll2.png` | In-Game Shop Catalog | Verified monetization options: Permanent VIP Cards (Gold +20%, Platinum +35%, Black +50%), Boost Clicker (auto-tap 30 min), Starter Pack, No Forced Ads, Ad Skip Tickets. |
| `docs/screenshots/46_gift_modal.png` | Daily / Instant Gift Claim | Verified instant cash gift mechanism ($32.00 bonus balance upon claiming unbadged gift icon). |
| `docs/screenshots/49_car_showroom.png` & `50_car_detail.png` | Car Showroom & Configurator | Verified multi-stage vehicle configurator: Trim Levels (Basic 1★, Average 2★ +15%, Maximum 3★ +35%) and Engine (Standard vs Premium +40%). |
| `docs/screenshots/51_aircraft_showroom.png` | Aircraft Showroom | Verified aviation catalog: helicopters and jets (SkyHawk $47K, WindRider $120K). |
| `docs/screenshots/52_yacht_showroom.png` | Yacht Showroom | Verified maritime catalog: Aqua Dream boat ($10K), Ocean Whisper ($80K), luxury yachts. |
| `docs/screenshots/54_islands_detail.png` & `55_island_view.png` | Private Islands Catalog | Verified ultra-luxury private island listings: Caye Chapel ($23M), Aegean Island ($38M), detailed lore and geographic specs. |

---

## 5. Verification Status Matrix

Every observation in this analysis adheres to rigorous scientific classification:

| Feature / Domain Concept | Status | Basis of Verification |
| :--- | :--- | :--- |
| **Single-Player Local Architecture** | **Verified** | Inspected `/sdcard/Android/data/.../files/`: 40 distinct subsystem `.json` files; game functions offline without authentication. |
| **Dual-Save Redundancy System** | **Verified** | Every subsystem writes `{Name}.json` and `{Name}.backup.json`; confirmed via runtime filesystem inspection and Unity error fallback logs. |
| **Real-time Stock Price Simulation** | **Verified** | Observed live tick fluctuations in `StocksData.json` and on screen (`17_stock_detail.png` vs `18_buy_shares_modal.png`). |
| **Non-Stacking VIP Card Rules** | **Verified** | Explicitly stated in Shop UI (`docs/screenshots/42_shop_screen.png`): "Only highest active bonus applies at all times." |
| **Business Sub-Activities (Surgeries, IT Projects, Car Repairs)** | **Verified** | Explicitly recorded in Achievement triggers (`Animal Savior`, `Master Diagnostician`, `Auto Service Empire`, `Digital Giant`). |
| **Offline Income Cap & Formula** | **Inferred** | Discovered IL2CPP properties `get_IsOfflineIncomeLimitEnabled`, `get_MaxOfflineIncomeHours`, `PendingOfflineIncomeData`. Default cap inferred at 2–3 hours. |
| **Dynamic Stock Drift Algorithm** | **Inferred** | Inspected `PricesForTwoHour` array in `StocksData.json`; price updates use bounded random walk with mean-reversion around baseline capitalization. |
| **Crypto Market Volatility Model** | **Inferred** | Crypto coin prices exhibit larger percentage swings than equities; higher risk/reward yield intended for speculative active trading. |
| **Push Notification Schedules** | **Not yet investigated** | `NotificationData.json` exists on device; exact notification intervals and deep link triggers remain unexecuted. |
| **Cloud Save / Backup Sync** | **Not yet investigated** | No cloud login UI was presented during cold start; remote Firebase sync suspected as purely optional or analytics-only. |
