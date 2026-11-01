# Gameplay Mechanics Specification

## 1. Core Game Loop Overview

The game loop operates on a hybrid **Active Tapping + Passive Portfolio Engine** progression model:

```
[ Active Tapping / Clicker ] ──> Generates Initial Seed Capital ($)
             │
             ▼
[ Base Business Acquisition ] ──> Generates Stable Hourly Passive Revenue
             │
             ├───> [ Stock & Dividend Portfolio ] ──> Yields Periodic Dividends & Capital Gains
             ├───> [ Crypto Speculation ] ──> High-Risk / High-Reward Swing Trading
             └───> [ Real Estate Purchases ] ──> High-Ticket Stable Rental Income
             │
             ▼
[ Business Mergers & Synergies ] ──> Unlocks Conglomerate Megacorporations & Space Agency
             │
             ▼
[ Luxury Purchases & Showrooms ] ──> Sinks Capital into Prestige Assets (Cars, Yachts, Jets, Islands)
             │
             ▼
[ Forbes Wealth Ranking Climb ] ──> Reaches Milestone Tiers (#1 Global Multibillionaire)
```

---

## 2. Active Tapping System (Clicker Loop)

### 2.1 Mechanics
- **Interaction:** Tapping anywhere inside the upper half tap area registers an immediate currency credit.
- **Base Reward:** Starts at **$4.00 per tap** at Level 1 (`IncomeData.json: "IncomeVariantId": "15_base"`).
- **Visual Feedback:**
  - Radial ripple effect originates at touch point (`docs/screenshots/02_tap_interaction.png`).
  - Floating green/white text indicator (e.g., `+$4.00`) animates upward and fades out.
  - Credit card balance dynamically increments without stutter.
  - Progress bar below the credit card fills proportionally toward the next level requirement ($1,000.00 at Level 1).
- **Haptic / Audio Feedback:** Low-latency click/coin audio cue triggers on down-event (`AudioTrack` session created in Unity logcat).

### 2.2 Tap Boosters & Multipliers
- **Rewarded Ad / Ticket Multiplier Banner:**
  - A purple banner appears periodically in the main Earnings tab: `"$24.00 per tap For 30 sec."` (`docs/screenshots/41_earnings_tab.png`).
  - Provides an immediate **6x tap multiplier** ($4.00 -> $24.00) for a 30-second burst duration.
  - *Verification Status:* **Verified behavior**.
- **Auto-Clicker Item:**
  - "Boost Clicker": Automatically registers continuous taps for 30 minutes without player finger fatigue (`AutoclickData.json: "AutoclickDuration"`).
  - *Verification Status:* **Verified behavior**.

---

## 3. Business Management System

### 3.1 Business Slot Architecture
- Player begins with **9 available business slots** (`BusinessUnlockData.json: {"BusinessSlots":9, "BusinessSlotsUnlockedByAds":0}`).
- Additional slots can be unlocked via rewarded actions (`Watch Ads for slot` button in `docs/screenshots/03_business_tab.png`).
- Total hourly business earnings are divided into:
  $$\text{Total Business Income} = \text{Base Income} + \text{Bonus Income}$$
  where Bonus income derives from upgrades, mergers, and active VIP cards.

### 3.2 Complete Business Inventory
Observed and cross-referenced from live screens (`docs/screenshots/04_start_a_business_modal.png`, `05_start_a_business_scroll.png`) and filesystem data:

| # | Business Type | Initial Cost | Primary Role / Special Mechanic | Verification Status |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Store (Retail Shop)** | $5,499.00 | Foundational retail store; upgradable stages | **Verified** |
| 2 | **Taxi Service** | $14,990.00 | Fleet management (cars capacity up to 300+ vehicles) | **Verified** |
| 3 | **Beauty Salon** | $29,990.00 | Personal service empire; stage upgrades | **Verified** |
| 4 | **Delivery Company** | $29,990.00 | Logistics fleet (city cars & long-distance transport) | **Verified** |
| 5 | **Manufacturing Plant** | $35,000.00 | Industrial manufacturing factory; multi-stage expansion | **Verified** |
| 6 | **Building Contractor** | $50,000.00 | Construction projects & infrastructure developments | **Verified** |
| 7 | **Autorepair Workshop** | $55,000.00 | Mechanical shop with active car repair operations | **Verified** |
| 8 | **Vet-Clinic** | $100,000.00 | Veterinary medicine; performing surgical operations | **Verified** |
| 9 | **Tech Company** | $355,000.00 | IT software firm; completing digital software projects | **Verified** |
| 10 | **Hospital / Clinic** | $500,000.00 | Medical center; diagnostic & surgical procedures | **Verified** |
| 11 | **Film Studio** | $1,240,000.00 | Movie production; movie ratings & box-office earnings | **Verified** |
| 12 | **Insurance Company** | $9,500,000.00 | Financial vault capitalization & policy reserves | **Verified** |
| 13 | **MMA Organization** | $25,000,000.00 | Sports promotion; championship tournament circuits | **Verified** |
| 14 | **Energy / Oil & Gas**| $990,000,000.00 | Industrial petroleum conglomerate | **Verified** |

