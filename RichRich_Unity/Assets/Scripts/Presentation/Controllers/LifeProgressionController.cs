using System;
using System.Collections.Generic;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller for Life Progression, Milestone Goals, FIRE metrics,
    /// and dynamic macroeconomic life events.
    /// Pure C# controller with zero direct UnityEngine dependencies.
    /// </summary>
    public class LifeProgressionController
    {
        private readonly LifeSimulationCoordinator _coordinator;

        public LifeProgressionService ProgressionService => _coordinator?.LifeProgressionService;
        public SimulationClock Clock => _coordinator?.Clock;
        public Wallet Wallet => _coordinator?.Wallet;

        public IReadOnlyList<MilestoneGoal> Goals => ProgressionService?.Goals ?? Array.Empty<MilestoneGoal>();
        public IReadOnlyList<MilestoneGoal> CompletedGoals => ProgressionService?.CompletedGoals ?? Array.Empty<MilestoneGoal>();
        public IReadOnlyList<MilestoneGoal> ActiveGoals => ProgressionService?.ActiveGoals ?? Array.Empty<MilestoneGoal>();

        public DynamicLifeEvent CurrentPendingEvent => ProgressionService?.CurrentPendingEvent;
        public bool HasPendingEvent => CurrentPendingEvent != null;

        public double FireRatio => ProgressionService?.PassiveIncomeRatio ?? 0.0;
        public bool IsFireAchieved => ProgressionService?.IsFireAchieved ?? false;

        public string FireProgressText
        {
            get
            {
                if (ProgressionService == null) return "0% FIRE";
                double pct = Math.Min(ProgressionService.PassiveIncomeRatio * 100.0, 999.0);
                return $"{pct:F0}% FIRE Target";
            }
        }

        public string FireSummaryText
        {
            get
            {
                if (ProgressionService == null) return "";
                long monthlyExp = ProgressionService.ExpenseService?.TotalMonthlyBudget.MinorUnits ?? 0L;
                long passive = 0L;
                if (ProgressionService.RealEstateService != null)
                {
                    passive += ProgressionService.RealEstateService.TotalMonthlyRentalIncome.MinorUnits;
                }
                if (ProgressionService.StockMarketService != null)
                {
                    long pVal = ProgressionService.StockMarketService.TotalPortfolioValuation.MinorUnits;
                    passive += (long)(pVal * 0.03 / 12.0);
                }

                return $"Passive Income: {Currency.FromCents(passive)} / mo (Expenses: {Currency.FromCents(monthlyExp)} / mo)";
            }
        }

        public string GoalsAchievedSummary
        {
            get
            {
                int completed = CompletedGoals.Count;
                int total = Goals.Count;
                return $"{completed}/{total} Milestones Unlocked";
            }
        }

        public event Action OnStateChanged;
        public event Action<MilestoneGoal> OnGoalCompleted;
        public event Action<DynamicLifeEvent> OnEventTriggered;
        public event Action<string> OnEventOutcome;

        public LifeProgressionController(LifeSimulationCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            if (ProgressionService != null)
            {
                ProgressionService.OnProgressionUpdated += () => OnStateChanged?.Invoke();
                ProgressionService.OnGoalCompleted += goal =>
                {
                    OnGoalCompleted?.Invoke(goal);
                    OnStateChanged?.Invoke();
                };
                ProgressionService.OnEventTriggered += ev =>
                {
                    OnEventTriggered?.Invoke(ev);
                    OnStateChanged?.Invoke();
                };
                ProgressionService.OnEventResolved += msg =>
                {
                    OnEventOutcome?.Invoke(msg);
                    OnStateChanged?.Invoke();
                };
            }
        }

        public bool ChooseEventOption(string choiceId, out string outcome, out string error)
        {
            outcome = "";
            error = null;
            if (ProgressionService == null)
            {
                error = "Progression service unavailable.";
                return false;
            }

            bool success = ProgressionService.ResolveCurrentEventChoice(choiceId, out outcome, out error);
            if (success)
            {
                _coordinator.SaveCurrentState();
            }
            return success;
        }

        public void CheckMilestones()
        {
            ProgressionService?.EvaluateGoals();
        }
    }
}
