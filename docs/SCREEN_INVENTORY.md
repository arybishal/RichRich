# Screen Inventory & Navigation Architecture

## 1. Global Navigation Architecture

The game uses a **Sticky Bottom Tab Navigation Bar** persistently pinned at the bottom of the viewport ($y \approx 2500 - 2700\text{ px}$ on a $1264 \times 2780$ display).

```
┌─────────────────────────────────────────────────────────────┐
│                       Top Status Bar                        │
│   Profit/Hour Indicator                  Gift / Rewards     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                                                             │
│                      Active Tab View                        │
│                                                             │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ [Business]  [Investments]  [Earnings]  [Luxe]  [Account]    │
│    (126)        (380)        (632)     (884)    (1138)      │
└─────────────────────────────────────────────────────────────┘
```

The active tab is highlighted with a vivid blue circular indicator backdrop. Secondary screens and detail catalogs push on top as full-screen modal pages with an upper-left Back button ($x \approx 70, y \approx 335$).

---

## 2. Comprehensive Screen Ledger

### Tab 1: Earnings (Home / Clicker Screen)
- **Screen ID:** `SCR_EARNINGS_HOME`
- **Primary Evidence Screenshot:** `docs/screenshots/01_initial_screen.png`, `02_tap_interaction.png`, `41_earnings_tab.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Profit Per Hour Header:** Displays total aggregate passive earnings/hr ($x \approx 100, y \approx 330$).
  - **Free Gift Button:** Top right gift box with exclamation badge ($x \approx 1160, y \approx 340$). Tapping grants immediate cash grant ($32.00 in test).
  - **Credit Card Widget:** Skewmorphic debit/credit card displaying current balance (`Balance $4.00`), card number `5282 3456 7890 1289`, expiration date `09/28`, Mastercard-style overlapping circles.
  - **Per Tap Tracker & Progress Bar:** Displays `$4.00 per tap`, current level (`1 level`), target balance (`$1,000.00`), and horizontal fill bar.
  - **Utility Action Row:**
    - `No ADS` button (white pill, $x \approx 250, y \approx 1100).
    - `Shop` button (blue pill, $x \approx 740, y \approx 1100).
  - **Multiplier Promo Banner:** Appears conditionally: `"$24.00 per tap For 30 sec."` (purple pill banner, $x \approx 500, y \approx 1300).
  - **Interactive Tap Area:** Large center region with biometric fingerprint glyph. Tapping anywhere in bounds triggers currency increment, floating text, and circular radial ripple.

---

### Tab 2: Business Tab
- **Screen ID:** `SCR_BUSINESS_MAIN`
- **Primary Evidence Screenshot:** `docs/screenshots/03_business_tab.png`, `14_business_tab_restored.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Header:** "Business" title, current player balance counter.
  - **Hourly Income Summary Card:** Large white typography showing `$0.00 Total income per hour`, accompanied by subtitle breakdown `Base: $0.00` and `Bonus: $0.00`.
  - **Action Row:**
    - `Business Mergers` (purple pill button, $x \approx 250, y \approx 840).
    - `Start a business` (white pill button, $x \approx 740, y \approx 840).
  - **Slot Status:** `Business List` header with `Free Slots 9/9` badge.
  - **Rewarded Slot Expansion:** `Watch Ads for slot` (green pill button, $x \approx 210, y \approx 1080).
  - **Active Business Grid:** Vertically scrollable list of owned businesses with individual upgrade meters and sub-action triggers.

---

### Sub-Screen 2A: Start a New Business Catalog
- **Screen ID:** `SCR_BUSINESS_CATALOG`
- **Primary Evidence Screenshot:** `docs/screenshots/04_start_a_business_modal.png`, `05_start_a_business_scroll.png`, `06_start_a_business_bottom.png`
- **Verification Status:** **Verified behavior**
- **Navigation Route:** Tapping `Start a business` from `SCR_BUSINESS_MAIN`.
- **Layout:** 2-column scrollable card grid:
  1. *Store* ($5,499.00)
  2. *Taxi* ($14,990.00)
  3. *Beauty Salon* ($29,990.00)
  4. *Delivery Company* ($29,990.00)
  5. *Manufacturing Plant* ($35,000.00)
  6. *Building Contractor* ($50,000.00)
  7. *Autorepair* ($55,000.00)
  8. *Vet-Clinic* ($100,000.00)
  9. *Tech Company* ($355,000.00)
  10. *Hospital* ($500,000.00)
  11. *Film Studio* ($1.24M)
  12. *Insurance Company* ($9.50M)
  13. *MMA* ($25.00M)
  14. *Energy Company* ($990.00M)

---

