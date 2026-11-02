using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Master commercial business management domain service.
    /// Manages the three core enterprises: Auto Repair Workshop, Convenience Store, and Digital Agency.
    /// Coordinates operations, inventory, client contracts, employees, P&L accounting,
    /// valuations, and double-entry ledger integration.
    /// Pure C# domain service with zero MonoBehaviour dependencies.
    /// </summary>
    public class BusinessService
    {
        private readonly List<Business> _catalog = new List<Business>();
        private readonly List<ServiceBay> _autoRepairBays = new List<ServiceBay>();
        private readonly List<VehicleRepairJob> _availableRepairJobs = new List<VehicleRepairJob>();
        private readonly List<StoreProduct> _storeInventory = new List<StoreProduct>();
        private readonly List<ClientProject> _agencyProjects = new List<ClientProject>();
        private readonly List<Employee> _candidatePool = new List<Employee>();

        public IReadOnlyList<Business> Catalog => _catalog;
        public IReadOnlyList<Business> AllBusinesses => _catalog;
        public IReadOnlyList<Business> OwnedBusinesses => _catalog.Where(b => b.IsOwned).ToList();

        public IReadOnlyList<ServiceBay> AutoRepairBays => _autoRepairBays;
        public IReadOnlyList<ServiceBay> ServiceBays => _autoRepairBays;
        public IReadOnlyList<VehicleRepairJob> AvailableRepairJobs => _availableRepairJobs;
        public IReadOnlyList<StoreProduct> StoreInventory => _storeInventory;
        public IReadOnlyList<StoreProduct> ConvenienceStoreCatalog => _storeInventory;
        public IReadOnlyList<ClientProject> AgencyProjects => _agencyProjects;
        public IReadOnlyList<Employee> CandidatePool => _candidatePool;
        public IEnumerable<Employee> HiredEmployees => OwnedBusinesses.SelectMany(b => b.Employees);

        public FinancialService FinancialService { get; }
        public SimulationClock Clock { get; }
        public CareerService CareerService { get; }

        public Currency TotalBusinessValuation
        {
            get
            {
                long sum = OwnedBusinesses.Sum(b => (long)b.CalculateValuation().MinorUnits);
                return Currency.FromCents(sum);
            }
        }

        public Currency ProjectedMonthlyNetProfit
        {
            get
            {
                long sum = OwnedBusinesses.Sum(b => b.PAndL.NetProfitCents);
                return sum > 0 ? Currency.FromCents(sum) : Currency.Zero;
            }
        }

        public Currency CalculateMonthlyNetProfit() => ProjectedMonthlyNetProfit;

        public event Action<Business> OnBusinessPurchased;
        public event Action<Business, Currency> OnBusinessSold;
        public event Action<Business> OnBusinessUpgraded;
        public event Action<VehicleRepairJob> OnRepairJobCompleted;
        public event Action<StoreDailySalesResult> OnStoreDailySalesProcessed;
        public event Action<ClientProject> OnAgencyProjectCompleted;
        public event Action<Employee> OnEmployeeHired;
        public event Action<Employee> OnEmployeeDismissed;

        public BusinessService(
            FinancialService financialService,
            SimulationClock clock,
            CareerService careerService,
            IEnumerable<Business> customCatalog = null)
        {
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            CareerService = careerService ?? throw new ArgumentNullException(nameof(careerService));

            if (customCatalog != null)
            {
                _catalog.AddRange(customCatalog);
            }
            else
            {
                _catalog.AddRange(GetDefaultCatalog());
            }

            InitializeDefaultSubsystems();

            Clock.OnDayPassed += ProcessDailyBusinessOperations;
            Clock.OnMinuteTicked += OnClockMinutePassed;
        }

        public static List<Business> GetDefaultCatalog()
        {
            return new List<Business>
            {
                new Business(
                    id: "auto_repair",
                    name: "Auto Repair Workshop",
                    baseCost: Currency.FromDollars(5499.00),
                    baseHourlyYield: Currency.FromDollars(120.00),
                    isOwned: false,
                    level: 1,
                    category: BusinessCategory.Automotive,
                    setupCost: Currency.FromDollars(500.00),
                    reputation: 60,
                    condition: 100,
                    dailyOperatingExpenses: Currency.FromDollars(25.00),
                    description: "Full-service automotive workshop offering maintenance, brake overhauls, and engine diagnostics."
                ),
                new Business(
                    id: "convenience_store",
                    name: "QuickStop Convenience Store",
                    baseCost: Currency.FromDollars(12500.00),
                    baseHourlyYield: Currency.FromDollars(220.00),
                    isOwned: false,
                    level: 1,
                    category: BusinessCategory.Retail,
                    setupCost: Currency.FromDollars(1200.00),
                    reputation: 65,
                    condition: 100,
                    dailyOperatingExpenses: Currency.FromDollars(50.00),
                    description: "High foot-traffic retail store selling packaged groceries, cold beverages, and daily necessities."
                ),
                new Business(
                    id: "digital_agency",
                    name: "Apex Digital Marketing Agency",
                    baseCost: Currency.FromDollars(25000.00),
                    baseHourlyYield: Currency.FromDollars(450.00),
                    isOwned: false,
                    level: 1,
                    category: BusinessCategory.DigitalServices,
                    setupCost: Currency.FromDollars(2500.00),
                    reputation: 70,
                    condition: 100,
                    dailyOperatingExpenses: Currency.FromDollars(85.00),
                    description: "Performance marketing and web development agency executing high-ticket enterprise client campaigns."
                )
            };
        }

        private void InitializeDefaultSubsystems()
        {
            // 1. Auto Repair service bays (Level 1 starts with 1 bay)
            _autoRepairBays.Clear();
            _autoRepairBays.Add(new ServiceBay(1));

            // Default available repair jobs
            _availableRepairJobs.Clear();
            _availableRepairJobs.Add(new VehicleRepairJob("job_oil_1", "Sedan Cruiser", "Marcus Vance", RepairCategory.Maintenance, requiredLevel: 1, requiredMinutes: 60, partsCost: Currency.FromDollars(25.00), customerPay: Currency.FromDollars(90.00), reputationGain: 2));
            _availableRepairJobs.Add(new VehicleRepairJob("job_brake_1", "Compact City GT", "Elena Rostova", RepairCategory.Brakes, requiredLevel: 1, requiredMinutes: 120, partsCost: Currency.FromDollars(60.00), customerPay: Currency.FromDollars(220.00), reputationGain: 3));
            _availableRepairJobs.Add(new VehicleRepairJob("job_diag_1", "SUV Pathfinder", "David Sterling", RepairCategory.Diagnostics, requiredLevel: 2, requiredMinutes: 180, partsCost: Currency.FromDollars(40.00), customerPay: Currency.FromDollars(380.00), reputationGain: 4));
            _availableRepairJobs.Add(new VehicleRepairJob("job_engine_1", "Executive V8", "Claire DuPont", RepairCategory.Powertrain, requiredLevel: 3, requiredMinutes: 360, partsCost: Currency.FromDollars(250.00), customerPay: Currency.FromDollars(1200.00), reputationGain: 6));

            // 2. Convenience Store Inventory
            _storeInventory.Clear();
            _storeInventory.Add(new StoreProduct("inv_beverages", "Chilled Soft Drinks & Energy Drinks", wholesaleCost: Currency.FromDollars(1.20), sellingPrice: Currency.FromDollars(2.50), stockOnHand: 50, maxCapacity: 200));
            _storeInventory.Add(new StoreProduct("inv_groceries", "Packaged Snacks & Groceries", wholesaleCost: Currency.FromDollars(3.50), sellingPrice: Currency.FromDollars(6.00), stockOnHand: 40, maxCapacity: 150));
            _storeInventory.Add(new StoreProduct("inv_necessities", "Daily Household Goods & Toiletries", wholesaleCost: Currency.FromDollars(4.00), sellingPrice: Currency.FromDollars(8.00), stockOnHand: 30, maxCapacity: 100));

            // 3. Digital Agency Client Contracts
            _agencyProjects.Clear();
            _agencyProjects.Add(new ClientProject("proj_social_1", "Organic Blend Foods", AgencyProjectCategory.SocialMedia, totalHoursRequired: 20, deadlineDays: 7, contractPayment: Currency.FromDollars(1500.00), reputationGain: 4, requiredSkill: SkillType.Communication, requiredSkillLevel: 15));
            _agencyProjects.Add(new ClientProject("proj_seo_1", "Urban Retail Outlets", AgencyProjectCategory.SEO, totalHoursRequired: 30, deadlineDays: 10, contractPayment: Currency.FromDollars(2400.00), reputationGain: 5, requiredSkill: SkillType.Analytics, requiredSkillLevel: 20));
            _agencyProjects.Add(new ClientProject("proj_web_1", "Horizon Financial Advisory", AgencyProjectCategory.WebDevelopment, totalHoursRequired: 50, deadlineDays: 14, contractPayment: Currency.FromDollars(4800.00), reputationGain: 8, requiredSkill: SkillType.Technical, requiredSkillLevel: 30));
            _agencyProjects.Add(new ClientProject("proj_growth_1", "OmniGlobal Logistics", AgencyProjectCategory.AdCampaign, totalHoursRequired: 80, deadlineDays: 21, contractPayment: Currency.FromDollars(9500.00), reputationGain: 12, requiredSkill: SkillType.Analytics, requiredSkillLevel: 45));

            // 4. Employee Candidate Pool
            _candidatePool.Clear();
            _candidatePool.Add(new Employee("emp_mech_1", "Trevor Knox", "auto_repair", EmployeeRole.ApprenticeMechanic, Currency.FromDollars(1800.00), productivity: 1.0));
            _candidatePool.Add(new Employee("emp_mech_2", "Sasha Petrov", "auto_repair", EmployeeRole.SeniorTechnician, Currency.FromDollars(3200.00), productivity: 1.4));
            _candidatePool.Add(new Employee("emp_clerk_1", "Maya Lin", "convenience_store", EmployeeRole.StoreClerk, Currency.FromDollars(1600.00), productivity: 1.0));
            _candidatePool.Add(new Employee("emp_inv_1", "Jordan Hayes", "convenience_store", EmployeeRole.InventoryManager, Currency.FromDollars(2400.00), productivity: 1.3));
            _candidatePool.Add(new Employee("emp_mkt_1", "Chloe Rivera", "digital_agency", EmployeeRole.JuniorMarketer, Currency.FromDollars(2600.00), productivity: 1.1));
            _candidatePool.Add(new Employee("emp_mkt_2", "Darius Vance", "digital_agency", EmployeeRole.SeniorStrategist, Currency.FromDollars(4500.00), productivity: 1.5));
        }

        public Business GetBusiness(string businessId)
        {
            return _catalog.FirstOrDefault(b => b.Id == businessId);
        }

        public bool CanPurchase(string businessId, out string reason)
        {
            var biz = GetBusiness(businessId);
            if (biz == null)
            {
                reason = $"Business with ID '{businessId}' does not exist.";
                return false;
            }

            if (biz.IsOwned)
            {
                reason = $"'{biz.Name}' is already owned.";
                return false;
            }

            Currency totalCost = biz.GetPurchaseCost();
            if (totalCost.MinorUnits > FinancialService.Wallet.Balance.MinorUnits)
            {
                reason = $"Insufficient funds to purchase {biz.Name}: Requires {totalCost}, current cash is {FinancialService.Wallet.Balance}.";
                return false;
            }

            reason = null;
            return true;
        }

        public bool TryPurchase(string businessId, out string error)
        {
            if (!CanPurchase(businessId, out error))
            {
                return false;
            }

            var biz = GetBusiness(businessId);
            Currency totalCost = biz.GetPurchaseCost();

            bool paid = FinancialService.TryRecordExpense(
                totalCost,
                TransactionCategory.AssetPurchase,
                $"Acquisition of {biz.Name} (Base: {biz.BaseCost}, Setup: {biz.SetupCost})",
                out _,
                out error,
                biz.Id
            );

            if (!paid)
            {
                return false;
            }

            biz.Acquire();
            UpdateBalanceSheetAssets();

            error = null;
            OnBusinessPurchased?.Invoke(biz);
            return true;
        }

        public bool TrySell(string businessId, out Currency proceeds, out string error)
        {
            var biz = GetBusiness(businessId);
            if (biz == null || !biz.IsOwned)
            {
                proceeds = Currency.Zero;
                error = "Cannot sell a business that you do not own.";
                return false;
            }

            proceeds = biz.CalculateValuation();

            FinancialService.RecordIncome(
                proceeds,
                TransactionCategory.AssetSale,
                $"Liquidation / Sale of {biz.Name}",
                biz.Id
            );

            biz.Sell();
            UpdateBalanceSheetAssets();

            error = null;
            OnBusinessSold?.Invoke(biz, proceeds);
            return true;
        }

        public bool TryUpgrade(string businessId, out string error)
        {
            var biz = GetBusiness(businessId);
            if (biz == null || !biz.IsOwned)
            {
                error = "Cannot upgrade a business that you do not own.";
                return false;
            }

            Currency upgradeCost = biz.GetUpgradeCost();
            if (upgradeCost.MinorUnits > FinancialService.Wallet.Balance.MinorUnits)
            {
                error = $"Insufficient funds for upgrade: Requires {upgradeCost}, available cash is {FinancialService.Wallet.Balance}.";
                return false;
            }

            bool paid = FinancialService.TryRecordExpense(
                upgradeCost,
                TransactionCategory.AssetPurchase,
                $"Facility & Equipment Upgrade: {biz.Name} to Level {biz.Level + 1}",
                out _,
                out error,
                biz.Id
            );

            if (!paid)
            {
                return false;
            }

            biz.Upgrade();

            // Upgrade Auto Repair service bays
            if (biz.Id == "auto_repair" && _autoRepairBays.Count < biz.Level)
            {
                _autoRepairBays.Add(new ServiceBay(_autoRepairBays.Count + 1));
            }

            UpdateBalanceSheetAssets();

            error = null;
            OnBusinessUpgraded?.Invoke(biz);
            return true;
        }

        public void ToggleOpen(string businessId, bool open)
        {
            var biz = GetBusiness(businessId);
            if (biz != null && biz.IsOwned)
            {
                biz.SetOpen(open);
            }
        }

        public void UpdateBalanceSheetAssets()
        {
            FinancialService.BalanceSheet.UpdateBusinessAssets(TotalBusinessValuation);
        }

        #region State Restoration

        public void RestoreOwnedBusiness(string businessId, int level, int reputation, int condition, bool isOpen, int completedTasks)
        {
            var biz = GetBusiness(businessId);
            if (biz != null)
            {
                biz.RestoreOwnedState(level, reputation, condition, isOpen, completedTasks);
                if (biz.Id == "auto_repair")
                {
                    while (_autoRepairBays.Count < biz.Level)
                    {
                        _autoRepairBays.Add(new ServiceBay(_autoRepairBays.Count + 1));
                    }
                }
                UpdateBalanceSheetAssets();
            }
        }

        public void RestoreEmployee(Employee employee)
        {
            if (employee == null) return;
            var biz = GetBusiness(employee.BusinessId);
            if (biz != null && biz.IsOwned)
            {
                biz.HireEmployee(employee);
            }
        }

        public void RestoreStoreProduct(string productId, int stockOnHand, Currency sellingPrice, int totalUnitsSold, int missedSales)
        {
            var product = _storeInventory.FirstOrDefault(p => p.Id == productId);
            product?.RestoreInventory(stockOnHand, sellingPrice, totalUnitsSold, missedSales);
        }

        public void RestoreAgencyProject(ClientProject restoredProject)
        {
            if (restoredProject == null) return;
            int idx = _agencyProjects.FindIndex(p => p.ProjectId == restoredProject.ProjectId);
            if (idx >= 0)
            {
                _agencyProjects[idx] = restoredProject;
            }
            else
            {
                _agencyProjects.Add(restoredProject);
            }
        }

        #endregion

        #region Auto Repair Operations

        public bool TryStartRepairJob(string jobId, int bayId, out string error)
        {
            var biz = GetBusiness("auto_repair");
            if (biz == null || !biz.IsOwned || !biz.IsOpen)
            {
                error = "Auto Repair Workshop must be owned and open.";
                return false;
            }

            var job = _availableRepairJobs.FirstOrDefault(j => j.JobId == jobId && !j.IsCompleted);
            if (job == null)
            {
                error = "Repair job not found in queue.";
                return false;
            }

            if (biz.Level < job.RequiredLevel)
            {
                error = $"Workshop equipment Level {job.RequiredLevel} required (Current: Level {biz.Level}).";
                return false;
            }

            var bay = _autoRepairBays.FirstOrDefault(b => b.BayId == bayId);
            if (bay == null || bay.IsOccupied)
            {
                error = $"Service Bay {bayId} is occupied or unavailable.";
                return false;
            }

            // Deduct parts expense
            if (job.PartsCost.MinorUnits > 0)
            {
                bool paid = FinancialService.TryRecordExpense(
                    job.PartsCost,
                    TransactionCategory.BusinessExpense,
                    $"Repair Parts: {job.Category} for {job.VehicleModel}",
                    out _,
                    out error,
                    biz.Id
                );

                if (!paid)
                {
                    return false;
                }

                biz.PAndL.RecordOperatingExpense(job.PartsCost);
            }

            bay.TryAssignJob(job);
            error = null;
            return true;
        }

        public bool DeliverFinishedVehicle(string jobId, out Currency payment, out string error)
        {
            payment = Currency.Zero;
            var biz = GetBusiness("auto_repair");
            if (biz == null || !biz.IsOwned)
            {
                error = "Auto Repair Workshop is not owned.";
                return false;
            }

            var bay = _autoRepairBays.FirstOrDefault(b => b.CurrentJob != null && b.CurrentJob.JobId == jobId);
            if (bay == null || !bay.CurrentJob.IsCompleted || bay.CurrentJob.IsDelivered)
            {
                error = "Job is either not completed or already delivered.";
                return false;
            }

            var job = bay.CurrentJob;
            job.Deliver();
            bay.Clear();

            payment = job.CustomerPay;
            FinancialService.RecordIncome(
                payment,
                TransactionCategory.BusinessRevenue,
                $"Vehicle Repair Payout: {job.VehicleModel} ({job.Category})",
                biz.Id
            );

            biz.PAndL.RecordRevenue(payment);
            biz.RecordCompletedTask();
            biz.SetReputation(biz.Reputation + job.ReputationGain);

            OnRepairJobCompleted?.Invoke(job);
            error = null;
            return true;
        }

        public bool DeliverFinishedVehicle(int bayId, out Currency payment, out string error)
        {
            var bay = _autoRepairBays.FirstOrDefault(b => b.BayId == bayId);
            if (bay == null || bay.CurrentJob == null)
            {
                payment = Currency.Zero;
                error = $"No active job in Service Bay {bayId}.";
                return false;
            }
            return DeliverFinishedVehicle(bay.CurrentJob.JobId, out payment, out error);
        }

        public bool TryClaimCompletedRepair(int bayId, out Currency payment, out string error)
            => DeliverFinishedVehicle(bayId, out payment, out error);

        #endregion

        #region Convenience Store Operations

        public bool OrderWholesaleStock(string productId, int units, out Currency totalCost, out string error)
        {
            totalCost = Currency.Zero;
            var biz = GetBusiness("convenience_store");
            if (biz == null || !biz.IsOwned)
            {
                error = "Convenience Store is not owned.";
                return false;
            }

            var product = _storeInventory.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                error = "Store product line not found.";
                return false;
            }

            totalCost = product.WholesaleCost * units;
            if (totalCost.MinorUnits > FinancialService.Wallet.Balance.MinorUnits)
            {
                error = $"Insufficient funds for wholesale stock: Requires {totalCost}, cash is {FinancialService.Wallet.Balance}.";
                return false;
            }

            bool paid = FinancialService.TryRecordExpense(
                totalCost,
                TransactionCategory.BusinessExpense,
                $"Wholesale Restock: {units} units of {product.Name}",
                out _,
                out error,
                biz.Id
            );

            if (!paid)
            {
                return false;
            }

            int added = product.Restock(units);
            biz.PAndL.RecordOperatingExpense(totalCost);

            error = null;
            return true;
        }

        public void SetProductSellingPrice(string productId, Currency newPrice)
        {
            var product = _storeInventory.FirstOrDefault(p => p.Id == productId);
            product?.SetPrice(newPrice);
        }

        #endregion

        #region Digital Agency Operations

        public bool TryAcceptClientProject(string projectId, out string error)
        {
            var biz = GetBusiness("digital_agency");
            if (biz == null || !biz.IsOwned || !biz.IsOpen)
            {
                error = "Digital Agency must be owned and operational.";
                return false;
            }

            var project = _agencyProjects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || project.IsAccepted)
            {
                error = "Project contract unavailable or already active.";
                return false;
            }

            project.Accept();
            error = null;
            return true;
        }

        public bool ContributeAgencyWorkHours(string projectId, int hours)
        {
            var project = _agencyProjects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project != null && project.IsAccepted && !project.IsCompleted)
            {
                return project.ContributeWorkHours(hours);
            }
            return false;
        }

        public bool ClaimCompletedProject(string projectId, out Currency payout, out string error)
        {
            payout = Currency.Zero;
            var biz = GetBusiness("digital_agency");
            if (biz == null || !biz.IsOwned)
            {
                error = "Digital Agency is not owned.";
                return false;
            }

            var project = _agencyProjects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || !project.IsCompleted)
            {
                error = "Project contract is not yet completed.";
                return false;
            }

            payout = project.ContractPayment;
            FinancialService.RecordIncome(
                payout,
                TransactionCategory.BusinessRevenue,
                $"Client Project Fee: {project.ClientName} ({project.Category})",
                biz.Id
            );

            biz.PAndL.RecordRevenue(payout);
            biz.SetReputation(biz.Reputation + project.ReputationGain);

            OnAgencyProjectCompleted?.Invoke(project);
            error = null;
            return true;
        }

        #endregion

        #region Employee Hiring & Dismissal

        public bool TryHireEmployee(string businessId, string candidateId, out string error)
        {
            var candidate = _candidatePool.FirstOrDefault(c => c.Id == candidateId);
            if (candidate == null)
            {
                error = "Candidate not found in recruitment pool.";
                return false;
            }
            return TryHireEmployee(businessId, candidate, out error);
        }

        public bool TryHireEmployee(string businessId, Employee candidate, out string error)
        {
            var biz = GetBusiness(businessId);
            if (biz == null || !biz.IsOwned)
            {
                error = "Business is not owned.";
                return false;
            }

            if (candidate == null)
            {
                error = "Invalid candidate.";
                return false;
            }

            biz.HireEmployee(candidate);
            _candidatePool.Remove(candidate);

            OnEmployeeHired?.Invoke(candidate);
            error = null;
            return true;
        }

        public bool TryDismissEmployee(string businessId, string employeeId, out string error)
        {
            var biz = GetBusiness(businessId);
            if (biz == null || !biz.IsOwned)
            {
                error = "Business is not owned.";
                return false;
            }

            var emp = biz.Employees.FirstOrDefault(e => e.Id == employeeId);
            if (emp == null)
            {
                error = "Employee not found on roster.";
                return false;
            }

            biz.DismissEmployee(employeeId);
            OnEmployeeDismissed?.Invoke(emp);
            error = null;
            return true;
        }

        #endregion

        #region Daily Simulation & Scheduled Operations

        private void ProcessDailyBusinessOperations(GameDateTime currentDate)
        {
            foreach (var biz in OwnedBusinesses)
            {
                if (!biz.IsOpen) continue;

                // 1. Deduct daily base operating overhead (utilities, rent, software licenses)
                if (biz.DailyOperatingExpenses.MinorUnits > 0)
                {
                    bool paid = FinancialService.TryRecordExpense(
                        biz.DailyOperatingExpenses,
                        TransactionCategory.BusinessExpense,
                        $"Daily Overhead: {biz.Name}",
                        out _,
                        out _,
                        biz.Id
                    );

                    if (paid)
                    {
                        biz.PAndL.RecordOperatingExpense(biz.DailyOperatingExpenses);
                    }
                }

                // 2. Specialized daily mechanics by category
                if (biz.Category == BusinessCategory.Automotive)
                {
                    OperateAutoRepairDaily(biz);
                }
                else if (biz.Category == BusinessCategory.Retail)
                {
                    OperateConvenienceStoreDaily(biz);
                }
                else if (biz.Category == BusinessCategory.DigitalServices)
                {
                    OperateDigitalAgencyDaily(biz);
                }
            }

            // 3. Monthly Payroll on Day 30
            if (currentDate.Day == 30)
            {
                ProcessMonthlyBusinessPayroll(currentDate);
            }

            // 4. Update balance sheet non-cash asset valuation
            UpdateBalanceSheetAssets();
        }

        private void OnClockMinutePassed(GameDateTime time)
        {
            var autoRepair = GetBusiness("auto_repair");
            if (autoRepair != null && autoRepair.IsOwned && autoRepair.IsOpen)
            {
                foreach (var bay in _autoRepairBays)
                {
                    if (bay.IsOccupied && !bay.CurrentJob.IsCompleted)
                    {
                        bay.CurrentJob.Progress(1);
                    }
                }
            }
        }

        private void OperateAutoRepairDaily(Business biz)
        {
            // Mechanics contribute work minutes
            int mechanicMinutes = 120 + (biz.EmployeeCount * 180);

            foreach (var bay in _autoRepairBays)
            {
                if (bay.IsOccupied && !bay.CurrentJob.IsCompleted)
                {
                    bay.CurrentJob.Progress(mechanicMinutes);
                }
            }
        }

        private void OperateConvenienceStoreDaily(Business biz)
        {
            // Foot traffic demand scaled by level, reputation, and store clerks
            int baseDemandPerItem = 10 + (biz.Level * 5) + (biz.Reputation / 10) + (biz.EmployeeCount * 8);

            foreach (var product in _storeInventory)
            {
                var salesResult = product.ProcessCustomerDemand(baseDemandPerItem);
                if (salesResult.UnitsSold > 0)
                {
                    FinancialService.RecordIncome(
                        salesResult.Revenue,
                        TransactionCategory.BusinessRevenue,
                        $"Retail Sales: {salesResult.UnitsSold}x {product.Name}",
                        biz.Id
                    );

                    biz.PAndL.RecordRevenue(salesResult.Revenue);
                    biz.PAndL.RecordCOGS(salesResult.CostOfGoodsSold);
                }

                if (salesResult.MissedSales > 0)
                {
                    // Stockout penalty to reputation
                    biz.SetReputation(biz.Reputation - 2);
                }

                OnStoreDailySalesProcessed?.Invoke(salesResult);
            }
        }

        private void OperateDigitalAgencyDaily(Business biz)
        {
            // Production capacity: 8 hrs/day base + 8 hrs/day per employee
            int dailyAgencyHours = 8 + (biz.EmployeeCount * 8);

            foreach (var project in _agencyProjects)
            {
                if (project.IsAccepted && !project.IsCompleted && !project.IsFailed)
                {
                    project.ContributeWorkHours(dailyAgencyHours);
                    bool deadlineMissed = project.AdvanceDay();

                    if (deadlineMissed)
                    {
                        // Penalty for missed deadline
                        biz.SetReputation(biz.Reputation - 10);
                    }
                    else if (project.IsCompleted)
                    {
                        ClaimCompletedProject(project.ProjectId, out _, out _);
                    }
                }
            }
        }

        private void ProcessMonthlyBusinessPayroll(GameDateTime currentDate)
        {
            foreach (var biz in OwnedBusinesses)
            {
                foreach (var emp in biz.Employees.Where(e => e.IsActive))
                {
                    bool paid = FinancialService.TryRecordExpense(
                        emp.MonthlySalary,
                        TransactionCategory.BusinessExpense,
                        $"Employee Payroll: {emp.Name} ({emp.Role}) at {biz.Name}",
                        out _,
                        out _,
                        biz.Id
                    );

                    if (paid)
                    {
                        biz.PAndL.RecordPayroll(emp.MonthlySalary);
                    }
                }
            }
        }

        #endregion
    }
}
