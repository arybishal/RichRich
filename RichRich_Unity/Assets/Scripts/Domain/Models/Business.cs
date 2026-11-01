using System;
using System.Collections.Generic;
using System.Linq;

namespace RichRich.Domain.Models
{
    public enum BusinessCategory
    {
        Automotive,
        Retail,
        DigitalServices,
        General
    }

    /// <summary>
    /// Financial statement tracking Profit and Loss for a business entity.
    /// </summary>
    public class BusinessPAndL
    {
        public Currency GrossRevenue { get; private set; } = Currency.Zero;
        public Currency TotalRevenue => GrossRevenue;
        public Currency CostOfGoodsSold { get; private set; } = Currency.Zero;
        public Currency OperatingExpenses { get; private set; } = Currency.Zero;
        public Currency PayrollExpenses { get; private set; } = Currency.Zero;
        public Currency EmployeePayroll => PayrollExpenses;

        public Currency GrossProfit => GrossRevenue >= CostOfGoodsSold ? GrossRevenue - CostOfGoodsSold : Currency.Zero;
        public long NetProfitCents => (long)GrossProfit.MinorUnits - (long)(OperatingExpenses + PayrollExpenses).MinorUnits;
        public bool IsProfitable => NetProfitCents > 0;

        public Currency NetProfit => NetProfitCents >= 0 ? Currency.FromCents(NetProfitCents) : Currency.Zero;
        public Currency NetLoss => NetProfitCents < 0 ? Currency.FromCents(-NetProfitCents) : Currency.Zero;

        public void RecordRevenue(Currency revenue)
        {
            GrossRevenue += revenue;
        }

        public void RecordCOGS(Currency cogs)
        {
            CostOfGoodsSold += cogs;
        }

        public void RecordOperatingExpense(Currency expense)
        {
            OperatingExpenses += expense;
        }

        public void RecordPayroll(Currency payroll)
        {
            PayrollExpenses += payroll;
        }

        public void ResetPeriod()
        {
            GrossRevenue = Currency.Zero;
            CostOfGoodsSold = Currency.Zero;
            OperatingExpenses = Currency.Zero;
            PayrollExpenses = Currency.Zero;
        }
    }

    /// <summary>
    /// Domain representation of a commercial business.
    /// Manages purchase status, level upgrades, reputation, employees, and financial P&L calculations.
    /// </summary>
    public class Business
    {
        private readonly List<Employee> _employees = new List<Employee>();

        public string Id { get; }
        public string Name { get; }
        public Currency BaseCost { get; }
        public Currency BaseHourlyYield { get; }
        public bool IsOwned { get; private set; }
        public int Level { get; private set; }
        public int CompletedTasksCount { get; private set; }

        public BusinessCategory Category { get; }
        public Currency SetupCost { get; }
        public int Reputation { get; private set; } // 0 to 100
        public int Condition { get; private set; }  // 0 to 100
        public bool IsOpen { get; private set; }
        public Currency DailyOperatingExpenses { get; private set; }
        public string Description { get; }
        public BusinessPAndL PAndL { get; } = new BusinessPAndL();

        public IReadOnlyList<Employee> Employees => _employees;
        public int EmployeeCount => _employees.Count(e => e.IsActive);
        public Currency MonthlyPayroll => Currency.FromCents(_employees.Where(e => e.IsActive).Sum(e => (long)e.MonthlySalary.MinorUnits));

        public event Action<Business> OnBusinessUpdated;

        public Business(
            string id,
            string name,
            Currency baseCost,
            Currency baseHourlyYield,
            bool isOwned = false,
            int level = 1,
            int completedTasksCount = 0,
            BusinessCategory category = BusinessCategory.Automotive,
            Currency? setupCost = null,
            int reputation = 50,
            int condition = 100,
            bool isOpen = true,
            Currency? dailyOperatingExpenses = null,
            string description = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseCost = baseCost;
            BaseHourlyYield = baseHourlyYield;
            IsOwned = isOwned;
            Level = Math.Max(1, level);
            CompletedTasksCount = Math.Max(0, completedTasksCount);

            Category = category;
            SetupCost = setupCost ?? Currency.Zero;
            Reputation = Math.Clamp(reputation, 0, 100);
            Condition = Math.Clamp(condition, 0, 100);
            IsOpen = isOpen;
            DailyOperatingExpenses = dailyOperatingExpenses ?? Currency.FromDollars(20.00);
            Description = description ?? $"{name} commercial enterprise.";
        }

