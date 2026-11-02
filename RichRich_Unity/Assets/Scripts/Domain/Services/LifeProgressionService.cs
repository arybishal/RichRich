using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;

namespace RichRich.Domain.Services
{
    /// <summary>
    /// Service orchestrating life achievements, wealth milestone goals,
    /// financial freedom (FIRE) metrics, and dynamic macroeconomic life events.
    /// Fully integrated with the central FinancialLedger, Wallet, and BalanceSheet.
    /// </summary>
    public class LifeProgressionService
    {
        private readonly List<MilestoneGoal> _goals = new List<MilestoneGoal>();
        private readonly List<DynamicLifeEvent> _eventPool = new List<DynamicLifeEvent>();
        private readonly Random _rng;

        public Wallet Wallet { get; }
        public FinancialLedger Ledger { get; }
        public FinancialService FinancialService { get; }
        public CareerService CareerService { get; }
        public BusinessService BusinessService { get; }
        public RealEstateService RealEstateService { get; }
        public StockMarketService StockMarketService { get; }
        public LivingExpenseService ExpenseService { get; }
        public SimulationClock Clock { get; }

        public IReadOnlyList<MilestoneGoal> Goals => _goals.AsReadOnly();
        public IReadOnlyList<MilestoneGoal> CompletedGoals => _goals.Where(g => g.IsCompleted).ToList().AsReadOnly();
        public IReadOnlyList<MilestoneGoal> ActiveGoals => _goals.Where(g => !g.IsCompleted).ToList().AsReadOnly();

        public DynamicLifeEvent CurrentPendingEvent { get; private set; }
        public int DaysUntilNextEvent { get; private set; } = 25;

        public double PassiveIncomeRatio
        {
            get
            {
                long monthlyExpenses = ExpenseService?.TotalMonthlyBudget.MinorUnits ?? 1L;
                if (monthlyExpenses <= 0) return 1.0;

                long passiveIncome = 0L;
                if (RealEstateService != null)
                {
                    passiveIncome += RealEstateService.TotalMonthlyRentalIncome.MinorUnits;
                }
                if (StockMarketService != null)
                {
                    // Approximate monthly dividend yield
                    long portfolioCents = StockMarketService.TotalPortfolioValuation.MinorUnits;
                    passiveIncome += (long)(portfolioCents * 0.03 / 12.0);
                }

                return (double)passiveIncome / monthlyExpenses;
            }
        }

        public bool IsFireAchieved => PassiveIncomeRatio >= 1.0;

        public event Action<MilestoneGoal> OnGoalCompleted;
        public event Action<DynamicLifeEvent> OnEventTriggered;
        public event Action<string> OnEventResolved;
        public event Action OnProgressionUpdated;

        public LifeProgressionService(
            Wallet wallet,
            FinancialLedger ledger,
            FinancialService financialService,
            CareerService careerService,
            BusinessService businessService,
            RealEstateService realEstateService,
            StockMarketService stockMarketService,
            LivingExpenseService expenseService,
            SimulationClock clock = null,
            IEnumerable<MilestoneGoal> goals = null,
            int? rngSeed = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            FinancialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
            CareerService = careerService;
            BusinessService = businessService;
            RealEstateService = realEstateService;
            StockMarketService = stockMarketService;
            ExpenseService = expenseService;
            Clock = clock;
            _rng = rngSeed.HasValue ? new Random(rngSeed.Value) : new Random();

            if (goals != null)
            {
                _goals.AddRange(goals);
            }
            else
            {
                _goals.AddRange(MilestoneGoal.GetDefaultGoals());
            }

            InitializeEventsPool();

            if (Clock != null)
            {
                Clock.OnDayPassed += _ => OnDayPassed();
            }

            EvaluateGoals();
        }