### Sub-Screen 2B: Business Mergers
- **Screen ID:** `SCR_BUSINESS_MERGERS`
- **Primary Evidence Screenshot:** `docs/screenshots/08_business_mergers.png`, `09_business_mergers_scroll.png`, `10_business_mergers_scroll2.png`
- **Verification Status:** **Verified behavior**
- **Navigation Route:** Tapping `Business Mergers` from `SCR_BUSINESS_MAIN`.
- **Layout:** Vertical card list with multi-condition checklists:
  - *Plastic Surgery:* Requirements checklist ($700K investment + Hospital Stage 3 + Beauty Salon Stage 5).
  - *Fashion Label:* Requirements checklist ($4.50M investment + Large chain of shops + Small factory).
  - *International Corporation:* Requirements checklist ($4.88B investment + Share portfolio >$38.00B + Insurance vault >$400.00M).
  - *Space Company:* Requirements checklist ($450.00M investment + Construction earnings >$75.00M + Large factory Stage 45 + Delivery fleet: 235 long-distance & 115 city cars).

---

### Tab 3: Investments Tab
- **Screen ID:** `SCR_INVESTMENTS_MAIN`
- **Primary Evidence Screenshot:** `docs/screenshots/15_investments_tab.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Sub-Tab Pill Bar:** Top horizontal selector: `Share` | `Crypto` | `Real Estate` ($y \approx 470$).
  - **Stock Portfolio Metric Card:**
    - Total Portfolio Value ($0.00)
    - All-time return percentage and dollar gain (`0.00 $ (0.00)%`)
    - Estimated Yield Per Hour ($0.00)
  - **Navigation Cards:**
    - `My Shares` (white card, bar chart icon, $x \approx 250, y \approx 1500)
    - `Stock Market` (purple card, line chart icon, $x \approx 740, y \approx 1500)

---

### Sub-Screen 3A: Stock Market Exchange
- **Screen ID:** `SCR_STOCK_MARKET`
- **Primary Evidence Screenshot:** `docs/screenshots/16_stock_market.png`
- **Verification Status:** **Verified behavior**
- **Navigation Route:** Tapping `Stock Market` from `SCR_INVESTMENTS_MAIN`.
- **Key UI Components:**
  - **Filter Chips:** `Highest dividend`, `Lowest dividend`, `Cheap first`.
  - **Equities List:** 15 parody stocks (Amason, Appli, CicssoSystems, Coca, Fort, Henda, Mikrosolt, Moestrocard, NeFlix, Niiki, NVIDA, Pepsi.co, Tezla, Toyoda, Viza).
  - **Row Data:** Company logo, name, availability tag, sparkline chart (green for positive, red for negative), current price, % 2-hour change.

---

### Sub-Screen 3B: Stock Detail & Trading View
- **Screen ID:** `SCR_STOCK_DETAIL`
- **Primary Evidence Screenshot:** `docs/screenshots/17_stock_detail.png`, `18_buy_shares_modal.png`
- **Verification Status:** **Verified behavior**
- **Navigation Route:** Tapping any stock row in `SCR_STOCK_MARKET`.
- **Key UI Components:**
  - **Summary Metrics:** Current Share Price ($107.15 $\rightarrow$ $110.33$), Yield in the period (1.20% per 3 hours).
  - **Interactive Financial Chart:** Multi-point SVG/mesh line with gradient green fill, dynamic price labels on Y axis, timestamp markers on X axis (24-min intervals).
  - **Execution Action:** Large blue `Buy` button.
  - **Fundamental Data:** Company Capitalization ($80.3B+), Number of Available Shares (750,000,000).

---

### Sub-Screen 3C: Cryptocurrency Exchange
- **Screen ID:** `SCR_CRYPTO_EXCHANGE`
- **Primary Evidence Screenshot:** `docs/screenshots/21_investments_crypto_attempt2.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - Crypto Wallet Card with aggregate holdings value.
  - `Buy` action button.
  - `My coins` holding list and empty-state informational text.

---

