# RichRich: Life & Wealth Simulation Design Specification

## 1. Vision & Core Philosophy

**RichRich** has evolved from an idle clicker prototype into an authentic, decision-driven **life, career, and financial simulation**. The player begins as a young adult with limited starting capital and must build long-term wealth through thoughtful decision-making, professional development, responsible financial management, and smart capital allocation.

### The Core Loop
```
┌────────────────────────────────────────────────────────┐
│                   Simulation Clock                     │
│           (1 real sec = 1 game min; 24 min = 1 day)    │
└───────────────────────────┬────────────────────────────┘
                            │ drives
┌───────────────────────────▼────────────────────────────┐
│                    Daily Life Loop                     │
│  - Attend job shifts / earn scheduled salary           │
│  - Study courses / accumulate skill points             │
│  - Incur living expenses (Rent, Food, Utilities)       │
│  - Service scheduled loan installments                │
└───────────────────────────┬────────────────────────────┘
                            │ generates
┌───────────────────────────▼────────────────────────────┐
│                  Financial Outcomes                    │
│  - Monthly Cash Flow: Income vs. Expenses              │
│  - Net Worth: Assets (Cash/Property) - Liabilities    │
│  - Financial Health: Healthy ──> Stressed ──> Insolvent│
└───────────────────────────┬────────────────────────────┘
                            │ enables
┌───────────────────────────▼────────────────────────────┐
│              Wealth Building & Career Growth           │
│  - Career Promotions (Entry-Level ──> Professional)    │
│  - Starting & Acquiring Businesses                     │
│  - Long-term property & capital accumulation           │
└────────────────────────────────────────────────────────┘
```

---

## 2. Starting Conditions & Difficulty Scaling

Starting money is determined by configuration rather than scattered hardcoded numbers:

| Difficulty Level | Starting Cash | Default Lifestyle | Financial Cushion |
| :--- | :---:| :--- | :--- |
| **Hard** | **$1,000.00** (100,000 cents) | Shared room, public transit | < 1 month living expenses (Immediate job urgency) |
| **Standard** (Default) | **$3,000.00** (300,000 cents) | Modest apartment, basic lifestyle | ~2.2 months living expenses |
| **Comfortable** | **$5,000.00** (500,000 cents) | Comfortable starter cushion | ~3.8 months living expenses |

The player begins without pre-owned businesses, real estate, or equity portfolios. Every asset must be legitimately earned or financed.

---

## 3. Simulation Clock & Time Progression

The world is driven by a central deterministic clock (`SimulationClock.cs`):
- **1 real second = 1 game minute** (Speed = 1.0x).
- **24 real minutes = 1 game day** (1,440 game minutes).
- **12 real hours = 1 game month** (30 game days / 43,200 game minutes).
- **Configurable Speed Controls:** 0x (Pause), 1x (Standard), 5x (Fast-Forward), and instant "Sleep to Morning" (advances to 08:00 AM next day).
- **Chronological Anti-Duplication:** Fast-forwarding or sleeping sequentially iterates through day transitions so scheduled paydays, rent deductions, and loan installments occur in exact order and are never duplicated or skipped.

---

## 4. Career & Employment Pathway

Employment provides the primary foundational cash flow in early and mid-game:

