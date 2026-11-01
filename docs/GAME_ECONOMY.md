# Game Economy & Numerical Balancing Specification

## 1. Life & Wealth Economic Foundation

RichRich operates on an authentic, decision-driven life simulation economy. Tap-to-earn idle clicker mechanics have been completely removed as a core foundation. Wealth is generated through employment, skill certifications, responsible credit management, business ownership, and long-term capital investments.

### 1.1 Configurable Starting Conditions
The player begins under configurable starting conditions balanced for a realistic progression curve:

| Difficulty | Starting Cash | Equivalent Cents | Initial Runway |
| :--- | :---:| :---:| :--- |
| **Hard** | **$1,000.00** | 100,000 cents | 0.75 months living expenses |
| **Standard** (Default) | **$3,000.00** | 300,000 cents | 2.25 months living expenses |
| **Comfortable** | **$5,000.00** | 500,000 cents | 3.80 months living expenses |

---

## 2. Income & Cash Flow Structure

### 2.1 Employment Salary Incomes (Entry Level)
Salaries are paid automatically via `FinancialService` on Day 30 of each month:
- **Retail Assistant (QuickMart):** $1,600.00 / month ($10.00 / hour equivalent). Entry level, 0 prerequisites.
- **Delivery Courier (SwiftDrop Logistics):** $1,800.00 / month. Entry level, 0 prerequisites.
- **Junior IT Helpdesk (Apex Solutions):** $2,800.00 / month. Requires `qual_it_support_cert` and Technical skill $\ge 20$.

### 2.2 Recurring Baseline Living Expenses
Living expenses represent recurring essential costs deducted on Day 1 of each month:
- **Apartment Rent:** $600.00 / month
- **Groceries & Food:** $300.00 / month
- **Public Transit Pass:** $100.00 / month
- **Utilities (Power & Water):** $120.00 / month
- **Mobile & Internet:** $50.00 / month
- **Personal & Household Items:** $150.00 / month
- **Total Standard Monthly Baseline:** **$1,320.00 / month** ($44.00 / day)

### 2.3 Net Monthly Cash Flow
$$\text{Net Monthly Cash Flow} = \sum \text{Salaries} - \left( \sum \text{Living Expenses} + \sum \text{Loan Installments} \right)$$
- An employed Retail Assistant earns $\$1,600 - \$1,320 = +\$280.00 / \text{month}$ net surplus.
- An employed Junior IT Technician earns $\$2,800 - \$1,320 = +\$1,480.00 / \text{month}$ net surplus.
- An unemployed player incurs a $-\$1,320.00 / \text{month}$ operational deficit, depleting cash cushion.

### 2.4 Deactivation of Tap-to-Earn
- Screen tapping does not generate cash.
- Tap multipliers and auto-clicker boosters are disabled in the main life simulation HUD.
- Repetitive clicking has been replaced by scheduled simulation calendar progression (1 real second = 1 game minute).

---

## 3. Passive Income Engine (Hourly Yields)

The player's aggregate passive income per hour is the sum of four discrete streams:
$$\text{Total Passive Income} = I_{\text{business}} + I_{\text{rental}} + I_{\text{dividends}} + I_{\text{crypto\_yield}}$$

### 3.1 Business Yields & ROI Modeling
Each business delivers an hourly cash yield based on its initial investment and stage level:

| Business | Purchase Price ($) | Est. Base Income / Hr ($) | Est. Break-Even Time (Hrs) | Verification Status |
| :--- | :--- | :--- | :--- | :--- |
| **Store (Retail)** | $5,499.00 | $120.00 | ~45.8 | **Inferred** |
| **Taxi Company** | $14,990.00 | $310.00 | ~48.3 | **Inferred** |
| **Beauty Salon** | $29,990.00 | $580.00 | ~51.7 | **Inferred** |
| **Delivery Company**| $29,990.00 | $600.00 | ~50.0 | **Inferred** |
| **Manufacturing Plant**| $35,000.00 | $680.00 | ~51.5 | **Inferred** |
| **Building Contractor**| $50,000.00 | $950.00 | ~52.6 | **Inferred** |
| **Autorepair Shop**| $55,000.00 | $1,020.00 | ~53.9 | **Inferred** |
| **Vet-Clinic** | $100,000.00 | $1,800.00 | ~55.5 | **Inferred** |
| **Tech Company** | $355,000.00 | $6,200.00 | ~57.2 | **Inferred** |
| **Hospital** | $500,000.00 | $8,500.00 | ~58.8 | **Inferred** |
| **Film Studio** | $1,240,000.00 | $20,000.00 | ~62.0 | **Inferred** |
| **Insurance Company**| $9,500,000.00 | $145,000.00 | ~65.5 | **Inferred** |
| **MMA Organization**| $25,000,000.00 | $360,000.00 | ~69.4 | **Inferred** |
| **Energy / Oil & Gas**| $990,000,000.00 | $13,500,000.00 | ~73.3 | **Inferred** |

*Economic Insight:* As capital requirements increase exponentially, break-even periods gently widen from ~45 hours to ~73 hours, preventing hyper-inflation while maintaining meaningful long-term progression goals.

### 3.2 Real Estate Rental Yields (Directly Verified)
From the live in-game property market (`docs/screenshots/24_real_estate_offers.png`, `25_real_estate_offers_scroll.png`):