        private void InitializeEventsPool()
        {
            _eventPool.Add(new DynamicLifeEvent(
                "event_seed_syndicate",
                "Angel Investment Opportunity",
                "A promising fintech startup is opening a friends-and-family seed round. You can participate with a $5,000 check.",
                EventSeverity.Opportunity,
                new List<EventChoice>
                {
                    new EventChoice("invest_seed", "Invest $5,000", "75% chance of 3x buyout exit, 25% total loss.", Currency.FromDollars(5000), Currency.FromDollars(15000), energyImpact: -10, successProbability: 0.75),
                    new EventChoice("pass_seed", "Pass on Deal", "Preserve your liquidity safely.", Currency.Zero, Currency.Zero)
                }
            ));

            _eventPool.Add(new DynamicLifeEvent(
                "event_industry_summit",
                "Executive Keynote Invitation",
                "You have been invited to deliver the keynote address at the Metropolitan Commerce Summit.",
                EventSeverity.Opportunity,
                new List<EventChoice>
                {
                    new EventChoice("accept_keynote", "Deliver Keynote", "Earn $3,500 speaker honorarium and boost industry reputation.", Currency.Zero, Currency.FromDollars(3500), energyImpact: -25),
                    new EventChoice("decline_keynote", "Decline Gracefully", "Conserve your daily energy.", Currency.Zero, Currency.Zero)
                }
            ));

            _eventPool.Add(new DynamicLifeEvent(
                "event_supply_chain",
                "Global Logistics Surcharge",
                "International maritime delays have increased inventory freight surcharges across wholesale suppliers.",
                EventSeverity.Warning,
                new List<EventChoice>
                {
                    new EventChoice("pay_freight", "Pay Expedited Airfreight", "Cover $2,000 surcharge to keep retail shelves fully stocked.", Currency.FromDollars(2000), Currency.Zero),
                    new EventChoice("accept_delay", "Accept Shipment Delays", "Save cash, but risk short-term retail inventory stockouts.", Currency.Zero, Currency.Zero, energyImpact: -5)
                }
            ));

            _eventPool.Add(new DynamicLifeEvent(
                "event_rd_rebate",
                "Government Innovation Tax Credit",
                "The corporate revenue authority has approved your enterprise innovation and equipment depreciation tax rebate.",
                EventSeverity.Info,
                new List<EventChoice>
                {
                    new EventChoice("claim_rebate", "Deposit $6,000 Tax Credit", "Directly deposit the tax credit into your operating account.", Currency.Zero, Currency.FromDollars(6000))
                }
            ));
        }

        public void EvaluateGoals()
        {
            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);
            long netWorth = FinancialService?.BalanceSheet?.NetWorthCents ?? 0L;
            long salary = CareerService?.CurrentMonthlySalary.MinorUnits ?? 0L;
            int properties = RealEstateService?.OwnedProperties.Count ?? 0;
            int businesses = BusinessService?.OwnedBusinesses.Count ?? 0;
            long portfolio = StockMarketService?.TotalPortfolioValuation.MinorUnits ?? 0L;

            foreach (var goal in _goals)
            {
                if (goal.IsCompleted) continue;

                bool completed = false;
                switch (goal.Id)
                {
                    case "goal_first_capital":
                        completed = goal.UpdateProgress(netWorth, currentDate);
                        break;
                    case "goal_career_lead":
                        completed = goal.UpdateProgress(salary, currentDate);
                        break;
                    case "goal_landlord":
                        completed = goal.UpdateProgress(properties, currentDate);
                        break;
                    case "goal_multi_biz":
                        completed = goal.UpdateProgress(businesses, currentDate);
                        break;
                    case "goal_market_fund":
                        completed = goal.UpdateProgress(portfolio, currentDate);
                        break;
                    case "goal_fire_freedom":
                        completed = goal.UpdateProgress(IsFireAchieved ? 1L : 0L, currentDate);
                        break;
                    case "goal_millionaire":
                        completed = goal.UpdateProgress(netWorth, currentDate);
                        break;
                }

                if (completed)
                {
                    AwardGoalReward(goal, currentDate);
                }
            }

            OnProgressionUpdated?.Invoke();
        }

