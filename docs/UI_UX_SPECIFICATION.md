# UI / UX Design & Visual Specification

## 1. Design System & Theme Foundations

RichLife and our original implementation adhere to a **Modern FinTech Dark Theme** aesthetic, blending high-end banking interfaces (like Revolut, Apple Card, Robinhood) with gamified idle mechanics.

### 1.1 Color Palette
The color system emphasizes deep obsidian/slate backgrounds with high-contrast functional color accents.

| Role | Color Name | Hex Code | Usage Context | Verification Status |
| :--- | :--- | :--- | :--- | :--- |
| **Canvas Background** | Slate Obsidian | `#121318` | Global application background | **Verified** |
| **Card Surface** | Deep Charcoal | `#1A1C23` | Container background for widgets, lists, cards | **Verified** |
| **Elevated Surface** | Charcoal Hover | `#252833` | Interactive inputs, selected pills, button states | **Verified** |
| **Primary Accent** | Electric Blue | `#2563EB` | Active tab icon, Buy buttons, primary CTAs | **Verified** |
| **Secondary Accent**| Royal Purple | `#7C3AED` | Mergers button, stock market CTA, promo banner | **Verified** |
| **Success / Positive**| Emerald Green| `#10B981` | Positive stock delta, hourly income label, green fill | **Verified** |
| **Danger / Negative**| Crimson Red  | `#EF4444` | Negative stock delta, expense indicators | **Verified** |
| **Reward / Gold**   | Warm Amber   | `#F59E0B` | Achievements card, VIP Gold card, trophy icons | **Verified** |
| **Text Primary**    | Pure White   | `#FFFFFF` | Primary headers, balance numbers, active labels | **Verified** |
| **Text Secondary**  | Muted Silver | `#9CA3AF` | Subtitles, labels, timestamps, inactive tab icons | **Verified** |

### 1.2 Contrast & Accessibility
- All primary text over `#121318` and `#1A1C23` exceeds WCAG AAA standards ($> 7:1$ contrast ratio).
- Interactive touch targets maintain a minimum hitbox of $48 \times 48\text{ dp}$ ($144 \times 144\text{ px}$ on 560 dpi displays).

---

## 2. Typography Scale

The game uses clean geometric sans-serif typefaces (rendered via TextMeshPro with signed distance field / SDF generation for ultra-sharp mobile rendering).

| Token | Size (px) | Weight | Line Height | Usage |
| :--- | :--- | :--- | :--- | :--- |
| **Display Large** | $72\text{ px}$ | Bold (700) | $80\text{ px}$ | Hero credit card balance, major total metrics |
| **Header 1** | $48\text{ px}$ | Semi-Bold (600) | $56\text{ px}$ | Screen titles ("Business", "Investments", "Luxe") |
| **Header 2** | $36\text{ px}$ | Semi-Bold (600) | $44\text{ px}$ | Card section titles ("My stock portfolio", "Shop") |
| **Body Bold** | $28\text{ px}$ | Medium (500) | $36\text{ px}$ | Stock names, business prices, button labels |
| **Body Regular** | $24\text{ px}$ | Regular (400) | $32\text{ px}$ | Secondary labels ("Total income per hour", "per tap") |
| **Caption / Tag** | $18\text{ px}$ | Medium (500) | $24\text{ px}$ | Filter chips, timestamps, stock percentages |

---

## 3. Core Component Specifications

### 3.1 Skewmorphic Credit Card Hero Widget
- **Dimensions:** Full width minus $48\text{ px}$ margin, aspect ratio $\approx 1.586 : 1$ (standard credit card ratio).
- **Corner Radius:** $28\text{ px}$ rounded corners.
- **Visuals:**
  - Dynamic gradient fill (Red/Amber gradient `#D9383A` $\rightarrow$ `#E67E22`).
  - Overlapping dual-circle logo in top left.
  - Card number placeholder: `5282 3456 7890 1289`.
  - Expiry date: `09/28`.
  - Balance label (`Balance`) in small muted font with large primary balance (`$8.00`, `$252.00`).
- *Verification Status:* **Verified behavior** (`docs/screenshots/01_initial_screen.png`).

### 3.2 Financial Charting Components (Powered by XCharts)
- **Sparkline Micro-Charts:**
  - Located in Stock Market equity rows (`docs/screenshots/16_stock_market.png`).
  - Size: $180 \times 60\text{ px}$.
  - Line thickness: $3\text{ px}$.
  - Color dynamically set by last delta: Emerald Green if $+$, Crimson Red if $-$.
  - Anti-aliased spline smoothing.