| Location | Purchase Price ($) | Hourly Rent ($) | Annualized Cap Rate / Yield | Verification Status |
| :--- | :--- | :--- | :--- | :--- |
| **Stavropol, Russia** | $35,000.00 | $564.00 | 1.61% per hour (~62 hrs break-even) | **Verified** |
| **Fes, Morocco** | $43,000.00 | $690.00 | 1.60% per hour (~62.3 hrs break-even) | **Verified** |
| **Iquitos, Peru** | $62,000.00 | $998.00 | 1.61% per hour (~62.1 hrs break-even) | **Verified** |
| **Tyumen, Russia** | $69,000.00 | $1,110.00 | 1.60% per hour (~62.2 hrs break-even) | **Verified** |

*Economic Insight:* Real estate yields exhibit an exceptionally stable, linear cap rate of **$\approx 1.60\% - 1.61\%$ per hour** (~62 hours to 100% ROI), providing players a predictable capital park for mid-game idle earnings.

### 3.3 Stock Dividend Yields (Directly Verified)
- Dividends pay out on a fixed **3-hour cycle** (`docs/screenshots/17_stock_detail.png`).
- Amason baseline dividend yield: **1.20% per 3-hour period** ($\approx 0.40\%$ per hour).
- Payout formula:
  $$\text{Dividend Payout} = \text{Shares Owned} \times \text{Current Share Price} \times \text{Dividend Rate}$$
- *Verification Status:* **Verified behavior**.

---

## 4. Financial Markets: Drift & Volatility Simulation

The in-game simulated market operates entirely locally without external financial APIs, using a bounded **Geometric Brownian Motion (GBM) with Mean-Reverting Ornstein-Uhlenbeck Drift**:

$$S_{t + \Delta t} = S_t + \theta (\mu - S_t) \Delta t + \sigma S_t \sqrt{\Delta t} \, Z_t$$

Where:
- $S_t$: Current share/coin price.
- $\mu$: Anchor fair value (derived from baseline market cap).
- $\theta$: Mean-reversion speed ($\approx 0.05$ to keep assets from drifting to zero or infinity).
- $\sigma$: Volatility parameter:
  - Equities (Stocks): $\sigma \approx 0.02 - 0.05$ (conservative swings, $\pm 2\% - 9\%$).
  - Cryptocurrencies: $\sigma \approx 0.10 - 0.25$ (aggressive swings, $\pm 10\% - 40\%$).
- $Z_t \sim \mathcal{N}(0, 1)$: Standard normal random variable.

Every tick (1–2 minutes), prices calculate and update the sliding 2-hour historical buffer (`PricesForTwoHour`), powering real-time graph visualization.
- *Verification Status:* **Inferred behavior** (empirically supported by observed price variations in `StocksData.json`).

---

## 5. Offline Earnings & Catch-Up Logic

When the player closes the application, offline idle progression is calculated upon the next launch:

### 5.1 Calculation Logic
1. Upon application pause/exit, the game records `SessionData.json: "LastExitUnixTimestamp"`.
2. Upon next launch, the game computes:
   $$\Delta t = \min(t_{\text{current}} - t_{\text{last\_exit}}, \text{MaxOfflineHours})$$
3. By default, `MaxOfflineIncomeHours` is capped at **2 to 3 hours** to encourage regular retention.
4. Total offline cash generated:
   $$\text{Cash}_{\text{offline}} = \Delta t \times (\text{Hourly Business Income} + \text{Hourly Rental Income})$$
5. The player is presented with a "Welcome Back" modal displaying the claimed offline sum.
- *Verification Status:* **Inferred behavior** (confirmed via IL2CPP properties `get_IsOfflineIncomeLimitEnabled`, `get_MaxOfflineIncomeHours`, and `Items.PassiveIncome:ProcessOfflineReturn` logs).

---

## 6. In-Game Store & Monetization Balancing

Observed from live shop screens (`docs/screenshots/42_shop_screen.png` to `44_shop_scroll2.png`):

### 6.1 Permanent VIP Cards (Non-Stacking)
- **Gold Card:** +20% Tap Bonus, +20% Passive Income (15.99 AED ~ $4.35 USD)
- **Platinum Card:** +35% Tap Bonus, +35% Passive Income (27.99 AED ~ $7.60 USD)
- **Black Card:** +50% Tap Bonus, +50% Passive Income (43.99 AED ~ $11.95 USD)
- *Balancing Rule:* Bonuses do **not** stack. Only the highest active tier applies.

### 6.2 Accelerating Utilities & Bundles
- **Boost Clicker:** Auto-clicker for 30 minutes (11.99 AED).
- **Starter Pack:** $25,000 instant cash, 5 ad-skip tickets, Lv.1 Factory (7.99 AED).
- **No More Ads:** Permanently removes forced interstitial ads (39.99 AED).
- **Ad-Skip Tickets:** Consumable instant-skips:
  - 8 Tickets: 7.99 AED
  - 20 Tickets: 19.99 AED
  - 60 Tickets: 47.99 AED
- *Verification Status:* **Verified behavior**.

### 6.3 Free Ad Rewards & Gifts
- **Free Cash Gift:** Timer-based top-right gift icon grants an instant free cash injection ($32.00 at Level 1).
- **Multiplier Ad Burst:** 30-second 6x tap multiplier via rewarded video.
- **Extra Slot Ad:** Watch video to unlock an extra business slot beyond the initial 9.
