# Animation & Visual Effects Specification

## 1. Animation Engine & Core Stack

Teardown of `ScriptingAssemblies.json` and string literal tables verified that the reference game employs **DOTween (`DOTween.dll`, `DG.Tweening`)** as its foundational tweening engine for all UI interactions, number counting, scale bounces, and screen transitions.

For our clean-room implementation, we specify identical tween curves and timings to achieve indistinguishable tactile polish.

---

## 2. Interactive Tap Feedback & Floating Numbers

### 2.1 Expanding Radial Ripple
- **Trigger:** `PointerDown` inside the center tap region.
- **Visual:** A circular ring outline expands outward from the exact $(x, y)$ touch coordinate.
- **Parameters:**
  - Initial Radius: $20\text{ px}$, Alpha: $0.6$
  - Final Radius: $180\text{ px}$, Alpha: $0.0$
  - Duration: $350\text{ ms}$
  - Easing: `Ease.OutQuad`
- *Verification Status:* **Verified behavior** (`docs/screenshots/02_tap_interaction.png`).

### 2.2 Floating Currency Text
- **Trigger:** Every successful tap.
- **Visual:** Text object (e.g., `+$4.00`) spawns slightly above the tap location or below the balance widget.
- **Motion:**
  - Upward translation: $\Delta y = +120\text{ px}$ over $600\text{ ms}$
  - Scale punch: Starts at $1.0 \rightarrow 1.25 \rightarrow 1.0$ (`DOPunchScale(0.25, 0.2)`)
  - Alpha fade: 1.0 for first $300\text{ ms}$, then linear fade to 0.0 over remaining $300\text{ ms}$
  - Easing: `Ease.OutCubic`
- *Verification Status:* **Verified behavior**.

### 2.3 Fingerprint Icon Punch Scale
- **Visual:** When tapping the central fingerprint graphic:
  - Punch scale: `transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.15f, 10, 1)`
  - Creates an elastic, springy tactile response simulating a physical button depress.
- *Verification Status:* **Verified behavior**.

---

## 3. Financial Metrics & Rolling Number Counters

To make balance increases feel substantial, balance values do not snap instantly between numbers; they roll smoothly via animated tickers.

### 3.1 Currency Roll-Up Tween
- **Implementation:**
  ```csharp
  DOTween.To(() => displayedBalance, x => {
      displayedBalance = x;
      balanceText.text = FormatCurrency(x);
  }, targetBalance, 0.4f).SetEase(Ease.OutQuad);
  ```
- **Duration:** Dynamic scale:
  - Small deltas ($<\$100$): $250\text{ ms}$
  - Large rewards (e.g., $\$32.00$ gift or $\$25,000$ purchase): $600\text{ ms}$
- **Formatting:** Dynamic abbreviation rules:
  - $< \$1,000,000$: `$###,###.00`
  - $\ge \$1,000,000$: `$##.##M`
  - $\ge \$1,000,000,000$: `$##.##B`
  - $\ge \$1,000,000,000,000$: `$##.##T`
- *Verification Status:* **Verified behavior**.

---

## 4. Financial Chart Animations (XCharts Engine)

### 4.1 Stock Sparkline Live Interpolation
- When a new price point is added to the 2-hour window buffer, the line does not jump abruptly.
- The latest line segment interpolates to the new price coordinate over $800\text{ ms}$ using `Ease.InOutSine`.
- The fill color under the line smoothly transitions:
  - Green gradient if $\Delta P \ge 0$
  - Red gradient if $\Delta P < 0$
- *Verification Status:* **Verified behavior** (`docs/screenshots/16_stock_market.png`).

### 4.2 Detailed Chart Scrubbing & Tooltip
- Dragging a finger across the chart area displays a vertical crosshair line that snaps to the nearest historical data point.
- Tooltip floats above the finger showing exact price and timestamp with a $150\text{ ms}$ spring pop-in (`Ease.OutBack`).
- *Verification Status:* **Verified behavior**.

---

## 5. Screen Transitions & Modal Presentations

### 5.1 Tab Switching (Bottom Navigation Bar)
- **Transition Style:** Instant view switch with crossfade on content container.
- **Active Tab Pill:**
  - The blue circular highlight expands/translates to the new tab position:
    - Translation duration: $250\text{ ms}$
    - Easing: `Ease.OutCubic`
  - Inactive icon tints from `#9CA3AF` to `#2563EB` (Active) over $180\text{ ms}$.
- *Verification Status:* **Verified behavior**.

### 5.2 Modal Catalog Presentation (Slide-Up / Push)
- Sub-screens (such as `SCR_BUSINESS_CATALOG`, `SCR_STOCK_MARKET`, `SCR_SHOP_MODAL`, `SCR_ACHIEVEMENTS`):
  - **Enter Animation:** Slides up from bottom of screen ($\Delta y = -\text{ScreenHeight} \rightarrow 0$) over $320\text{ ms}$ using `Ease.OutCubic`.
  - **Exit Animation:** Slides down off-screen ($\Delta y = 0 \rightarrow -\text{ScreenHeight}$) over $280\text{ ms}$ using `Ease.InCubic` upon pressing the Back arrow.
- *Verification Status:* **Verified behavior**.

### 5.3 Button Press Feedback
- All buttons (pill chips, primary action buttons, cards) have an interactive micro-interaction:
  - On `PointerDown`: Scale to $0.96$ over $100\text{ ms}$
  - On `PointerUp`: Scale back to $1.00$ over $150\text{ ms}$ with slight overshoot (`Ease.OutBack`, overshoot $1.2$)
- *Verification Status:* **Verified behavior**.

---

## 6. Haptic Feedback Integration

To complement visual animations:
- **Tap click:** Ultra-light haptic tick (`HapticFeedbackType.KeyboardTap` or Android `VibrationEffect.createPredefined(EFFECT_TICK)`).
- **Level Up / Milestone:** Medium double pulse (`EFFECT_DOUBLE_CLICK`).
- **Major Purchase (Car/Island):** Heavy haptic pulse (`EFFECT_HEAVY_CLICK`).
- *Verification Status:* **Inferred behavior** (standard mobile haptic companion patterns).
