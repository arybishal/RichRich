# RichRich v0.2.0 MVP Release Notes

**Release Tag:** `v0.2.0`  
**Android Package:** `com.bishal.richrich`  
**Target Platform:** Android ARM64 (Portrait)  
**Target Architecture:** IL2CPP ARM64 (API level 35/37)  
**Version Code:** `5`  
**Application Version:** `0.2.0`  
**APK Filename:** `RichRich-v0.2.0-android-arm64.apk`  
**APK Size:** 24,425,322 bytes (~23.29 MB)  
**APK SHA-256:** `424338C613B2074A05B19EAD7FD6767A2085F57AAFE38CC29088E133EFBBF585`  

---

## Executive Summary & Milestones Delivered

RichRich v0.2.0 delivers the cohesive, playable Android MVP across seven interconnected wealth and enterprise simulation systems, meeting the strict offline-first, auditable ledger, and non-clicker architectural criteria.

### 1. Milestone 6: Offline Simulated Financial Markets & Equity Portfolios
- **6 Realistic Market Instruments:**
  - `NVST` (Novus Technologies - Tech growth, 0.8% dividend yield)
  - `BIOC` (BioCure Therapeutics - Healthcare & Pharma, 1.2% dividend yield)
  - `SLRS` (Solaris Energy - Clean Utilities & Energy, 3.2% dividend yield)
  - `APXR` (Apex Consumer Products - Consumer Staples, 2.4% dividend yield)
  - `GLBK` (Global Alliance Bank - Financial Services, 4.1% dividend yield)
  - `TITN` (Titan Infrastructure - Industrial REIT & Materials, 3.8% dividend yield)
- **Macroeconomic Sentiment Engine:** 4 market sentiment regimes (`Bullish`, `Neutral`, `Bearish`, `Volatile`) with Markovian transitions and persistent sentiment horizons.
- **Trading Execution & Order Sizing:** Market Buy/Sell orders with order sizing (`-1`, `+1`, `+10`, `Max Buy`, `Max Sell`), brokerage commission (0.5%), and 60-day price history window.
- **Dividends & Ledger Integration:** Monthly dividend distributions credited directly to `Wallet` and recorded in `FinancialLedger`.
- **Automated Test Suite:** 9 dedicated stock market unit tests covering pricing, order sizing, commissions, dividend payouts, and balance sheet valuation synchronization.

### 2. Milestone 7: Life Progression, FIRE Metric & Dynamic Life Events
- **7 Actionable Life Milestone Goals:**
  1. *Financial Foundation*: Accumulate $10,000 net worth (Reward: $1,000 cash).
  2. *Corporate Climber*: Reach $4,000 monthly career salary (Reward: $1,500 cash).
  3. *Property Investor*: Acquire first real estate asset (Reward: $2,500 cash).
  4. *Serial Entrepreneur*: Own and operate 2+ commercial enterprises (Reward: $5,000 cash).
  5. *Wall Street Capitalist*: Build $50,000+ stock portfolio (Reward: $5,000 cash).
  6. *Financial Independence (FIRE)*: Passive income exceeds monthly living expenses (Reward: $10,000 cash).
  7. *Self-Made Millionaire*: Surpass $1,000,000 balance sheet net worth (Reward: $50,000 cash).
- **FIRE Ratio Calculation:** Mathematically compares aggregate passive income (Real Estate rents + stock dividends) against `LivingExpenseService.TotalMonthlyBudget`.
- **Dynamic Macro Events:** Interactive choice scenarios (angel investments, keynote honorariums, logistics freight surcharges, R&D tax rebates) with probabilistic payoffs, cash costs, and ledger trails.
- **Save Schema 7 Migration:** Complete backward-compatible snapshot persistence for goals and event timers.

### 3. Milestone 8: Original Playable 3D Metropolitan Environment
- **3D City Environment:** Stylized isometric skyline with key architectural landmarks rendered directly behind the dark FinTech HUD at 60 FPS mobile portrait:
  - *Stock Exchange Tower* (Center): Direct portal to Equities & Markets.
  - *Metropolitan Commercial Bank* (Left Front): Direct portal to Commercial Debt & Finance.
  - *Skyline Real Estate Plaza* (Right Front): Direct portal to Real Estate & Property Investments.
  - *Apex Corporate Headquarters* (Left Rear): Direct portal to Business Enterprise Dashboard.
  - *Residential District* (Right Rear): Direct portal to Housing & Accommodation leases.
- **Touch & Raycasting Interaction:** `CityInteractionController` with mobile pointer raycasting and squash/bounce visual feedback when tapping 3D building points of interest.
- **Full 3D Explore Mode:** Dedicated toggle between 2D scroll grid and full 3D skyline perspective with a floating return button.

### 4. Milestone 9: Final Polish, Verification & QA
- **Zero Black Screen Startup:** Fully verified cold-boot initialization on physical OnePlus Nord CE4 5G (Android 17 / API 37).
- **100% Passing Automated Tests:** 154 unit and integration tests passing with 0 failures and 0 skipped.
- **Auditable Financial Ledger:** Every single currency exchange, trade, dividend, rent payment, and achievement award audited through `FinancialLedger`.
- **Zero Tap-to-Earn Mechanics:** Clean life simulation driven by macroeconomic cycles and business decision-making.

---

## Automated Test Results

```
Test Run Summary:
Total Tests: 154
Passed: 154
Failed: 0
Skipped: 0
Duration: 611 ms
Test Assembly: tests/RichRich.Tests/bin/Debug/net8.0/RichRich.Tests.dll (.NET 8.0)
```

---

## Physical Device Verification Evidence

- **Device:** OnePlus Nord CE4 5G (`CPH2691IN`, Android 17 / API 37, Serial: `3B15B800NRT00000`)
- **Process Status:** Active PID 19599 (`com.bishal.richrich`)
- **Verified Scenarios:**
  1. Cold boot startup: Verified `[GameBootstrapper] RichRich Life & Enterprise Simulation successfully initialized!`.
  2. Stock Market Modal: Tapped `Equities & Markets`, bought 10 shares of `NVST` at $142.50 ($1,432.12 total including commission), verified portfolio and cash updates on HUD.
  3. Goals & FIRE Modal: Tapped `Goals & FIRE`, verified 7 progress bars, FIRE target ratio, and milestone award values.
  4. 3D City Plaza: Tapped `3D City Plaza`, entered 3D explore view, tapped the 3D Stock Exchange building, and confirmed seamless transition to the subsystem modal.
  5. Android Back: Hardware back button dismissed open modals and safely restored previous view states.
