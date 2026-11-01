# Implementation Roadmap & Production Plan

## 1. Project Overview & Guiding Constraints

This roadmap outlines the complete production plan for building an **original, high-fidelity business wealth simulation game** based on the gameplay concepts investigated during Phase 1 reconnaissance.

### Core Non-Negotiable Constraints:
- **No Login / Signup:** Instant-play frictionless entry.
- **No Required Backend:** 100% offline-first playable simulation with zero server dependency for progression.
- **Local Multi-File Dual-Save Persistence:** Resilient `{Subsystem}.json` + `{Subsystem}.backup.json` architecture.
- **Original Branding & Assets:** 100% original graphics, UI design system, 3D models, sound effects, and fictional company names. Zero copyright/trademark infringement.
- **Clean-Room Engineering:** All mechanics independently coded without copying decompiled IL2CPP assemblies.

---

## 2. Staged Production Milestones

```
[M0: Foundation & Core Stack] ──> [M1: Clicker & Balance Engine] ──> [M2: Business & Mergers]
                                                                            │
┌───────────────────────────────────────────────────────────────────────────┘
▼
[M3: Financial Markets & Real Estate] ──> [M4: Luxury Showrooms & Islands] ──> [M5: Rankings & Achievements]
                                                                                        │
┌───────────────────────────────────────────────────────────────────────────────────────┘
▼
[M6: Audio, Polish, QA & Release]
```

---

### Milestone 0: Foundation & Core Stack
**Goal:** Initialize project infrastructure, dependency injection, and local persistence.
- [ ] Initialize Unity 6 (`6000.0.x` or `6000.2.x`) Android project with URP (Universal Render Pipeline).
- [ ] Configure IL2CPP, ARM64 target architecture, and Android Frame Pacing (Swappy).
- [ ] Import core libraries:
  - `Extenject / Zenject` (DI Container)
  - `R3` / UniTask (Reactive event pipeline)
  - `DOTween Pro` (Procedural UI tweens)
  - `XCharts` (Real-time financial charts)
  - `Newtonsoft.Json` (Dual-file save serialization)
  - `TextMeshPro` (Resolution-independent typography)
- [ ] Implement `SaveDataService` with temporary-write and backup rotation.
- [ ] Create persistent Navigation Shell with 5 bottom tabs, status bar, and Safe Area notch compensation.
- **Deliverable:** Compiling project with functional tab switching, save/load pipeline, and empty tab canvases.

---

### Milestone 1: Core Clicker Loop & Economy Engine
**Goal:** Implement the primary active tap mechanic, visual feedback, and initial economy progression.
- [ ] Build Skewmorphic Credit Card Widget with procedural gradients and animated balance counter.
- [ ] Implement center tap area with expanding radial ripple effect and fingerprint glyph punch-scale.
- [ ] Implement floating currency text (`+$4.00`) spawner with vertical float and cubic alpha fade.
- [ ] Implement level-up logic and progress bar interpolation.
- [ ] Implement Multiplier Promo Banner (e.g., 6x boost for 30s) and Auto-Clicker booster timer.
- [ ] Integrate low-latency click audio cues and haptic tick vibrations.
- **Deliverable:** Fully playable, satisfying active clicker loop with local balance persistence.

---

### Milestone 2: Business Management & Mergers System
**Goal:** Build the primary idle income foundation and multi-business conglomerate mergers.
- [ ] Implement Business Slot Manager (9 base slots + expansion slots).
- [ ] Build `Start a Business` 2-column catalog containing 14 base businesses (Store to Energy Company).
- [ ] Implement individual business upgrade paths (leveling stages, base income, bonus income).
- [ ] Implement business sub-mechanics:
  - Fleet management for Taxi & Delivery
  - Operations counter for Clinic & Vet
  - Car repairs counter for Auto Workshop
  - IT software projects counter for Tech Company
  - Movie production and box-office critic rating for Film Studio
- [ ] Build `Business Mergers` module with checklist validation:
  - Plastic Surgery ($700K + Hospital Lv.3 + Salon Lv.5)
  - Fashion Label ($4.5M + Retail Chain + Factory)
  - International Corporation ($4.88B + Shares + Vault)
  - Space Company ($450M + Fleet + Industrial Megaplant)
- **Deliverable:** Complete business empire simulation with active sub-activities and mergers.

---

### Milestone 3: Financial Markets & Real Estate
**Goal:** Deliver realistic simulated stock trading, cryptocurrency swings, and global real estate.
- [ ] Implement Stock Market Engine with 15 original parody equities.
- [ ] Implement real-time Geometric Brownian Motion / mean-reversion price tick algorithm.
- [ ] Integrate XCharts:
  - Row sparkline graphs with green/red trend coloring
  - Detailed 2-hour interactive historical chart with price axes, timestamps, and gradient area fill