### Sub-Screen 3D: Real Estate Hub & Offers
- **Screen ID:** `SCR_REAL_ESTATE_HUB` & `SCR_REAL_ESTATE_OFFERS`
- **Primary Evidence Screenshot:** `docs/screenshots/23_real_estate_attempt2.png`, `24_real_estate_offers.png`, `25_real_estate_offers_scroll.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Rental Income Card:** Aggregate hourly rental yield (Base + Bonus).
  - **Map Launcher:** `My property` card linking to geographic asset visualization.
  - **Offers Catalog:** Full-screen scrollable property feed featuring realistic residential photos, city/country location tags (Stavropol, Fes, Iquitos, Tyumen, Kathmandu), purchase prices ($35K to millions), hourly rental income tags, and direct `Buy` actions.

---

### Tab 4: Luxe & Prestige Tab
- **Screen ID:** `SCR_LUXE_MAIN`
- **Primary Evidence Screenshot:** `docs/screenshots/28_luxe_tab.png`, `29_luxe_scroll.png`, `30_luxe_scroll2.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Showroom Section:** Car Showroom, Aircraft Showroom, Yacht Showroom with `Explore ->` triggers.
  - **Transport Storage Hubs:** Garage (cars), Hangar (planes/helicopters), Harbor (yachts).
  - **Collections Directory:** 10 catalog categories (Rare Motorbikes, Air, Scientific Artifacts, Weapons & Armor, Stamps, Jewels, Exclusive Items, Artworks, Artefacts, Islands).

---

### Sub-Screen 4A: Car Showroom & Configurator
- **Screen ID:** `SCR_CAR_SHOWROOM` & `SCR_CAR_CONFIGURATOR`
- **Primary Evidence Screenshot:** `docs/screenshots/49_car_showroom.png`, `50_car_detail.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - 3D/Isometric render of selected model (Opyl Kadett E, Taraband 501, Fort Focas).
  - **Vehicle Trim Selector:** Basic (1★), Average (+15%, 2★), Maximum (+35%, 3★).
  - **Engine Config Selector:** Standard Engine, Premium Engine (+40%).
  - **Dynamic Price Calculator:** Recalculates total summary cost in real-time before purchase.

---

### Sub-Screen 4B: Private Islands Catalog & Detail
- **Screen ID:** `SCR_ISLANDS_CATALOG` & `SCR_ISLAND_DETAIL`
- **Primary Evidence Screenshot:** `docs/screenshots/54_islands_detail.png`, `55_island_view.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - Hero aerial photography of exotic islands (Caye Chapel, North Aegean, Isla de Sa Ferradura).
  - Pricing ($23.00M to $50.00M+).
  - Detailed geographic and historical lore text.
  - `Buy` CTA button.

---

### Tab 5: Account & Wealth Progression Tab
- **Screen ID:** `SCR_ACCOUNT_MAIN`
- **Primary Evidence Screenshot:** `docs/screenshots/31_account_tab.png`, `32_account_scroll.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - **Language Selector:** Top right `Eng` pill button.
  - **General Expenses Donut Chart:** 7-color procedural segmented ring chart categorizing all historical expenditures: Balance (Blue), Businesses (Red), Share (Magenta), Real Estate (Orange), Vehicles (Teal), Collections (Purple), Cryptocurrencies (Yellow).
  - **Achievements Banner:** Gold metallic ribbon card displaying `0 of 56` progress.
  - **Asset Summary Counters:** Acquired companies, renting real estate, active businesses, collectibles, islands, aircraft, yachts, cars.
  - **Received By (Income Source Ledger):** Granular income tracking by Rent, Businesses, Crypto trading, Tapping, Dividends, Trading.
  - **Wealth Ranking Banner:** Forbes "F" badge card with `See >` trigger.

---

### Sub-Screen 5A: Wealth Ranking Ladder
- **Screen ID:** `SCR_WEALTH_RANKING`
- **Primary Evidence Screenshot:** `docs/screenshots/33_wealth_ranking.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - 5 Progressive status cards with lock badges and net worth thresholds ($50K, $1M, $50M, $500M, $5B+).

---

### Sub-Screen 5B: Achievements Ledger
- **Screen ID:** `SCR_ACHIEVEMENTS`
- **Primary Evidence Screenshot:** `docs/screenshots/37_achievements_screen.png` to `40_achievements_scroll3.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - 56 distinct achievement rows with progress bar header.
  - Unlocked / Locked states, title, requirement description, icon badge.

---

### Modal Screens: In-Game Shop
- **Screen ID:** `SCR_SHOP_MODAL`
- **Primary Evidence Screenshot:** `docs/screenshots/42_shop_screen.png`, `43_shop_scroll.png`, `44_shop_scroll2.png`
- **Verification Status:** **Verified behavior**
- **Key UI Components:**
  - Ticket Currency Counter: `You have: 3 [Ticket icon]`.
  - **Premium VIP Cards:** Gold (+20%), Platinum (+35%), Black (+50%) tap & passive boosts.
  - **Accelerating Items:** Boost Clicker (auto-tap 30 min).
  - **Bundle Packs:** Starter Pack ($25K cash + 5 tickets + Lv.1 factory).
  - **Permanent Utilities:** Remove Forced Ads.
  - **Consumable Packs:** Ad-skip tickets (8, 20, 60 ticket bundles).