        private void AwardGoalReward(MilestoneGoal goal, GameDateTime currentDate)
        {
            if (goal.RewardCash.MinorUnits > 0)
            {
                Wallet.Add(goal.RewardCash);
                Ledger.Record(new FinancialTransaction(
                    Guid.NewGuid().ToString("N"),
                    goal.RewardCash,
                    TransactionType.Income,
                    TransactionCategory.Miscellaneous,
                    currentDate,
                    $"Milestone achievement award: '{goal.Title}'",
                    goal.Id
                ));
            }

            OnGoalCompleted?.Invoke(goal);
        }

        public void OnDayPassed()
        {
            EvaluateGoals();

            DaysUntilNextEvent--;
            if (DaysUntilNextEvent <= 0 && CurrentPendingEvent == null)
            {
                TriggerRandomEvent();
                DaysUntilNextEvent = _rng.Next(25, 45);
            }
        }

        public void TriggerRandomEvent()
        {
            if (_eventPool.Count == 0) return;
            int idx = _rng.Next(0, _eventPool.Count);
            CurrentPendingEvent = _eventPool[idx];
            OnEventTriggered?.Invoke(CurrentPendingEvent);
            OnProgressionUpdated?.Invoke();
        }

        public bool ResolveCurrentEventChoice(string choiceId, out string outcomeMessage, out string error)
        {
            outcomeMessage = "";
            error = null;

            if (CurrentPendingEvent == null)
            {
                error = "No active event pending.";
                return false;
            }

            var choice = CurrentPendingEvent.Choices.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null)
            {
                error = $"Choice {choiceId} not recognized.";
                return false;
            }

            var currentDate = Clock?.CurrentDate ?? new GameDateTime(2026, 1, 1);

            // Deduct cash cost if required
            if (choice.CashCost.MinorUnits > 0)
            {
                if (!Wallet.TrySpend(choice.CashCost, out string spendError))
                {
                    error = spendError ?? $"Insufficient funds for choice: Requires {choice.CashCost}.";
                    return false;
                }

                Ledger.Record(new FinancialTransaction(
                    Guid.NewGuid().ToString("N"),
                    choice.CashCost,
                    TransactionType.Expense,
                    TransactionCategory.Miscellaneous,
                    currentDate,
                    $"Event Choice: {CurrentPendingEvent.Title} - {choice.Label}",
                    CurrentPendingEvent.Id
                ));
            }

            // Determine payoff
            bool isSuccess = _rng.NextDouble() <= choice.SuccessProbability;
            if (isSuccess && choice.CashReward.MinorUnits > 0)
            {
                Wallet.Add(choice.CashReward);
                Ledger.Record(new FinancialTransaction(
                    Guid.NewGuid().ToString("N"),
                    choice.CashReward,
                    TransactionType.Income,
                    TransactionCategory.Miscellaneous,
                    currentDate,
                    $"Event Return: {CurrentPendingEvent.Title} - {choice.Label}",
                    CurrentPendingEvent.Id
                ));

                outcomeMessage = $"Outcome: Success! Received {choice.CashReward}. {choice.ConsequenceText}";
            }
            else if (!isSuccess)
            {
                outcomeMessage = "Outcome: The venture did not pan out. Capital was lost, but valuable experience was gained.";
            }
            else
            {
                outcomeMessage = choice.ConsequenceText;
            }

            CurrentPendingEvent = null;
            EvaluateGoals();
            OnEventResolved?.Invoke(outcomeMessage);
            OnProgressionUpdated?.Invoke();
            return true;
        }

        #region State Restoration

        public void RestoreGoalCompletion(string goalId, bool isCompleted, long completedUnix)
        {
            var goal = _goals.FirstOrDefault(g => g.Id == goalId);
            if (goal != null && isCompleted)
            {
                var dt = completedUnix > 0 ? (GameDateTime?)new GameDateTime(completedUnix) : null;
                goal.UpdateProgress(goal.TargetValue, dt ?? new GameDateTime(2026, 1, 1));
            }
        }

        #endregion
    }
}