- [ ] Implement Buy/Sell order execution, portfolio valuation, and 3-hour dividend payouts.
- [ ] Implement 100% float purchase mechanic ("Controlling Stake").
- [ ] Implement Cryptocurrency Exchange with 10 high-volatility crypto assets and dedicated wallet.
- [ ] Implement Real Estate Catalog with international property listings, purchasing, and hourly rental yield accumulation.
- **Deliverable:** Working financial investment ecosystem with interactive stock charts and rental income.

---

### Milestone 4: Luxury Showrooms & Prestige Collectibles
**Goal:** Provide late-game capital sinks and player status rewards.
- [ ] Build 3 Specialized Showrooms:
  - Car Showroom with trim configurator (Basic 1★, Average 2★ +15%, Max 3★ +35%) and engine options (Standard, Premium +40%)
  - Aircraft Showroom (Helicopters & Private Jets)
  - Yacht Showroom (Boats, Cruisers, Superyachts)
- [ ] Build Transport Hub views: Garage, Hangar, and Harbor.
- [ ] Build 10 Collectibles catalogs with item quotas (Motorbikes, Armor, Jewels, Artworks, etc.).
- [ ] Build Private Islands catalog with high-res aerial visuals, geographic lore, and multi-million dollar price tags.
- **Deliverable:** Complete luxury lifestyle progression allowing players to spend billions in prestige assets.

---

### Milestone 5: Forbes Rankings, Achievements & In-Game Shop
**Goal:** Implement meta-progression, lifetime milestones, and in-game shop items.
- [ ] Build 7-category General Expenses Donut Chart (`COMP_DONUT_CHART`) on Account Tab.
- [ ] Build Account Summary Ledger tracking all 6 distinct income sources (Rent, Business, Crypto, Tapping, Dividends, Trading).
- [ ] Implement Forbes-style Wealth Ranking with 5 progressive tiers ($50K to $150B+).
- [ ] Implement 56 Achievements with automatic event-driven trigger tracking.
- [ ] Build In-Game Shop:
  - Non-stacking permanent VIP Cards (Gold +20%, Platinum +35%, Black +50%)
  - Boost Clicker (auto-tap 30 min)
  - Ad-skip tickets
  - Starter Packs
- [ ] Implement Offline Income Catch-Up dialog upon application launch.
- **Deliverable:** Complete meta-progression, statistics, achievements, and monetization framework.

---

### Milestone 6: Audio, Polish, Performance & QA
**Goal:** Deliver a console-quality polished build with rock-solid frame pacing.
- [ ] Full audio design: Original UI tap sounds, cash register chimes, ambient lounge background music, purchase fanfares.
- [ ] Performance profiling: Ensure 60/120 FPS on Snapdragon / Dimensity Android devices with zero GC allocation in hot paths.
- [ ] Memory profiling: Texture compression (ASTC 6x6 / 8x8), audio streaming, asset garbage collection.
- [ ] Edge-case testing:
  - App killed during save operation (verify backup recovery)
  - System clock rolled back (verify anti-tamper)
  - Device orientation locked to portrait
  - Android display cutout / notch handling
- **Deliverable:** Release-ready APK and Google Play App Bundle (AAB).

---

## 3. Original Asset Creation Matrix

To ensure absolute originality and prevent IP contamination:

| Asset Category | Specification | Production Method |
| :--- | :--- | :--- |
| **Brand Identity** | Original game title, custom logo typography, modern financial icon glyphs | Vector Figma / Illustrator design |
| **UI Components** | Custom dark-mode cards, credit card textures, button states, progress bars | Custom 9-slice sprites and procedural shaders |
| **Vehicles (Cars/Jets/Yachts)** | Original 3D models with stylized realism; original car parody names | Blender low/mid-poly modeling or licensed royalty-free assets |
| **Real Estate & Islands** | Original photography / AI-assisted architectural renders with original city lore | High-resolution photography licensed under Unsplash/Pexels or custom digital paintings |
| **Stock & Crypto Tickers**| 100% original parody names (e.g., NovaTech, OmniGlobal, ApexPay, Stellaris) | Original creative fiction |
| **Audio & SFX** | Original UI clicks, coin drops, celebration stings, corporate chill lounge tracks | Custom synthesized audio or licensed royalty-free sound libraries |

---

## 4. Phase 1 Completion Gate

With this comprehensive reconnaissance package complete across all 8 technical documents and over 50 captured visual evidence frames, **Phase 1 (Reconnaissance Only)** is formally concluded.

Implementation will proceed under Phase 2 according to Milestone 0 specifications.