| Job Title | Company | Track | Monthly Salary | Weekly Hours | Requirements | Role Status |
| :--- | :--- | :--- | :---:| :---:| :--- | :--- |
| **Retail Assistant** | QuickMart | Retail | $1,600.00 | 40 hrs | None (Entry level) | **Implemented** |
| **Delivery Courier** | SwiftDrop | General | $1,800.00 | 40 hrs | None (Entry level) | **Implemented** |
| **Office Assistant** | Beacon Corp | Business | $1,900.00 | 40 hrs | None (Entry level) | **Implemented** |
| **Sales Assistant** | Metro Direct | Retail | $2,000.00 | 40 hrs | None (Entry level) | **Implemented** |
| **Senior Retail Assistant** | QuickMart | Retail | $2,300.00 | 40 hrs | 6 mo exp, Sales $\ge$ 15, Comm $\ge$ 10 | **Implemented** |
| **Administrative Officer** | Beacon Corp | Business | $2,700.00 | 40 hrs | 6 mo exp, Comm $\ge$ 20, Analytics $\ge$ 10 | **Implemented** |
| **Junior IT Helpdesk**| Apex Solutions | Tech | $2,800.00 | 40 hrs | IT Support Cert + Technical $\ge$ 20 | **Implemented** |
| **Shift Supervisor** | QuickMart | Retail | $3,200.00 | 42 hrs | 12 mo exp, 6 mo tenure, Management $\ge$ 15 | **Implemented** |
| **IT Support Specialist** | Apex Solutions | Tech | $3,600.00 | 40 hrs | 6 mo exp, Technical $\ge$ 35, Comm $\ge$ 15 | **Implemented** |
| **Business Operations Executive** | Vanguard | Business | $4,200.00 | 42 hrs | 12 mo exp, Qual: `qual_business_diploma` | **Implemented** |
| **Store Manager** | QuickMart | Retail | $4,600.00 | 45 hrs | 24 mo exp, Qual: `qual_retail_sales`, Mgmt $\ge$ 30 | **Implemented** |
| **Junior Software Developer** | Nova Byte | Tech | $4,800.00 | 40 hrs | 6 mo exp, Technical $\ge$ 50, Analytics $\ge$ 25 | **Implemented** |
| **Operations Manager** | Vanguard | Business | $6,500.00 | 45 hrs | 24 mo exp, 6 mo tenure, Management $\ge$ 45 | **Implemented** |
| **Software Developer** | Nova Byte | Tech | $6,800.00 | 40 hrs | 18 mo exp, 6 mo tenure, Tech $\ge$ 70 | **Implemented** |
| **Senior Developer** | Nova Byte | Tech | $9,200.00 | 40 hrs | 36 mo exp, 6 mo tenure, Tech $\ge$ 85 | **Implemented** |
| **Enterprise Consultant**| Global Advisory | Business | $10,500.00 | 50 hrs | 36 mo exp, Qual: `qual_business_diploma` | **Implemented** |
| **Technology Manager** | Nova Byte | Tech | $13,500.00 | 45 hrs | 48 mo exp, Qual: `qual_cs_degree`, Mgmt $\ge$ 50 | **Implemented** |

Salaries are credited automatically via `FinancialService` on each job's designated payday (Day 30 of each month).

---

## 5. Education & Skill Progression

Players cannot advance to lucrative corporate, engineering, or executive roles without developing skills and earning formal credentials:

| Course Title | Tuition | Duration | Target Skills | Granted Credential | Status |
| :--- | :---:| :---:| :--- | :--- | :--- |
| **Digital Skills Basics** | Free ($0) | 20 study hrs | Technical (+10) | `qual_digital_basics` | **Implemented** |
| **Sales & Engagement Bootcamp** | $250.00 | 30 study hrs | Communication (+15), Sales (+20) | `qual_retail_sales` | **Implemented** |
| **IT Support Certificate** | $500.00 | 60 study hrs | Technical (+25), Communication (+10) | `qual_it_support_cert` | **Implemented** |
| **Diploma of Business Admin** | $3,500.00 | 180 study hrs | Management (+35), Analytics (+25) | `qual_business_diploma` | **Implemented** |
| **Bachelor of Computer Science**| $12,000.00 | 480 study hrs | Technical (+70), Analytics (+50) | `qual_cs_degree` | **Implemented** |

Students study 2 hours daily (Part-Time) or 6 hours daily (Full-Time) while the clock ticks. Tuition is deducted upfront via `FinancialService`.

---

## 6. Living Expenses, Housing & Lifestyle

Living accommodations are upgradeable across 5 distinct tiers:
- **Shared Room:** $300.00 / month (Comfort 20, +50 energy recovery / night)
- **Studio Apartment:** $600.00 / month (Comfort 45, +70 energy recovery / night - Default)
- **One-Bedroom Apartment:** $1,100.00 / month (Comfort 70, +85 energy recovery / night)
- **Luxury High-Rise:** $2,500.00 / month (Comfort 90, +100 energy recovery / night)
- **Penthouse Suite:** $5,500.00 / month (Comfort 100, +100 energy recovery / night)

Standard recurring survival expenses (in Studio):
- **Apartment Rent:** $600.00 / month
- **Groceries & Food:** $300.00 / month
- **Public Transit Pass:** $100.00 / month
- **Utilities (Power & Water):** $120.00 / month
- **Mobile & Internet Plan:** $50.00 / month
- **Personal & Household Items:** $150.00 / month
- **Total Standard Living Cost:** **$1,320.00 / month** ($44.00 / day)

Players with jobs earning $\ge \$1,600.00/\text{month}$ maintain a positive cash flow cushion. Unemployment causes cash depletion at -$1,320.00/month.

---

## 7. Responsible Borrowing & Credit Engine

Borrowing provides capital leverage but carries strict legal obligations:
- **Available Products:**
  - *Starter Personal Loan:* $5,000 principal, 12% APR (1% monthly), 12-month term ($444.24/month installment).
  - *Capital Growth Loan:* $10,000 principal, 12% APR, 12-month term ($888.49/month installment).
- **Double-Entry Discipline:**
  - Loan disbursement increases Cash (+$5,000) and Liabilities (+$5,000) simultaneously. **Net worth change = $0.00.**
  - Monthly installments split into principal reduction (decreases liabilities) and interest payment (financial expense).
