# RichRich: Education & Higher Degrees System Specification

**Specification Version:** 1.0 (Milestone 3 Baseline)  
**Author:** Lead Systems Architect & Gameplay Engineer  
**Status:** Implemented & Verified (87 / 87 Passing Automated Tests)

---

## 1. Overview & Pedagogical Purpose

Education in **RichRich** is the primary engine of upward socio-economic mobility. Rather than granting instant access to lucrative executive positions, the player must invest time and financial capital into accredited credentials and skill points.

The system unifies:
- Short introductory workshops (0-cost bootstrap).
- Professional technical certificates (mid-tier gateway).
- Accredited higher education degrees (university programs unlocking senior engineering and corporate roles).

---

## 2. Program Catalogue

| Program ID | Title | Type | Tuition | Duration | Pace (Default) | Prereqs | Granted Credential | Skill Rewards |
| :--- | :--- | :--- | :---:| :---:| :--- | :--- | :--- | :--- |
| `course_free_digital` | Foundations of Digital Skills | ShortCourse | **$0.00** | 20 hrs | Part-Time | None | `qual_digital_basics` | Technical (+10) |
| `course_retail_sales_bootcamp` | Sales & Customer Engagement Bootcamp | ShortCourse | **$250.00** | 30 hrs | Part-Time | None | `qual_retail_sales` | Comm (+15), Sales (+20) |
| `course_it_support_cert` | IT Support Professional Certificate | ProfessionalCertificate | **$500.00** | 60 hrs | Part-Time | `qual_digital_basics` | `qual_it_support_cert` | Technical (+25), Comm (+10) |
| `degree_business_diploma` | Diploma in Business Administration | Degree | **$3,500.00** | 180 hrs | Part-Time | None | `qual_business_diploma` | Management (+35), Analytics (+25), Comm (+15) |
| `degree_computer_science` | Bachelor of Computer Science | Degree | **$12,000.00** | 480 hrs | Part-Time | `qual_digital_basics` | `qual_cs_degree` | Technical (+70), Analytics (+50), Management (+15) |

---

## 3. Study Mechanics & Work-Study Balance

### 3.1 Study Paces
The player can toggle between two study intensities at any time:
1. **Part-Time (`StudyPace.PartTime`):**
   - **2.0 study hours / day**
   - Energy cost: **10 energy / day**
   - Ideal for working full-time (40 hrs/wk) without risking fatigue.
2. **Full-Time (`StudyPace.FullTime`):**
   - **6.0 study hours / day**
   - Energy cost: **30 energy / day**
   - 3x faster graduation speed; recommended when the player has accumulated an emergency cash cushion or upgraded housing.

### 3.2 Concurrent Work & Study
- Players are **never blocked from earning money while studying**.
- Both work shifts and daily study progress run concurrently on every daily clock tick.
- Full-Time study combined with a high-intensity career requires managing energy through upgraded accommodation or scheduled rest breaks.

---

## 4. Financial Integration

- **Tuition Deduction:** Evaluated upfront through [`FinancialService.TryRecordExpense()`](file:///D:/Webapp/RichyRich/RichRich_Unity/Assets/Scripts/Domain/Services/FinancialService.cs) under `TransactionCategory.EducationTuition`.
- **Atomic Balance Safety:** If the wallet has insufficient funds, enrollment is safely rejected before any state mutations occur.
- **ROI Payoff:**
  - `course_it_support_cert`: Costs $500.00 $\rightarrow$ Unlocks Junior IT Helpdesk (+$1,000/mo salary raise over Courier). Pays for itself in **15 game days**.
  - `degree_business_diploma`: Costs $3,500.00 $\rightarrow$ Unlocks Business Operations Executive ($4,200/mo) and Enterprise Consultant ($10,500/mo).
  - `degree_computer_science`: Costs $12,000.00 $\rightarrow$ Unlocks Technology Manager ($13,500/mo).

---

## 5. Persistence Schema Representation

In `SaveData.cs` (Schema v3):
```json
{
  "ActiveCourseId": "degree_business_diploma",
  "ActiveCourseHoursCompleted": 62.0,
  "StudyPace": "FullTime",
  "EarnedQualifications": [
    "qual_digital_basics",
    "qual_it_support_cert"
  ]
}
```
Academic progress and earned qualifications are restored across session boundaries.
