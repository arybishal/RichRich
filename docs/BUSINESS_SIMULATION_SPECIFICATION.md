# RichRich: Commercial Business Simulation Mechanics Specification

## 1. Simulation Architecture Overview

Business simulation in RichRich operates deterministically on top of the central `SimulationClock`:
- **Minute-by-Minute Loop (`OnMinuteTicked`):** Advances active mechanical operations in occupied Auto Repair Workshop service bays.
- **Daily Transition Loop (`OnDayPassed`):** Processes customer retail traffic, evaluates store inventory depletion, advances marketing agency campaign deadlines, and deducts facility operating overhead.
- **Monthly Calendar Settlement (Day 30):** Dispatches staff payroll across all commercial facilities.

---

## 2. Business A: Auto Repair Workshop Simulation

### 2.1 Service Bay Mechanics
- **Capacity:** Starts with 1 physical service bay at Facility Level 1. Upgrades grant +1 bay per facility level (Level 2 = 2 bays, Level 3 = 3 bays).
- **Intake & Bay Allocation:**
  1. The player inspects incoming vehicles in the repair queue.
  2. Selecting an empty bay assigns the vehicle and immediately deducts the replacement parts cost.
  3. The bay status switches to `IsOccupied = true`.

### 2.2 Repair Job Taxonomy & Prerequisites

| Job ID | Vehicle Model | Category | Required Workshop Level | Duration | Parts Cost | Customer Payout | Net Cashflow | Rep Gain |
|---|---|---|---|---|---|---|---|---|
| `job_oil_1` | Sedan Cruiser | Maintenance | Level 1 | 60 min | $25.00 | $90.00 | +$65.00 | +2 |
| `job_brake_1` | Compact City GT | Brakes | Level 1 | 120 min | $60.00 | $220.00 | +$160.00 | +3 |
| `job_diag_1` | SUV Pathfinder | Diagnostics | Level 2 | 180 min | $40.00 | $380.00 | +$340.00 | +4 |
| `job_engine_1` | Executive V8 | Powertrain | Level 3 | 360 min | $250.00 | $1,200.00 | +$950.00 | +6 |

### 2.3 Delivery & Revenue Collection
- When `ElapsedMinutes >= RequiredMinutes`, the job status transitions to `IsCompleted = true`.
- Calling `DeliverFinishedVehicle(bayId)` delivers the car to the customer, credits the payout to `Wallet`, records `BusinessRevenue` in P&L, awards reputation, and clears the service bay for the next vehicle.

---

## 3. Business B: Convenience Store Retail Simulation

### 3.1 Inventory Management & Stock Lines

| Product Line ID | Description | Wholesale Unit Cost | Default Selling Price | Initial Capacity | Default Mark-up |
|---|---|---|---|---|---|
| `inv_beverages` | Chilled Soft Drinks & Energy Drinks | $1.20 | $2.50 | 200 units | 108.3% |
| `inv_groceries` | Packaged Snacks & Quick Groceries | $3.50 | $6.00 | 150 units | 71.4% |
| `inv_necessities` | Household Goods & Toiletries | $4.00 | $8.00 | 100 units | 100.0% |

### 3.2 Pricing Strategy & Dynamic Margins
The player can adjust retail selling prices at any time. Pricing alters gross margins per unit:
$$\text{GrossMarginPercentage} = \frac{\text{SellingPrice} - \text{WholesaleCost}}{\text{SellingPrice}} \times 100\%$$

### 3.3 Daily Foot Traffic & Demand Modeling
Every midnight (`OnDayPassed`), customer foot traffic arrives at the store:
$$\text{CustomerDemand} = 10 + (\text{Level} \times 5) + \lfloor\text{Reputation} / 10\rfloor + (\text{StoreClerks} \times 8)$$

For each product line:
$$\text{UnitsSold} = \min(\text{CustomerDemand}, \text{StockOnHand})$$
$$\text{MissedSales} = \text{CustomerDemand} - \text{UnitsSold}$$
$$\text{StockOnHand} \leftarrow \text{StockOnHand} - \text{UnitsSold}$$
$$\text{DailyRevenue} = \text{UnitsSold} \times \text{SellingPrice}$$
$$\text{DailyCOGS} = \text{UnitsSold} \times \text{WholesaleCost}$$

- **Stockout Penalty:** If `MissedSales > 0`, store reputation is docked due to disappointed walk-in shoppers, reducing future traffic.

---

## 4. Business C: Digital Marketing Agency Simulation

### 4.1 Client Contracts & Scope

| Contract ID | Client | Category | Scope (Labor Hours) | Deadline | Contract Retainer | Skill Prerequisite |
|---|---|---|---|---|---|---|
| `proj_social_1` | Organic Blend Foods | Social Media | 20 hrs | 7 days | $1,500.00 | Communication $\ge 15$ |
| `proj_seo_1` | Urban Retail Outlets | SEO Campaign | 30 hrs | 10 days | $2,400.00 | Analytics $\ge 20$ |
| `proj_web_1` | Horizon Financial Advisory | Web Development | 50 hrs | 14 days | $4,800.00 | Technical $\ge 30$ |
| `proj_growth_1` | OmniGlobal Logistics | Omni-channel Ad Campaign | 80 hrs | 21 days | $9,500.00 | Analytics $\ge 45$ |

### 4.2 Work Hour Contribution
- **Owner Manual Labor:** The player can contribute hours directly via `TryWorkOnProject(hours)`. Each hour consumes 5 lifestyle energy points.
- **Automated Staff Labor:** Each hired agency employee contributes +8 billable hours per day toward active projects automatically.

### 4.3 Deadline Enforcement & Client Relations
- Each calendar day elapsed reduces `DaysRemaining`.
- If `DaysRemaining == 0` before all required hours are logged:
  - Contract transitions to `IsFailed = true`.
  - Retainer payout is forfeited.
  - Agency reputation suffers a severe -10 penalty.
- If all hours are completed before the deadline:
  - Contract transitions to `IsCompleted = true`.
  - Payout is credited to `Wallet` with `TransactionCategory.BusinessRevenue`.
  - Agency reputation increases, qualifying for larger corporate accounts.
