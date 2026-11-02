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
        public BusinessService BusinessService { get; private set; }
        public RealEstateService RealEstateService { get; private set; }
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

            // 9. Entrepreneurship & Commercial Business Ownership
            BusinessService = new BusinessService(FinancialService, Clock, CareerService);
            LoanService.BusinessService = BusinessService;

            // 10. Financial Health Evaluator
            HealthService = new FinancialHealthService(FinancialService, ExpenseService, CareerService, LoanService, BusinessService);

            // Restore Businesses
            if (save.Businesses != null && save.Businesses.Count > 0)
            {
                foreach (var bSnap in save.Businesses)
                {
                    if (bSnap.IsOwned)
                    {
                        BusinessService.RestoreOwnedBusiness(bSnap.Id, bSnap.Level, bSnap.Reputation, (int)bSnap.Condition, bSnap.IsOpen, bSnap.CompletedTasks);
                    }
                }
            }
            else if (save.IsBusinessOwned)
            {
                // Schema 1/2/3 backwards compatibility
                BusinessService.RestoreOwnedBusiness("auto_repair", save.BusinessLevel, 50, 100, true, save.BusinessTasksCompleted);
            }

            // Restore Employees
            if (save.Employees != null && save.Employees.Count > 0)
            {
                foreach (var eSnap in save.Employees)
                {
                    if (Enum.TryParse(eSnap.Role, out EmployeeRole role))
                    {
                        var hireDate = eSnap.HiredMonth > 0 ? new GameDateTime(currentDate.Year, Math.Clamp(eSnap.HiredMonth, 1, 12), 1) : Clock.CurrentDate;
                        var emp = new Employee(eSnap.Id, eSnap.Name, eSnap.BusinessId, role, Currency.FromCents(eSnap.MonthlySalaryCents), eSnap.Productivity, hireDate);
                        BusinessService.RestoreEmployee(emp);
                    }
                }
            }

            // Restore Convenience Store inventory
            if (save.StoreInventory != null && save.StoreInventory.Count > 0)
            {
                foreach (var pSnap in save.StoreInventory)
                {
                    BusinessService.RestoreStoreProduct(pSnap.ProductId, pSnap.StockOnHand, Currency.FromCents(pSnap.SellingPriceCents), pSnap.TotalUnitsSold, pSnap.MissedSalesDueToStockout);
                }
            }

            // Restore Digital Agency client projects
            if (save.AgencyProjects != null && save.AgencyProjects.Count > 0)
            {
                foreach (var prSnap in save.AgencyProjects)
                {
                    if (Enum.TryParse(prSnap.Category, out AgencyProjectCategory cat))
                    {
                        var proj = new ClientProject(
                            prSnap.ProjectId,
                            prSnap.Name,
                            cat,
                            (int)prSnap.RequiredHours,
                            prSnap.DeadlineDay,
                            Currency.FromCents(prSnap.ContractValueCents),
                            reputationGain: prSnap.RequiredReputation,
                            completedHours: (int)prSnap.CompletedHours,
                            isAccepted: prSnap.IsAccepted,
                            isCompleted: prSnap.IsCompleted,
                            isFailed: prSnap.IsExpired
                        );
                        BusinessService.RestoreAgencyProject(proj);
                    }
                }
            }

            // 11. Real Estate & Property Investments (Schema 5)
            List<RealEstateProperty> propList = RealEstateProperty.GetDefaultMarketplace();
            if (save.Properties != null && save.Properties.Count > 0)
            {
                foreach (var pSnap in save.Properties)
                {
                    var existing = propList.FirstOrDefault(p => p.Id == pSnap.Id);
                    if (existing != null && pSnap.IsOwned)
                    {
                        Mortgage restoredMortgage = null;
                        if (pSnap.Mortgage != null)
                        {
                            restoredMortgage = new Mortgage(
                                pSnap.Mortgage.Id,
                                pSnap.Mortgage.PropertyId,
                                Currency.FromCents(pSnap.Mortgage.OriginalPrincipalCents),
                                pSnap.Mortgage.AnnualInterestRate,
                                pSnap.Mortgage.TermMonths,
                                Currency.FromCents(pSnap.Mortgage.RemainingPrincipalCents),
                                pSnap.Mortgage.MonthsRemaining,
                                pSnap.Mortgage.PaymentsMade
                            );
                        }

                        var restoredProp = new RealEstateProperty(
                            existing.Id,
                            existing.Name,
                            existing.Type,
                            existing.PurchasePrice,
                            currentValuation: Currency.FromCents(pSnap.CurrentValuationCents),
                            downPaymentPercent: pSnap.DownPaymentPercent,
                            condition: pSnap.Condition,
                            baseMonthlyRent: existing.BaseMonthlyRent,
                            monthlyMaintenanceCost: Currency.FromCents(pSnap.MonthlyMaintenanceCostCents),
                            annualAppreciationRate: existing.AnnualAppreciationRate,
                            isOwned: true,
                            isRented: pSnap.IsRented,
                            tenantMonthlyRent: Currency.FromCents(pSnap.TenantMonthlyRentCents),
                            mortgage: restoredMortgage
                        );

                        int idx = propList.IndexOf(existing);
                        propList[idx] = restoredProp;
                    }
                }
            }

            RealEstateService = new RealEstateService(Wallet, Ledger, FinancialService, Clock, propList);
            if (HealthService != null)
            {
                HealthService.RealEstateService = RealEstateService;
            }

            BusinessService.UpdateBalanceSheetAssets();
            RealEstateService.UpdateBalanceSheet();
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
            var autoRepair = BusinessService?.GetBusiness("auto_repair");
            var save = new SaveData
            {
                SchemaVersion = 5,
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
                }).ToList(),
                Businesses = BusinessService?.AllBusinesses.Select(b => new BusinessSaveSnapshot
                {
                    Id = b.Id,
                    Name = b.Name,
                    Category = b.Category.ToString(),
                    IsOwned = b.IsOwned,
                    Level = b.Level,
                    Reputation = b.Reputation,
                    Condition = b.Condition,
                    IsOpen = b.IsOpen,
                    CompletedTasks = b.CompletedTasksCount
                }).ToList() ?? new List<BusinessSaveSnapshot>(),
                Employees = BusinessService?.HiredEmployees.Select(e => new EmployeeSaveSnapshot
                {
                    Id = e.Id,
                    Name = e.Name,
                    BusinessId = e.BusinessId,
                    Role = e.Role.ToString(),
                    MonthlySalaryCents = e.MonthlySalary.MinorUnits,
                    Productivity = e.Productivity,
                    HiredMonth = e.HireDate.Month
                }).ToList() ?? new List<EmployeeSaveSnapshot>(),
                StoreInventory = BusinessService?.ConvenienceStoreCatalog.Select(p => new StoreProductSaveSnapshot
                {
                    ProductId = p.Id,
                    StockOnHand = p.StockOnHand,
                    SellingPriceCents = p.SellingPrice.MinorUnits,
                    TotalUnitsSold = p.UnitsSoldTotal,
                    MissedSalesDueToStockout = p.MissedSalesTotal
                }).ToList() ?? new List<StoreProductSaveSnapshot>(),
                AgencyProjects = BusinessService?.AgencyProjects.Select(pr => new AgencyProjectSaveSnapshot
                {
                    ProjectId = pr.ProjectId,
                    Name = pr.ClientName,
                    Category = pr.Category.ToString(),
                    ContractValueCents = pr.ContractPayment.MinorUnits,
                    RequiredHours = pr.TotalHoursRequired,
                    CompletedHours = pr.CompletedHours,
                    DeadlineDay = pr.DeadlineDays,
                    RequiredReputation = pr.ReputationGain,
                    IsAccepted = pr.IsAccepted,
                    IsCompleted = pr.IsCompleted,
                    IsExpired = pr.IsFailed
                }).ToList() ?? new List<AgencyProjectSaveSnapshot>(),
                ActiveRepairJobs = BusinessService?.ServiceBays.Where(b => b.IsOccupied).Select(b => new AutoRepairJobSaveSnapshot
                {
                    JobId = b.CurrentJob.JobId,
                    VehicleModel = b.CurrentJob.VehicleModel,
                    Category = b.CurrentJob.Category.ToString(),
                    RevenueCents = b.CurrentJob.CustomerPayment.MinorUnits,
                    PartsCostCents = b.CurrentJob.PartsCost.MinorUnits,
                    DurationMinutes = b.CurrentJob.RequiredMinutes,
                    ElapsedMinutes = b.CurrentJob.ElapsedMinutes,
                    IsCompleted = b.CurrentJob.IsCompleted,
                    AssignedBayIndex = b.BayId
                }).ToList() ?? new List<AutoRepairJobSaveSnapshot>(),
                Properties = RealEstateService?.AllProperties.Select(p => new PropertySaveSnapshot
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type.ToString(),
                    PurchasePriceCents = p.PurchasePrice.MinorUnits,
                    CurrentValuationCents = p.CurrentValuation.MinorUnits,
                    DownPaymentPercent = p.DownPaymentPercent,
                    Condition = p.Condition,
                    BaseMonthlyRentCents = p.BaseMonthlyRent.MinorUnits,
                    TenantMonthlyRentCents = p.TenantMonthlyRent.MinorUnits,
                    MonthlyMaintenanceCostCents = p.MonthlyMaintenanceCost.MinorUnits,
                    AnnualAppreciationRate = p.AnnualAppreciationRate,
                    IsOwned = p.IsOwned,
                    IsRented = p.IsRented,
                    Mortgage = p.Mortgage != null ? new MortgageSaveSnapshot
                    {
                        Id = p.Mortgage.Id,
                        PropertyId = p.Mortgage.PropertyId,
                        OriginalPrincipalCents = p.Mortgage.OriginalPrincipal.MinorUnits,
                        RemainingPrincipalCents = p.Mortgage.RemainingPrincipal.MinorUnits,
                        AnnualInterestRate = p.Mortgage.AnnualInterestRate,
                        TermMonths = p.Mortgage.TermMonths,
                        MonthsRemaining = p.Mortgage.MonthsRemaining,
                        PaymentsMade = p.Mortgage.PaymentsMade
                    } : null
                }).ToList() ?? new List<PropertySaveSnapshot>(),
                CurrentLevel = 1,
                Tickets = EconomyConfig.StartingTickets,
                BusinessId = "auto_repair",
                IsBusinessOwned = autoRepair != null && autoRepair.IsOwned,
                BusinessLevel = autoRepair != null ? autoRepair.Level : 1,
                BusinessTasksCompleted = autoRepair != null ? autoRepair.CompletedTasksCount : 0
            };

            SaveService.Save(save);
            OnSimulationSaved?.Invoke();
            return save;
        }
    }
}
