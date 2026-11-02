using System;
using System.Collections.Generic;

namespace RichRich.Domain.Models
{
    public enum GoalCategory
    {
        Wealth,
        Career,
        RealEstate,
        Enterprise,
        FinancialFreedom
    }

    public enum EventSeverity
    {
        Info,
        Opportunity,
        Warning,
        Crisis
    }

    /// <summary>
    /// Represents an actionable life milestone or financial achievement goal.
    /// Tracks numerical progress, completion status, and cash bonus rewards.
    /// </summary>
    public class MilestoneGoal
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public GoalCategory Category { get; }
        public long TargetValue { get; }
        public long CurrentValue { get; private set; }
        public bool IsCompleted { get; private set; }
        public GameDateTime? CompletedDate { get; private set; }
        public Currency RewardCash { get; }

        public double ProgressFraction => TargetValue > 0 ? Math.Clamp((double)CurrentValue / TargetValue, 0.0, 1.0) : 0.0;
        public string ProgressFormatted => $"{ProgressFraction * 100.0:F0}%";

        public MilestoneGoal(
            string id,
            string title,
            string description,
            GoalCategory category,
            long targetValue,
            Currency rewardCash,
            long currentValue = 0,
            bool isCompleted = false,
            GameDateTime? completedDate = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? "";
            Category = category;
            TargetValue = Math.Max(1L, targetValue);
            RewardCash = rewardCash;
            CurrentValue = currentValue;
            IsCompleted = isCompleted;
            CompletedDate = completedDate;
        }

        public bool UpdateProgress(long newValue, GameDateTime currentDate)
        {
            CurrentValue = newValue;
            if (!IsCompleted && CurrentValue >= TargetValue)
            {
                IsCompleted = true;
                CompletedDate = currentDate;
                return true; // Newly achieved
            }
            return false;
        }

        public static List<MilestoneGoal> GetDefaultGoals()
        {
            return new List<MilestoneGoal>
            {
                new MilestoneGoal("goal_first_capital", "Financial Foundation", "Accumulate $10,000 in net worth.", GoalCategory.Wealth, 1000000L, Currency.FromDollars(1000.00)),
                new MilestoneGoal("goal_career_lead", "Corporate Climber", "Earn a monthly career salary of at least $4,000.", GoalCategory.Career, 400000L, Currency.FromDollars(1500.00)),
                new MilestoneGoal("goal_landlord", "Property Investor", "Acquire your first residential or commercial real estate property.", GoalCategory.RealEstate, 1L, Currency.FromDollars(2500.00)),
                new MilestoneGoal("goal_multi_biz", "Serial Entrepreneur", "Own and operate at least 2 commercial enterprises.", GoalCategory.Enterprise, 2L, Currency.FromDollars(5000.00)),
                new MilestoneGoal("goal_market_fund", "Wall Street Capitalist", "Build a stock market portfolio valued at $50,000+.", GoalCategory.Wealth, 5000000L, Currency.FromDollars(5000.00)),
                new MilestoneGoal("goal_fire_freedom", "Financial Independence (FIRE)", "Generate monthly passive income (Rent + Dividends) exceeding living expenses.", GoalCategory.FinancialFreedom, 1L, Currency.FromDollars(10000.00)),
                new MilestoneGoal("goal_millionaire", "Self-Made Millionaire", "Surpass $1,000,000 in total balance sheet net worth.", GoalCategory.Wealth, 100000000L, Currency.FromDollars(50000.00))
            };
        }
    }

    /// <summary>
    /// Choice option presented inside a dynamic life event scenario.
    /// </summary>
    public class EventChoice
    {
        public string Id { get; }
        public string Label { get; }
        public string ConsequenceText { get; }
        public Currency CashCost { get; }
        public Currency CashReward { get; }
        public int EnergyImpact { get; }
        public double SuccessProbability { get; }

        public EventChoice(
            string id,
            string label,
            string consequenceText,
            Currency? cashCost = null,
            Currency? cashReward = null,
            int energyImpact = 0,
            double successProbability = 1.0)
        {
            Id = id;
            Label = label;
            ConsequenceText = consequenceText;
            CashCost = cashCost ?? Currency.Zero;
            CashReward = cashReward ?? Currency.Zero;
            EnergyImpact = energyImpact;
            SuccessProbability = Math.Clamp(successProbability, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Represents a dynamic life or macroeconomic scenario requiring a decision.
    /// </summary>
    public class DynamicLifeEvent
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public EventSeverity Severity { get; }
        public List<EventChoice> Choices { get; }

        public DynamicLifeEvent(
            string id,
            string title,
            string description,
            EventSeverity severity,
            IEnumerable<EventChoice> choices)
        {
            Id = id;
            Title = title;
            Description = description;
            Severity = severity;
            Choices = new List<EventChoice>(choices ?? Array.Empty<EventChoice>());
        }
    }
}