### 3.3 Interactive Sub-Mechanics
The businesses are not mere idle counters; they contain operational tasks revealed in the achievement triggers:
- **Vet-Clinic & Hospital:** Performing surgical/diagnostic operations (`Animal Savior: 150 operations`, `Master Diagnostician: 150 clinic operations`).
- **Autorepair:** Servicing cars (`Auto Service Empire: repair 250 cars`).
- **Tech Company:** Shipping software releases (`Digital Giant: complete 250 IT projects`).
- **Film Studio:** Producing films to achieve maximum box-office/critic ratings (`Absolute Cinema`).
- **Taxi & Delivery:** Managing fleet asset allocations (`City on Wheels: 300+ taxis`).
- *Verification Status:* **Verified behavior**.

---

## 4. Business Mergers (Conglomerates)

Mergers allow players to fuse multiple businesses and fulfill capital thresholds to form high-tier monopolies:

| Merged Entity | Capital Requirement | Prerequisite 1 | Prerequisite 2 | Prerequisite 3 | Evidence Screenshot |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Plastic Surgery** | $700,000.00 | Hospital at Stage 3 | Beauty Salon at Stage 5 | — | `docs/screenshots/08_business_mergers.png` |
| **Fashion Label** | $4,500,000.00 | Large Chain of Shops | Small Factory | — | `docs/screenshots/08_business_mergers.png` |
| **International Corp** | $4,880,000,000.00 | Stock Portfolio > $38.00B | Insurance Vault > $400.00M | — | `docs/screenshots/09_business_mergers_scroll.png` |
| **Space Company** | $450,000,000.00 | Construction Earnings > $75M | Large Factory Stage 45 | Delivery Fleet: 235 long-distance + 115 city cars | `docs/screenshots/10_business_mergers_scroll2.png` |

- *Verification Status:* **Verified behavior**.

---

## 5. Investment Portfolios: Stock Market & Equities

### 5.1 Market Structure
- The stock market simulates 15 publicly traded corporate equities (`StocksData.json`).
- Each equity has:
  - Base Share Price ($)
  - Historical Price Buffer (`PricesForTwoHour` array of floats)
  - Market Capitalization ($)
  - Total Available Shares Count
  - Dividend Yield Rate (e.g., 1.20% per 3-hour cycle)
- Equities can be sorted via filter chips:
  - *Highest dividend*
  - *Lowest dividend*
  - *Cheap first*
- *Verification Status:* **Verified behavior**.

### 5.2 Complete Stock Roster
Extracted from live memory/persistence inspection:

1. **Amason:** $100.10 (Base), 750M shares, $75B+ Cap, 1.20% dividend / 3 hrs
2. **Appli:** $163.11, high-cap consumer tech
3. **CicssoSystems:** $57.43, networking hardware
4. **Coca:** $57.98, beverage staple
5. **Fort:** $12.12, entry-level automotive stock
6. **Henda:** $30.87, mass-market manufacturing
7. **Mikrosolt:** $332.54, enterprise computing
8. **Moestrocard:** $434.61, payment processing
9. **NeFlix:** $388.26, digital entertainment
10. **Niiki:** $95.95, athletic apparel
11. **NVIDA:** $463.60, semiconductor & AI hardware
12. **Pepsi.co:** $153.23, consumer food & beverages
13. **Tezla:** $233.19, electric mobility
14. **Toyoda:** $180.88, automotive conglomerate
15. **Viza:** $237.50, global payment network

### 5.3 Controlling Stake Mechanic
Purchasing 100% of the available float for an equity triggers the `Controlling Stake` achievement (`TrophiesData.json`), giving the player total corporate control.
- *Verification Status:* **Verified behavior**.

---

## 6. Cryptocurrency Exchange

### 6.1 Market Mechanics
- Features 10 crypto coins (`CryptocurrencyData.json`).
- High price volatility relative to stocks.
- Supports buying, holding in a dedicated Crypto Wallet, and selling for profit.
- Price updates dynamically across 2-hour sliding windows.
- *Verification Status:* **Verified behavior**.

