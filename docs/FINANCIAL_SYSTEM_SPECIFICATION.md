# RichRich: Financial System Technical Specification

## 1. Monetary Data Integrity & Minor-Unit Currency

To eliminate IEEE 754 floating-point drift, rounding anomalies, and fractional cent corruption, all monetary values in RichRich are strictly modeled as integer minor units (`long` cents) within the [`Currency`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Models/Currency.cs) struct.

### 1.1 Structural Definition
```csharp
public readonly struct Currency : IEquatable<Currency>, IComparable<Currency>
{
    public long MinorUnits { get; } // 100 minor units = $1.00 USD
    public double ToDollars() => (double)MinorUnits / 100.0;
}
```

### 1.2 Mathematical Safeguards
1. **Underflow Protection:** Subtraction that would result in a negative balance throws `InvalidOperationException`. Negative cash balances in wallet are illegal; debt obligations are tracked explicitly as liabilities.
2. **Deterministic Rounding:** Multiplication by interest rates or fractional durations uses `Math.Round(dollars * 100.0, MidpointRounding.AwayFromZero)`.
3. **Display Formatting:**
   - Values $< \$10,000$: `"$1,250.50"`
   - Values $\ge \$1,000,000$: `"$1.25M"`
   - Values $\ge \$1,000,000,000$: `"$4.88B"`

---

## 2. Double-Entry Accounting Model & Ledger

Every money movement in the game is processed through the [`FinancialService`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialService.cs) and recorded in the auditable [`FinancialLedger`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialLedger.cs).

### 2.1 Transaction Record
```csharp
public class FinancialTransaction
{
    public string Id { get; }                  // Unique GUID / transaction identifier
    public Currency Amount { get; }            // Positive integer minor units
    public TransactionType Type { get; }       // Income, Expense, LiabilityDraw, LiabilityRepay, etc.
    public TransactionCategory Category { get; }// EmploymentSalary, LivingAccommodation, etc.
    public GameDateTime Date { get; }          // Simulation timestamp
    public string Description { get; }         // Human-readable ledger entry
    public string RelatedEntityId { get; }     // Optional foreign key (jobId, loanId, courseId)
}
```

### 2.2 Balance Sheet Equations
- **Total Assets:**
  $$\text{Total Assets} = \text{Cash Balance} + \text{Non-Cash Assets}$$
- **Total Liabilities:**
  $$\text{Total Liabilities} = \text{Remaining Loan Principals} + \text{Overdue Liabilities}$$
- **Net Worth:**
  $$\text{Net Worth} = \text{Total Assets} - \text{Total Liabilities}$$

### 2.3 Strict Balance Sheet Rules
1. **Loan Disbursement:**
   $$\Delta \text{Cash} = +P, \quad \Delta \text{Loan Liabilities} = +P \implies \Delta \text{Net Worth} = \$0.00$$
   *Taking a loan never generates income or increases net worth.*
2. **Loan Principal Repayment:**
   $$\Delta \text{Cash} = -P_{\text{paid}}, \quad \Delta \text{Loan Liabilities} = -P_{\text{paid}} \implies \Delta \text{Net Worth} = \$0.00$$
3. **Interest & Operating Expenses:**
   $$\Delta \text{Cash} = -I_{\text{paid}}, \quad \text{Expense Recorded} = I_{\text{paid}} \implies \Delta \text{Net Worth} = -I_{\text{paid}}$$
4. **Salary Income:**
   $$\Delta \text{Cash} = +S, \quad \text{Income Recorded} = +S \implies \Delta \text{Net Worth} = +S$$

---

## 3. Loan Amortization & Debt Calculations

Loans follow standard monthly equal installment amortization.

### 3.1 Monthly Payment Formula
$$M = P \times \frac{r(1+r)^n}{(1+r)^n - 1}$$
Where:
- $P$ = Loan Principal (e.g., $5,000.00)
- $r$ = Monthly interest rate ($\text{Annual APR} / 12 = 0.12 / 12 = 0.01$)
- $n$ = Term in months (12 months)

**Calculated Values:**
- For **$5,000.00 at 12% APR (12 mo):** $M = \$444.24 / \text{month}$.
  - Total Interest Paid over term: $\$330.88$. Total Repaid: $\$5,330.88$.
- For **$10,000.00 at 12% APR (12 mo):** $M = \$888.49 / \text{month}$.

### 3.2 Monthly Split Between Principal and Interest
For any month $k$ with remaining principal $P_{k}$:
1. **Interest Portion:** $I_{k} = P_{k} \times r$
2. **Principal Portion:** $\Delta P_{k} = \min(M - I_{k}, P_{k})$
3. **Updated Principal:** $P_{k+1} = P_{k} - \Delta P_{k}$

---

## 4. Cash Flow & Solvency Analysis

The [`FinancialHealthService`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialHealthService.cs) evaluates monthly net cash flow and risk exposure:

$$\text{Projected Net Monthly Cash Flow} = \sum \text{Monthly Salaries} - \left( \sum \text{Living Expenses} + \sum \text{Loan Installments} \right)$$

### Decision Tree for Health Classification:
```
                                 [Evaluate Balance Sheet & Cash Flow]
                                                  │
                                   Is Default Delinquent? (>=2 missed & NetWorth < 0)
                                    ├── YES ──> [BANKRUPT]
                                    └── NO
                                                  │
                                   Is Insolvent? (NetWorth < 0 & CashFlow < 0)
                                    ├── YES ──> [INSOLVENT]
                                    └── NO
                                                  │
                                   Is Overdue > 0 OR Cash < 1 Week Expenses?
                                    ├── YES ──> [CASH CRISIS]
                                    └── NO
                                                  │
                                   Is CashFlow < 0 OR Cash < 1 Month Expenses?
                                    ├── YES ──> [FINANCIALLY STRESSED]
                                    └── NO ───> [HEALTHY]
```

---

## 5. Persistence Schema (Version 2)

Saved to local JSON (`richrich_save.json` + `richrich_save.backup.json`):
```json
{
  "SchemaVersion": 2,
  "CreatedAtUnix": 1700000000,
  "LastSavedUnix": 1700000500,
  "Difficulty": "Standard",
  "SimulationMinutes": 21600,
  "LastProcessedSimulationMinute": 21600,
  "BalanceCents": 300000,
  "LifetimeEarnedCents": 300000,
  "OverdueLiabilitiesCents": 0,
  "CreditScore": 650,
  "ActiveLoans": [],
  "CurrentJobId": "job_retail_assistant",
  "TotalMonthsWorked": 1,
  "ActiveCourseId": "course_it_support_cert",
  "ActiveCourseHoursCompleted": 20.0,
  "EarnedQualifications": ["qual_digital_basics"],
  "SoundEnabled": true,
  "HapticsEnabled": true
}
```
Backward compatibility is maintained: Loading a Schema 1 file seamlessly initializes missing fields to default values without data corruption.
