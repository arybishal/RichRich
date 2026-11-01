using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Master coordinator for the Life & Wealth simulation.
    /// Unifies the simulation clock, financial service, living expenses, housing, career,
    /// education, loans, and lifestyle energy.
    /// Pure C# domain coordinator with zero MonoBehaviour dependencies.
    /// </summary>
    public class LifeSimulationCoordinator
    {
        public GameDifficulty Difficulty { get; private set; }
        public SimulationClock Clock { get; private set; }
        public Wallet Wallet { get; private set; }
        public FinancialLedger Ledger { get; private set; }
        public BalanceSheet BalanceSheet { get; private set; }
        public FinancialService FinancialService { get; private set; }
        public LivingExpenseService ExpenseService { get; private set; }
        public HousingService HousingService { get; private set; }
        public EducationService EducationService { get; private set; }
        public CareerService CareerService { get; private set; }
        public LoanService LoanService { get; private set; }
        public LifestyleService LifestyleService { get; private set; }
        public FinancialHealthService HealthService { get; private set; }
        public ISaveDataService SaveService { get; }

        private long _createdAtUnix;

        public event Action OnSimulationSaved;

        public LifeSimulationCoordinator(ISaveDataService saveService)
        {
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public void InitializeSession(GameDifficulty difficulty = GameDifficulty.Standard)
        {
            SaveData save;
            if (SaveService is SaveDataService fileSvc && File.Exists(fileSvc.PrimaryPath))
            {
                save = SaveService.Load();
                _createdAtUnix = save.CreatedAtUnix != 0 ? save.CreatedAtUnix : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                _createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                save = SaveData.CreateDefault(_createdAtUnix, difficulty);
            }

            if (Enum.TryParse(save.Difficulty, out GameDifficulty diff))
            {
                Difficulty = diff;
            }
            else
            {
                Difficulty = difficulty;
            }

            // 1. Clock
            var currentDate = new GameDateTime(save.SimulationMinutes);
            Clock = new SimulationClock(currentDate, save.LastProcessedSimulationMinute);

            // 2. Financial foundation
            Wallet = new Wallet(Currency.FromCents(save.BalanceCents), Currency.FromCents(save.LifetimeEarnedCents));
            Ledger = new FinancialLedger();
            BalanceSheet = new BalanceSheet();
            BalanceSheet.UpdateCash(Wallet.Balance);
            BalanceSheet.UpdateOverdueLiabilities(Currency.FromCents(save.OverdueLiabilitiesCents));

            FinancialService = new FinancialService(Wallet, Ledger, Clock, BalanceSheet);

            // 3. Living expenses
            ExpenseService = new LivingExpenseService(FinancialService, Clock);
            if (save.LastExpensePaidMonth > 0)
            {
                foreach (var exp in ExpenseService.Expenses)
                {
                    exp.MarkPaid(new GameDateTime(currentDate.Year, save.LastExpensePaidMonth, exp.DueDayOfMonth));
                }
            }

            // 4. Education & Skills
            EducationService = new EducationService(FinancialService, Clock);
            if (save.EarnedQualifications != null)
            {
                foreach (var qual in save.EarnedQualifications)
                {
                    EducationService.AddQualification(qual);
                }
            }

            if (!string.IsNullOrEmpty(save.ActiveCourseId))
            {
                var course = EducationService.Catalog.FirstOrDefault(c => c.Id == save.ActiveCourseId);
                if (course != null)
                {
                    var pace = Enum.TryParse(save.StudyPace, out StudyPace parsedPace) ? parsedPace : StudyPace.PartTime;
                    EducationService.TryEnroll(course.Id, pace, out _);
                    if (EducationService.ActiveEnrollment != null && save.ActiveCourseHoursCompleted > 0)
                    {
                        EducationService.ActiveEnrollment.AddStudyHours(save.ActiveCourseHoursCompleted, Clock.CurrentDate);
                    }
                }
            }

            // 5. Career & Employment
            CareerService = new CareerService(FinancialService, EducationService, Clock);
            if (save.RetailExperienceMonths > 0) CareerService.SetTrackExperience(CareerTrack.Retail, save.RetailExperienceMonths);
            if (save.TechExperienceMonths > 0) CareerService.SetTrackExperience(CareerTrack.Technology, save.TechExperienceMonths);
            if (save.BusinessExperienceMonths > 0) CareerService.SetTrackExperience(CareerTrack.Business, save.BusinessExperienceMonths);

            if (!string.IsNullOrEmpty(save.CurrentJobId))
            {
                CareerService.TryApply(save.CurrentJobId, out _);
                if (CareerService.IsEmployed && save.LastSalaryPaidMonth > 0)
                {
                    var payDate = new GameDateTime(currentDate.Year, save.LastSalaryPaidMonth, CareerService.CurrentEmployment.Job.PayDayOfMonth);
                    CareerService.CurrentEmployment.RecordSalaryPaid(payDate);
                }
            }

            // 6. Housing & Accommodation
            HousingService = new HousingService(FinancialService, ExpenseService, CareerService, Clock);
            string housingId = !string.IsNullOrEmpty(save.CurrentHousingTierId) ? save.CurrentHousingTierId : "housing_studio";
            long depositCents = save.HousingDepositPaidCents > 0 ? save.HousingDepositPaidCents : 60000L;
            HousingService.RestoreSavedHousing(housingId, Currency.FromCents(depositCents));

            // 7. Loans & Liabilities
            LoanService = new LoanService(FinancialService, CareerService, Clock);
            if (save.ActiveLoans != null && save.ActiveLoans.Count > 0)
            {
                foreach (var snapshot in save.ActiveLoans)
                {
                    var product = LoanService.Catalog.FirstOrDefault(p => p.Id == snapshot.ProductId)
                                  ?? LoanService.Catalog.FirstOrDefault();

                    if (product != null)
                    {
                        var restoredLoan = new ActiveLoan(
                            snapshot.LoanId,
                            product,
                            Clock.CurrentDate,
                            snapshot.DueDayOfMonth,
                            Currency.FromCents(snapshot.RemainingPrincipalCents),
                            snapshot.MonthsRemaining,
                            snapshot.PaymentsMade,
                            snapshot.MissedPaymentsCount,
                            Currency.FromCents(snapshot.OverdueAmountCents),
                            snapshot.LastPaidMonth
                        );
                        LoanService.AddRestoredLoan(restoredLoan);
                    }
                }
            }

            // 8. Lifestyle & Energy
            int energy = save.CurrentEnergy > 0 ? save.CurrentEnergy : 100;
            LifestyleService = new LifestyleService(Clock, CareerService, EducationService, HousingService, energy);

            // 9. Financial Health Evaluator
            HealthService = new FinancialHealthService(FinancialService, ExpenseService, CareerService, LoanService);
        }

        public void Tick(double deltaRealSeconds)
        {
            Clock?.Tick(deltaRealSeconds);
        }

        public void FastForwardDays(int days)
        {
            Clock?.AdvanceDays(days);
        }

        public SaveData SaveCurrentState()
        {
            var save = new SaveData
            {
                SchemaVersion = 3,
                CreatedAtUnix = _createdAtUnix != 0 ? _createdAtUnix : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Difficulty = Difficulty.ToString(),
                SimulationMinutes = Clock.CurrentDate.TotalMinutes,
                LastProcessedSimulationMinute = Clock.LastProcessedMinute,
                BalanceCents = Wallet.Balance.MinorUnits,
                LifetimeEarnedCents = Wallet.LifetimeEarned.MinorUnits,
                OverdueLiabilitiesCents = BalanceSheet.OverdueLiabilities.MinorUnits,
                CreditScore = LoanService.CreditScore,
                CurrentJobId = CareerService.IsEmployed ? CareerService.CurrentEmployment.Job.Id : null,
                TotalMonthsWorked = CareerService.IsEmployed ? CareerService.CurrentEmployment.TotalMonthsWorked : 0,
                CurrentJobTenureMonths = CareerService.IsEmployed ? CareerService.CurrentEmployment.CurrentJobTenureMonths : 0,
                LastSalaryPaidMonth = CareerService.IsEmployed && CareerService.CurrentEmployment.LastPaidDate.HasValue
                    ? CareerService.CurrentEmployment.LastPaidDate.Value.Month : 0,
                RetailExperienceMonths = CareerService.GetTrackExperienceMonths(CareerTrack.Retail),
                TechExperienceMonths = CareerService.GetTrackExperienceMonths(CareerTrack.Technology),
                BusinessExperienceMonths = CareerService.GetTrackExperienceMonths(CareerTrack.Business),
                LastExpensePaidMonth = ExpenseService.Expenses.FirstOrDefault(e => e.LastPaidDate.HasValue)?.LastPaidDate.Value.Month ?? 0,
                CurrentHousingTierId = HousingService.CurrentHousing.Id,
                HousingDepositPaidCents = HousingService.HousingDepositPaid.MinorUnits,
                ActiveCourseId = EducationService.ActiveEnrollment != null && !EducationService.ActiveEnrollment.IsCompleted
                    ? EducationService.ActiveEnrollment.Course.Id : null,
                ActiveCourseHoursCompleted = EducationService.ActiveEnrollment?.HoursCompleted ?? 0.0,
                StudyPace = EducationService.CurrentStudyPace.ToString(),
                EarnedQualifications = EducationService.EarnedQualifications.ToList(),
                CurrentEnergy = LifestyleService.CurrentEnergy,
                ActiveLoans = LoanService.ActiveLoans.Select(l => new LoanSaveSnapshot
                {
                    LoanId = l.LoanId,
                    ProductId = l.Product.Id,
                    RemainingPrincipalCents = l.RemainingPrincipal.MinorUnits,
                    MonthsRemaining = l.MonthsRemaining,
                    PaymentsMade = l.PaymentsMade,
                    MissedPaymentsCount = l.MissedPaymentsCount,
                    OverdueAmountCents = l.OverdueAmount.MinorUnits,
                    DueDayOfMonth = l.DueDayOfMonth,
                    LastPaidMonth = l.LastPaidMonth
                }).ToList()
            };

            SaveService.Save(save);
            OnSimulationSaved?.Invoke();
            return save;
        }
    }
}