- **Detailed Interactive Market Chart:**
  - Located in Stock Detail screen (`docs/screenshots/17_stock_detail.png`).
  - Shows 2-hour historical window with 24-minute interval tick marks on X-axis.
  - Linear price scale with dynamic min/max gridlines on Y-axis.
  - Area fill under curve with vertical gradient ($40\%$ alpha green at top to $0\%$ alpha at bottom).
  - Touch scrubbing displays instantaneous price tooltip.
- *Verification Status:* **Verified behavior**.

### 3.3 General Expenses Donut Chart
- **Component ID:** `COMP_DONUT_CHART`
- **Location:** Account Tab (`docs/screenshots/31_account_tab.png`).
- **Structure:**
  - Outer ring with 7 colored segments representing proportional spend:
    1. Balance: `#2563EB` (Blue)
    2. Businesses: `#DC2626` (Red)
    3. Share: `#C026D3` (Magenta)
    4. Real Estate: `#F59E0B` (Orange)
    5. Vehicles: `#059669` (Teal)
    6. Collections: `#8B5CF6` (Light Purple)
    7. Cryptocurrencies: `#EAB308` (Yellow)
  - Center cutout displaying bold "Total" label and cumulative net value.
- *Verification Status:* **Verified behavior**.

### 3.4 Interactive Action Buttons & Pills
- **Pill Filter Chips:**
  - Selected State: White fill `#FFFFFF` with black text `#000000`.
  - Inactive State: Semi-transparent charcoal `#1A1C23` with white text `#FFFFFF` and $1\text{ px}$ border `#333745`.
- **Primary CTA Buttons:**
  - Solid electric blue `#2563EB`, $20\text{ px}$ rounded corners.
  - Press state: Scale down to $96\%$ with darkened tint `#1D4ED8` via DOTween.
- **Secondary CTA Buttons:**
  - Purple pill `#7C3AED` (used for Mergers and special market features).

---

## 4. Layout, Responsive Grids & Safe Areas

### 4.1 Display Aspect Ratio Matrix
Modern Android smartphones exhibit screen aspect ratios ranging from $16:9$ ($1.77$), $19.5:9$ ($2.16$), up to $21:9$ ($2.33$):
- **Reference Device:** $1264 \times 2780$ (approx. $19.8:9$).
- **Layout Strategy:**
  - Top status bar and bottom navigation bar anchor to screen bounds with Android Notch / DisplayCutout Safe Area insets (`Screen.safeArea` in Unity).
  - Main content area sits in an expandable, vertical ScrollRect with elastic momentum scrolling and clamping.
  - In wider aspect ratios (tablets / foldables), the container is constrained to a maximum width of $720\text{ dp}$ centered horizontally to preserve ergonomic tap reachability.

---

## 5. Information Architecture & UX Flow

```
[Main App Launch]
      │
      ▼
[Tab 3: Earnings] (Default Landing)
      │
      ├──> [Free Gift / Rewards Claim Modal]
      ├──> [Shop Modal]
      │
      ├──> [Tab 1: Business]
      │          ├──> [Start Business Catalog]
      │          │          └──> [Business Detail / Purchase Confirmation]
      │          └──> [Business Mergers Catalog]
      │                     └──> [Merger Checklist View]
      │
      ├──> [Tab 2: Investments]
      │          ├──> [Share Sub-Tab]
      │          │          ├──> [My Shares Portfolio]
      │          │          └──> [Stock Market Feed]
      │          │                     └──> [Stock Detail & Chart View]
      │          │                                └──> [Buy / Sell Execution]
      │          ├──> [Crypto Sub-Tab]
      │          │          └──> [Crypto Wallet & Exchange]
      │          └──> [Real Estate Sub-Tab]
      │                     ├──> [My Property Map]
      │                     └──> [Real Estate Offers Catalog]
      │                                └──> [Property Detail & Purchase]
      │
      ├──> [Tab 4: Luxe]
      │          ├──> [Car Showroom] ──> [Vehicle Configurator]
      │          ├──> [Aircraft Showroom] ──> [Aviation Detail]
      │          ├──> [Yacht Showroom] ──> [Vessel Detail]
      │          ├──> [Transport Hubs: Garage, Hangar, Harbor]
      │          └──> [Collections & Islands] ──> [Island Lore & Buy View]
      │
      └──> [Tab 5: Account]
                 ├──> [General Expenses Donut Breakdown]
                 ├──> [Summary & Income Source Ledger]
                 ├──> [Wealth Ranking Milestone Ladder]
                 └──> [56 Achievements Full Registry]
```