- **Underwriting Rules:**
  - Minimum Credit Score: 580.
  - Debt-to-Income (DTI) limit: Monthly debt payments cannot exceed 50% of monthly salary.
  - Maximum 2 concurrent active loans.
- **Early Repayments:** Permitted anytime with zero early prepayment penalties.
- **Delinquency Penalties:** Missed payments trigger a $25.00 late fee, record overdue liabilities, and reduce credit score by 25 points.

---

## 8. Financial Health & Bankruptcy States

The player's status is evaluated across 6 formal states:
1. **Healthy:** Positive monthly cash flow, $\ge$ 1 month emergency cash cushion, 0 overdue liabilities.
2. **Financially Stressed:** Negative monthly cash flow or cash cushion < 1 month expenses, but all debts currently up-to-date.
3. **Cash Crisis:** Cash balance is insufficient to service immediate upcoming weekly obligations or 1 active missed payment.
4. **Insolvent:** Total liabilities exceed total assets (negative net worth) with ongoing operational deficits.
5. **Bankrupt:** Delinquent default with 2+ missed payments, negative net worth, and insufficient cash to cure delinquency.
6. **Restructuring:** Formal debt repayment moratorium and workout plan negotiated under financial distress.

---

## 9. Implementation Status Matrix

| Subsystem | Feature | Status | Notes |
| :--- | :--- | :--- | :--- |
| **Economy** | Integer Minor-Unit Math (`Currency.cs`) | **Implemented** | Exact `long` cents arithmetic with underflow checks |
| **Economy** | Configurable Starting Difficulty (`GameDifficulty.cs`) | **Implemented** | Hard ($1K), Standard ($3K), Comfortable ($5K) |
| **Clock** | Deterministic Calendar Clock (`SimulationClock.cs`) | **Implemented** | 1 real sec = 1 game min; daily/monthly events; anti-duplication |
| **Finance** | Double-Entry Financial Service (`FinancialService.cs`) | **Implemented** | All balance changes gated through ledger and balance sheet |
| **Finance** | Full Transaction Ledger (`FinancialLedger.cs`) | **Implemented** | Auditable history, income/expense aggregations |
| **Finance** | Living Expenses Engine (`LivingExpenseService.cs`) | **Implemented** | 6 standard categories; $1,320/mo baseline; scheduled deductions |
| **Career** | Career Progression Engine (`CareerService.cs`) | **Implemented** | Retail, Courier, IT Helpdesk; requirements & scheduled paydays |
| **Education** | Education & Skills Engine (`EducationService.cs`) | **Implemented** | Course catalog, tuition, daily study, qualification awards |
| **Loans** | Amortized Debt & Credit Engine (`LoanService.cs`) | **Implemented** | $5K/$10K loans, 12% APR, eligibility limits, early payoff |
| **Health** | 6-State Health Evaluator (`FinancialHealthService.cs`) | **Implemented** | Healthy $\rightarrow$ Stressed $\rightarrow$ Crisis $\rightarrow$ Insolvent $\rightarrow$ Bankrupt |
| **UI** | Life Simulation HUD (`LifeSimulationHUDController.cs`)| **Implemented** | Replaces tap button with Cash, Net Worth, Date, Cash Flow, Status |
| **UI** | Unity HUD View (`MainHUDView.cs`) | **Implemented** | Binds Life HUD to Unity UI Text/Buttons |
| **Career** | Expanded Multi-Tier Career Ladders | **Implemented** | Retail (4 tiers), Tech (6 tiers), Business (5 tiers) with promotions |
| **Education** | University Degrees & Online Modules | **Implemented** | Business Admin Diploma, Computer Science Degree, study pacing |
| **Housing** | Housing Accommodations System | **Implemented** | 5 tiers, move-in deposits, landlord verification, anti-duplication rent |
| **Lifestyle** | Energy & Fatigue Dynamics | **Implemented** | Energy pool (0-100), work/study drain, rest, housing sleep recovery |
| **Business** | Commercial Business Framework | **Implemented** | Auto Repair, Convenience Store, Digital Agency with P&L and valuation |
| **Staffing** | Human Resources & Staffing | **Implemented** | 6 employee roles, candidate pool recruitment, Day 30 monthly payroll |
| **Credit** | Commercial Business Loans | **Implemented** | $15K starter and $35K expansion loans, underwriting on business profit |
| **Property** | Real Estate Investment & Portfolios | *Designed* | Planned for Milestone 5 |
| **Testing** | Android Physical Hardware Deployment | **Deferred / Blocked** | No physical phone attached to ADB; headless .NET 8 CLI harness verified |