        public Currency GetPurchaseCost() => BaseCost + SetupCost;

        public Currency GetUpgradeCost()
        {
            if (!IsOwned)
            {
                return BaseCost;
            }

            // Exponential scaling: BaseCost * 1.25^(Level - 1)
            double factor = Math.Pow(EconomyConfig.UpgradeCostMultiplier, Level - 1);
            return BaseCost * factor;
        }

        public Currency GetHourlyIncome()
        {
            if (!IsOwned || !IsOpen)
            {
                return Currency.Zero;
            }

            // Income = BaseHourlyYield * (1 + 0.25 * (Level - 1))
            double bonusFactor = 1.0 + (EconomyConfig.UpgradeYieldBonusPercent * (Level - 1));
            return BaseHourlyYield * bonusFactor;
        }

        public Currency CalculateValuation()
        {
            if (!IsOwned) return Currency.Zero;

            // Valuation: BaseCost * (1.0 + 0.5 * (Level - 1)) * (Reputation / 100) * (Condition / 100)
            double levelBonus = 1.0 + (0.5 * (Level - 1));
            double repMultiplier = Math.Clamp(Reputation / 100.0, 0.25, 1.25);
            double conditionMultiplier = Math.Clamp(Condition / 100.0, 0.5, 1.0);

            long baseCents = (long)(BaseCost.MinorUnits * levelBonus * repMultiplier * conditionMultiplier);
            return Currency.FromCents(baseCents);
        }

        public void Acquire()
        {
            if (IsOwned)
            {
                throw new InvalidOperationException($"Business '{Name}' is already owned.");
            }
            IsOwned = true;
            IsOpen = true;
            Level = 1;
            OnBusinessUpdated?.Invoke(this);
        }

        public void Sell()
        {
            if (!IsOwned)
            {
                throw new InvalidOperationException($"Cannot sell unowned business '{Name}'.");
            }
            IsOwned = false;
            IsOpen = false;
            _employees.Clear();
            PAndL.ResetPeriod();
            OnBusinessUpdated?.Invoke(this);
        }

        public void Upgrade()
        {
            if (!IsOwned)
            {
                throw new InvalidOperationException($"Cannot upgrade unowned business '{Name}'.");
            }
            Level++;
            Reputation = Math.Min(100, Reputation + 5);
            OnBusinessUpdated?.Invoke(this);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            OnBusinessUpdated?.Invoke(this);
        }

        public void SetReputation(int reputation)
        {
            Reputation = Math.Clamp(reputation, 0, 100);
            OnBusinessUpdated?.Invoke(this);
        }

        public void SetCondition(int condition)
        {
            Condition = Math.Clamp(condition, 0, 100);
            OnBusinessUpdated?.Invoke(this);
        }

        public void SetDailyOperatingExpenses(Currency expenses)
        {
            DailyOperatingExpenses = expenses;
            OnBusinessUpdated?.Invoke(this);
        }

        public void RecordCompletedTask()
        {
            CompletedTasksCount++;
            Reputation = Math.Min(100, Reputation + 1);
            OnBusinessUpdated?.Invoke(this);
        }

        public void HireEmployee(Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            _employees.Add(employee);
            OnBusinessUpdated?.Invoke(this);
        }

        public bool DismissEmployee(string employeeId)
        {
            var emp = _employees.FirstOrDefault(e => e.Id == employeeId);
            if (emp != null)
            {
                emp.Dismiss();
                _employees.Remove(emp);
                OnBusinessUpdated?.Invoke(this);
                return true;
            }
            return false;
        }

        public void RestoreOwnedState(int level, int reputation, int condition, bool isOpen, int completedTasks)
        {
            IsOwned = true;
            Level = Math.Max(1, level);
            Reputation = Math.Clamp(reputation, 0, 100);
            Condition = Math.Clamp(condition, 0, 100);
            IsOpen = isOpen;
            CompletedTasksCount = Math.Max(0, completedTasks);
        }
    }
}
