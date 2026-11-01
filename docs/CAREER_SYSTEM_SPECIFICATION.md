# RichRich: Career & Employment System Specification

**Specification Version:** 1.0 (Milestone 3 Baseline)  
**Author:** Lead Systems Architect & Gameplay Engineer  
**Status:** Implemented & Verified (87 / 87 Passing Automated Tests)

---

## 1. Subsystem Architecture Overview

The **Career & Employment System** transitions RichRich from an idle tap prototype to an authentic, decision-driven career progression simulator. The player builds lifetime earnings by navigating employment vacancies, fulfilling qualifications, climbing career tracks, and managing job tenure.

All financial payouts are executed through double-entry accounting in [`FinancialService`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialService.cs) and recorded immutably in [`FinancialLedger`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialLedger.cs).

```
┌────────────────────────────────────────────────────────┐
│                   Simulation Clock                     │
│                  (Day 30 Payday Check)                 │
└───────────────────────────┬────────────────────────────┘
                            │ triggers
┌───────────────────────────▼────────────────────────────┐
│                    CareerService                       │
│  - Evaluates active EmploymentRecord                   │
│  - Validates LastPaidDate (Anti-duplication)           │
│  - Disburses Monthly Salary via FinancialService       │
│  - Increments TrackExperience & CurrentJobTenure       │
└───────────────────────────┬────────────────────────────┘
                            │ updates
┌───────────────────────────▼────────────────────────────┐
│           FinancialLedger & BalanceSheet               │
│  - TransactionCategory.EmploymentSalary                │
│  - Total Assets, Cash, Lifetime Earned                 │
└────────────────────────────────────────────────────────┘
```

---

## 2. Career Tracks & Progression Ladders

The game features three structured career ladders and general entry-level logistics:

### 2.1 Retail Operations Ladder
| Tier | Job Title | Company | Salary / mo | Hours / wk | Requirements |
| :---: | :--- | :--- | :---:| :---:| :--- |
| **1** | Retail Assistant | QuickMart Supermarkets | $1,600.00 | 40 | None (Entry level) |
| **1** | Sales Assistant | Metro Direct Stores | $2,000.00 | 40 | None (Entry level) |
| **2** | Senior Retail Assistant | QuickMart Supermarkets | $2,300.00 | 40 | 6 mo exp, Sales $\ge$ 15, Comm $\ge$ 10 |
| **3** | Shift Supervisor | QuickMart Supermarkets | $3,200.00 | 42 | 12 mo exp, 6 mo tenure, Management $\ge$ 15, Sales $\ge$ 25 |
| **4** | Store Manager | QuickMart Supermarkets | $4,600.00 | 45 | 24 mo exp, 6 mo tenure, Management $\ge$ 30, Analytics $\ge$ 20, Qual: `qual_retail_sales` |

### 2.2 Technology & Engineering Ladder
| Tier | Job Title | Company | Salary / mo | Hours / wk | Requirements |
| :---: | :--- | :--- | :---:| :---:| :--- |
| **1** | IT Support Trainee | Apex Enterprise Solutions | $2,800.00 | 40 | Qual: `qual_it_support_cert`, Technical $\ge$ 20 |
| **2** | IT Support Specialist | Apex Enterprise Solutions | $3,600.00 | 40 | 6 mo exp, Technical $\ge$ 35, Comm $\ge$ 15 |
| **3** | Junior Software Developer | Nova Byte Dynamics | $4,800.00 | 40 | 6 mo exp, Technical $\ge$ 50, Analytics $\ge$ 25 |
| **4** | Software Developer | Nova Byte Dynamics | $6,800.00 | 40 | 18 mo exp, 6 mo tenure, Technical $\ge$ 70, Analytics $\ge$ 40 |
| **5** | Senior Software Developer | Nova Byte Dynamics | $9,200.00 | 40 | 36 mo exp, 6 mo tenure, Technical $\ge$ 85, Analytics $\ge$ 60, Management $\ge$ 20 |
| **6** | Technology Manager | Nova Byte Dynamics | $13,500.00 | 45 | 48 mo exp, 6 mo tenure, Technical $\ge$ 80, Management $\ge$ 50, Analytics $\ge$ 50, Qual: `qual_cs_degree` |

### 2.3 Business & Enterprise Consulting Ladder
| Tier | Job Title | Company | Salary / mo | Hours / wk | Requirements |
| :---: | :--- | :--- | :---:| :---:| :--- |
| **1** | Office Assistant | Beacon Corporate Services | $1,900.00 | 40 | None (Entry level) |
| **2** | Administrative Officer | Beacon Corporate Services | $2,700.00 | 40 | 6 mo exp, Comm $\ge$ 20, Analytics $\ge$ 10 |
| **3** | Business Operations Executive | Vanguard Holdings | $4,200.00 | 42 | 12 mo exp, Management $\ge$ 25, Analytics $\ge$ 25, Qual: `qual_business_diploma` |
| **4** | Business Operations Manager | Vanguard Holdings | $6,500.00 | 45 | 24 mo exp, 6 mo tenure, Management $\ge$ 45, Analytics $\ge$ 40 |
| **5** | Enterprise Consultant | Global Advisory Partners | $10,500.00 | 50 | 36 mo exp, 6 mo tenure, Management $\ge$ 60, Comm $\ge$ 50, Analytics $\ge$ 50, Qual: `qual_business_diploma` |

### 2.4 General & Logistics
| Tier | Job Title | Company | Salary / mo | Hours / wk | Requirements |
| :---: | :--- | :--- | :---:| :---:| :--- |
| **1** | Delivery Courier | SwiftDrop Logistics | $1,800.00 | 40 | None (Entry level) |

---

## 3. Application, Eligibility & Promotion Mechanics

1. **Qualification Validation:** Enforces exact credential verification via [`EducationService.HasQualification()`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/EducationService.cs).
2. **Skill Thresholds:** Checks proficiency levels across `Technical`, `Communication`, `Management`, `Sales`, and `Analytics`.
3. **Experience & Tenure:**
   - Overall Experience: Sum of all cumulative months worked across the career.
   - Track Experience: Dedicated experience accumulated in the relevant sector (`RetailExperienceMonths`, `TechExperienceMonths`, `BusinessExperienceMonths`).
   - Tenure: Minimum consecutive months in current position before internal promotion eligibility (prevents skipping tiers).
4. **Structured Application Results:**
   - [`JobApplicationResult`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Models/JobModel.cs) returns discrete statuses: `Offered`, `RejectedMissingQualification`, `RejectedInsufficientSkills`, `RejectedInsufficientExperience`, `RejectedTenureNotMet`, or `PositionNotFound`.
5. **Anti-Duplication Job Switching Rule:**
   - When switching jobs mid-month after payday, `priorLastPaid` is transferred to the new [`EmploymentRecord`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Models/JobModel.cs). A player cannot collect a second salary on Day 30 by changing jobs.

---

## 4. Persistence Schema Representation

In `SaveData.cs` (Schema v3):
```json
{
  "CurrentJobId": "job_it_helpdesk",
  "TotalMonthsWorked": 14,
  "CurrentJobTenureMonths": 8,
  "LastSalaryPaidMonth": 5,
  "RetailExperienceMonths": 6,
  "TechExperienceMonths": 8,
  "BusinessExperienceMonths": 0
}
```
All career experience and tenure are restored across application restarts.
