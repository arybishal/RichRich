# RichRich: Housing, Lifestyle & Energy System Specification

**Specification Version:** 1.0 (Milestone 3 Baseline)  
**Author:** Lead Systems Architect & Gameplay Engineer  
**Status:** Implemented & Verified (87 / 87 Passing Automated Tests)

---

## 1. Overview

The **Housing, Lifestyle & Energy System** introduces meaningful living trade-offs into **RichRich**. The player must balance financial conservation (minimizing monthly living expenses) against quality of life, comfort, and physical fatigue recovery.

---

## 2. Housing Accommodations Catalogue

| Tier Level | Housing Name | ID | Monthly Rent | Move-in Deposit | Comfort (0-100) | Overnight Energy Recovery | Landlord Eligibility Requirements |
| :---: | :--- | :--- | :---:| :---:| :---:| :---:| :--- |
| **1** | Shared Room | `housing_shared_room` | **$300.00** | $300.00 | 20 | **+50 energy** | None ($0 min income) |
| **2** | Studio Apartment | `housing_studio` | **$600.00** | $600.00 | 45 | **+70 energy** | None (Default starting residence) |
| **3** | One-Bedroom Flat | `housing_one_bedroom` | **$1,100.00** | $1,100.00 | 70 | **+85 energy** | Salary $\ge \$2,500$ OR Savings $\ge \$5,000$ |
| **4** | Luxury High-Rise | `housing_luxury` | **$2,500.00** | $2,500.00 | 90 | **+100 energy** | Salary $\ge \$6,000$ OR Savings $\ge \$25,000$ |
| **5** | Penthouse Suite | `housing_penthouse` | **$5,500.00** | $5,500.00 | 100 | **+100 energy** | Salary $\ge \$12,000$ OR Savings $\ge \$60,000$ |

---

## 3. Lease Signing & Anti-Duplication Mechanics

1. **Move-in Deposit:**
   - Moving into a new residence deducts a 1-month rental deposit upfront via [`FinancialService.TryRecordExpense()`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialService.cs) under `TransactionCategory.LivingAccommodation`.
2. **Recurring Rent Mutation:**
   - [`HousingService`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/HousingService.cs) mutates the existing `exp_housing` recurring expense in [`LivingExpenseService`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/LivingExpenseService.cs).
3. **Mid-Month Anti-Duplication Rule:**
   - If the player moves mid-month after rent was already paid on Day 1, `exp_housing.LastPaidDate` is preserved.
   - The simulation clock does **NOT** charge a second month's rent for the current month.
   - The new rental rate takes effect strictly on Day 1 of the subsequent calendar month.
4. **Day 1 Move-in Rule:**
   - If moving on Day 1 prior to the scheduled expense check, the newly selected tier's rent is deducted on Day 1.

---

## 4. Energy, Fatigue & Rest Loop

```
┌────────────────────────────────────────────────────────┐
│                   Daily Life Demands                   │
│  - Work shift (40h = -25 energy; 50h = -35 energy)     │
│  - Study shift (Part-Time = -10; Full-Time = -30)      │
└───────────────────────────┬────────────────────────────┘
                            │ depletes
┌───────────────────────────▼────────────────────────────┐
│                      Energy Pool                       │
│  - WellRested: 70 - 100 energy (Optimal performance)   │
│  - Normal:     30 - 69 energy  (Standard baseline)     │
│  - Fatigued:   10 - 29 energy  (Burnout warning)       │
│  - Exhausted:   0 - 9 energy   (Severe exhaustion)     │
└───────────────────────────┬────────────────────────────┘
                            │ replenished by
┌───────────────────────────▼────────────────────────────┐
│                    Rest & Recovery                     │
│  - Take Rest Action: +20 energy during day             │
│  - Sleep to Morning: Housing comfort recovery rate     │
│    (Shared +50 | Studio +70 | 1-Bed +85 | Luxury +100) │
└────────────────────────────────────────────────────────┘
```

### Strategic Life Balancing
- A player in a **Shared Room** ($300/mo) saves $300/mo on rent, but only recovers +50 energy/night. Working 40h (-25) and studying full-time (-30) drains -55 energy/day, causing gradual burnout unless rest days are taken.
- Upgrading to a **One-Bedroom** ($1,100/mo) or **Luxury High-Rise** ($2,500/mo) provides +85 to +100 recovery/night, allowing high-powered executives to work 50 hours and study degrees simultaneously with 100% refreshed energy every morning.

---

## 5. Persistence Schema Representation

In `SaveData.cs` (Schema v3):
```json
{
  "CurrentHousingTierId": "housing_one_bedroom",
  "HousingDepositPaidCents": 110000,
  "CurrentEnergy": 85,
  "StudyPace": "PartTime"
}
```
All housing leases, comfort parameters, and energy reserves are restored across sessions.