### 6.2 Token Roster
1. **Dogicoin:** $0.20 (Micro-cap entry meme coin)
2. **Kardano:** $0.74 (Low-cap smart contract coin)
3. **Tocoin:** $1.95 (Utility token)
4. **Ripplli:** $2.77 (Settlement coin)
5. **Tron:** $4.54 (Content network coin)
6. **Litekoin:** $132.56 (Silver equivalent)
7. **Solana:** $165.09 (High-speed DeFi token)
8. **Binanze Coin:** $731.89 (Exchange token)
9. **Athereum:** $3,608.85 (Major smart contract platform)
10. **Bitkoin:** $121,910.92 (Flagship digital store of value)

---

## 7. Real Estate & Global Properties

### 7.1 Mechanics
- Properties are purchased as high-ticket capital expenditures.
- Each property delivers a guaranteed, steady **Hourly Rental Yield** (`IncomeData.json`, `MyPropertyData.json`).
- Locations span diverse international cities with realistic architectural visual assets (`docs/screenshots/24_real_estate_offers.png`).
- Total rental revenue accumulates into `Received by -> Rent` in the Account ledger.

### 7.2 Verified Property Sample
- **Stavropol, Russia:** $35,000.00 Purchase Price $\rightarrow$ **$564.00 / hour** rental yield.
- **Fes, Morocco:** $43,000.00 Purchase Price $\rightarrow$ **$690.00 / hour** rental yield.
- **Iquitos, Peru:** $62,000.00 Purchase Price $\rightarrow$ **$998.00 / hour** rental yield.
- **Tyumen, Russia:** $69,000.00 Purchase Price $\rightarrow$ **$1,110.00 / hour** rental yield.
- **Kathmandu, Nepal:** Mid-tier residential villa.
- *Verification Status:* **Verified behavior**.

---

## 8. Luxury Showrooms & Customization

Players spend accumulated wealth on lifestyle status symbols across 3 specialized showrooms:

### 8.1 Car Showroom & Configurator
- Allows custom vehicle building (`docs/screenshots/50_car_detail.png`):
  - **Trim Configuration:**
    - Basic (1 Star): Standard baseline cost
    - Average (2 Stars): **+15%** cost & value
    - Maximum (3 Stars): **+35%** cost & value
  - **Engine Configuration:**
    - Standard Engine: Default
    - Premium Engine: **+40%** cost & value
- Models include: Opyl Kadett E ($1,500 base), Taraband 501 ($2,700 base), Fort Focas Old, up to exotic supercars.
- Vehicles are stored in the player's personal **Garage**.

### 8.2 Aircraft Showroom
- Private aviation catalog: helicopters and jets (`SkyHawk` at $47K, `WindRider` at $120K, `AeroGlide`).
- Stored in the player's **Hangar**.

### 8.3 Yacht Showroom
- Maritime vessels: wooden skiffs (`Aqua Dream` at $10K), fishing/day boats (`Ocean Whisper` at $80K), motor yachts (`Elysium`).
- Stored in the player's **Harbor**.

---

## 9. Prestige Collectibles & Islands

### 9.1 Collectibles Categories
Players complete 10 distinct collections, each with set quotas:
1. Rare Motorbikes (6 items)
2. Air (20 items)
3. Scientific Artifacts (10 items)
4. Weapons & Armor (20 items)
5. Stamps (14 items)
6. Jewels (20 items - e.g., Diamond Ring $10M, Signet Ring $12M)
7. Exclusive Items (12 items)
8. Artworks (19 items)
9. Artefacts (6 items)
10. Islands (Private luxury real estate)

### 9.2 Private Islands
Ultra-late-game wealth sink:
- **Caye Chapel Island (Belize):** $23,000,000.00 (`docs/screenshots/55_island_view.png`)
- **North Aegean Island (Greece):** $38,000,000.00
- **Isla de Sa Ferradura (Spain):** $39,690,000.00
- Includes rich geographic and historical descriptions.

---

## 10. Forbes-Style Wealth Ranking

Tracks player lifetime net worth across 5 progressive milestone ladders (`docs/screenshots/33_wealth_ranking.png`):

| Rank Tier | Net Worth Requirement | Theme / Card Visual |
| :--- | :--- | :--- |
| **Tier 1: Aspiring Entrepreneurs** | $50,000 – $1,000,000 | Bronze metallic card |
| **Tier 2: Successful Business Owners** | $1,000,000 – $50,000,000 | Silver metallic card |
| **Tier 3: Millionaires, Regional Tycoons** | $50,000,000 – $500,000,000 | Gold metallic card |
| **Tier 4: Oligarchs, Billionaires** | $500,000,000 – $5,000,000,000 | Platinum brushed steel card |
| **Tier 5: Global Multibillionaires** | $5,000,000,000 – $150,000,000,000+ | Black Obsidian card |

Reaching #1 unlocks `The richest in the world` achievement.
- *Verification Status:* **Verified behavior**.
