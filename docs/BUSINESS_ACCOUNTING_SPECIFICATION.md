# RichRich: Business Accounting, P&L & Commercial Finance Specification

## 1. Accounting Philosophy & Principles

In RichRich, business revenue is **never conflated with business profit**. Operating an enterprise involves explicit cost structures, capital accounting, and debt obligations:

1. **Revenue $\neq$ Net Profit:** Gross cash received from customer repair payments, retail counter sales, or agency client retainers is booked as `GrossRevenue`.
2. **Cost of Goods Sold (COGS):** Wholesale stock acquisition costs or vehicle replacement parts directly associated with units sold are categorized as `COGS`.
3. **Operating Expenses (OPEX):** Commercial lease rent, utilities, insurance, and software licenses are deducted on a deterministic daily schedule.
4. **Labor Overhead (Payroll):** Recurring monthly staff compensation is accrued and deducted on Day 30 of each calendar month.
5. **Gross Profit vs. Net Operating Income:**
   $$\text{GrossProfit} = \text{GrossRevenue} - \text{COGS}$$
   $$\text{NetProfit} = \text{GrossProfit} - (\text{OperatingExpenses} + \text{PayrollExpenses})$$
6. **Balance Sheet Integration:** Enterprise facilities and equipment are accounted for as `NonCashAssets`, fluctuating with business level, physical condition, and reputation.

---

## 2. Income Statement Structure (`BusinessPAndL`)

Each owned business maintains an isolated, auditable P&L statement:

```
[CONSOLIDATED COMMERCIAL INCOME STATEMENT]
-------------------------------------------------------------
Gross Commercial Revenue                      $ XX,XXX.XX
Less: Cost of Goods Sold (COGS)              ($  X,XXX.XX)
-------------------------------------------------------------
GROSS OPERATING PROFIT                        $ XX,XXX.XX
  Gross Margin %                              [XX.X%]

Less Operating Overhead (OPEX):
  Facility Lease & Fixed Overhead            ($    XXX.XX)
  Staff Compensation & Payroll               ($  X,XXX.XX)
-------------------------------------------------------------
NET OPERATING PROFIT (EBIT)                   $  X,XXX.XX
-------------------------------------------------------------
```

### 2.1 P&L Metrics by Industry Sector

| Sector | Revenue Sources | Cost of Goods Sold (COGS) | Operating Overhead | Payroll Structure |
|---|---|---|---|---|
| **Automotive** (`auto_repair`) | Customer repair labor & diagnostics fees | Replacement parts (fluids, brake pads, diagnostic kits, overhaul components) | Workshop rent, compressor electricity, waste disposal ($35.00/day) | Apprentice & Senior Mechanics |
| **Retail** (`convenience_store`) | OTC counter sales (drinks, snacks, daily goods) | Wholesale purchase price per unit sold | Retail space lease, commercial refrigeration, POS license ($45.00/day) | Store Clerks & Inventory Managers |
| **Digital Services** (`digital_agency`) | Fixed-fee enterprise client contracts | None ($0.00 COGS) | Cloud hosting, creative software suite, office space ($85.00/day) | Junior Marketers & Senior Strategists |

---

## 3. Commercial Credit & Business Loans

Entrepreneurs can leverage debt to finance facility purchases, acquire inventory in bulk, or bridge payroll during lean cycles.

### 3.1 Commercial Loan Catalog

| Product ID | Product Name | Principal | APR | Term | Min Monthly Income | Eligibility Criteria |
|---|---|---|---|---|---|---|
| `loan_business_starter` | Commercial Equipment & Starter Loan | $15,000.00 | 10.0% | 24 mo | $2,000.00 | Requires $\ge 1$ active owned commercial business |
| `loan_business_expansion` | Commercial Enterprise Expansion Loan | $35,000.00 | 9.0% | 36 mo | $3,500.00 | Requires $\ge 1$ business operating at Facility Level 2+ |

### 3.2 Underwriting & Solvency Assessment
- **Dual Income Evaluation:** Unlike consumer loans which evaluate only employment salary, commercial loan underwriting evaluates:
  $$\text{QualifyingMonthlyIncome} = \text{EmploymentSalary} + \max(0, \text{CommercialNetMonthlyProfit})$$
  This ensures players who quit their jobs to become full-time entrepreneurs can still access credit based on business performance.
- **Debt-to-Income (DTI) Limit:** Total active installments plus the prospective loan installment cannot exceed 50% of qualifying monthly income.
- **Credit Score Impact:** On-time loan servicing grants +3 credit points/month and +10 on debt payoff. Delinquencies incur severe late fees and -25 credit score penalties.

---

## 4. Personal Balance Sheet Linkage

All commercial transactions directly sync with the player's personal financial balance sheet:

$$\text{TotalAssets} = \text{LiquidCash} + \text{NonCashAssets} (\text{Business Portfolio Valuation})$$
$$\text{TotalLiabilities} = \text{PersonalLoanLiabilities} + \text{CommercialLoanLiabilities} + \text{OverdueDebt}$$
$$\text{NetWorth} = \text{TotalAssets} - \text{TotalLiabilities}$$

When an entrepreneur reinvests profits into facility upgrades or inventory, cash is exchanged for asset valuation without arbitrary net worth distortion.
