using System;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Life & Wealth simulation HUD.
    /// Prioritizes: Cash, Net Worth, Date/Time, Career, Housing, Energy/Lifestyle, Monthly Cashflow, and Financial Health.
    /// Completely replaces tap-to-earn interfaces with financial decision metrics.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class LifeSimulationHUDController
    {
        public LifeSimulationCoordinator Coordinator { get; }

        public string CashBalanceText => Coordinator.Wallet.Balance.ToString();
        public string NetWorthText => Coordinator.FinancialService.NetWorthFormatted;
        public bool IsNetWorthNegative => Coordinator.BalanceSheet.IsNegativeNetWorth;

        public string SimulationDateLongText => Coordinator.Clock.CurrentDate.ToLongDateString();
        public string SimulationDateShortText => Coordinator.Clock.CurrentDate.ToString();
        public string SimulationTimeString => Coordinator.Clock.CurrentDate.ToShortTimeString();

        public string CareerStatusText => Coordinator.CareerService.IsEmployed
            ? $"{Coordinator.CareerService.CurrentEmployment.Job.Title} ({Coordinator.CareerService.CurrentEmployment.Job.MonthlySalary}/mo)"
            : "Unemployed";

        public string HousingStatusText => Coordinator.HousingService.CurrentHousing != null
            ? $"{Coordinator.HousingService.CurrentHousing.Name} ({Coordinator.HousingService.CurrentHousing.MonthlyRent}/mo)"
            : "No Housing";

        public int CurrentEnergy => Coordinator.LifestyleService != null ? Coordinator.LifestyleService.CurrentEnergy : 100;
        public string EnergyStatusText => $"{CurrentEnergy}%";
        public string FatigueStatusText => Coordinator.LifestyleService != null ? Coordinator.LifestyleService.FatigueState.ToString() : "WellRested";

        public string MonthlyIncomeText => $"+{Coordinator.HealthService.ProjectedMonthlyIncome}/mo";
        public string MonthlyExpensesText => $"-{Coordinator.HealthService.ProjectedMonthlyExpenses}/mo";

        public long NetMonthlyCashFlowCents => Coordinator.HealthService.ProjectedNetMonthlyCashFlowCents;
        public string NetMonthlyCashFlowText
        {
            get
            {
                long cents = NetMonthlyCashFlowCents;
                if (cents >= 0)
                {
                    return $"+{Currency.FromCents(cents)}/mo";
                }
                else
                {
                    return $"-{Currency.FromCents(-cents)}/mo";
                }
            }
        }

        public FinancialHealthReport CurrentHealthReport => Coordinator.HealthService.EvaluateStatus();
        public FinancialHealthState HealthState => CurrentHealthReport.State;
        public string HealthBadgeText => HealthState.ToString();
        public string HealthAdviceText => CurrentHealthReport.Recommendation;

        public string EducationStatusText
        {
            get
            {
                var enrollment = Coordinator.EducationService.ActiveEnrollment;
                if (enrollment == null || enrollment.IsCompleted)
                {
                    return "No Active Course";
                }
                int pct = (int)(enrollment.ProgressNormalized * 100);
                return $"{enrollment.Course.Title} ({pct}%)";
            }
        }

        public string ClockSpeedText => Coordinator.Clock.IsPaused
            ? "PAUSED"
            : $"{Coordinator.Clock.SpeedMultiplier:F0}x Speed";

        public event Action OnHUDUpdated;

        public LifeSimulationHUDController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.Wallet.OnBalanceChanged += _ => OnHUDUpdated?.Invoke();
            Coordinator.FinancialService.OnBalanceSheetUpdated += () => OnHUDUpdated?.Invoke();
            Coordinator.Clock.OnMinuteTicked += _ => OnHUDUpdated?.Invoke();
            Coordinator.Clock.OnDayPassed += _ => OnHUDUpdated?.Invoke();
            Coordinator.CareerService.OnHired += _ => OnHUDUpdated?.Invoke();
            Coordinator.CareerService.OnResigned += _ => OnHUDUpdated?.Invoke();
            Coordinator.EducationService.OnEnrolled += _ => OnHUDUpdated?.Invoke();
            Coordinator.EducationService.OnCourseProgressed += _ => OnHUDUpdated?.Invoke();

            if (Coordinator.HousingService != null)
            {
                Coordinator.HousingService.OnHousingChanged += _ => OnHUDUpdated?.Invoke();
            }

            if (Coordinator.LifestyleService != null)
            {
                Coordinator.LifestyleService.OnEnergyChanged += (_, _) => OnHUDUpdated?.Invoke();
                Coordinator.LifestyleService.OnFatigueChanged += _ => OnHUDUpdated?.Invoke();
            }
        }

        public void TogglePause()
        {
            if (Coordinator.Clock.IsPaused)
            {
                Coordinator.Clock.SpeedMultiplier = 1.0f;
            }
            else
            {
                Coordinator.Clock.SpeedMultiplier = 0.0f;
            }
            OnHUDUpdated?.Invoke();
        }

        public void SetSpeed(float multiplier)
        {
            Coordinator.Clock.SpeedMultiplier = Math.Max(0f, multiplier);
            OnHUDUpdated?.Invoke();
        }

        public void SleepToNextMorning()
        {
            Coordinator.Clock.FastForwardToNextMorning();
            OnHUDUpdated?.Invoke();
        }

        public void AdvanceDays(int days)
        {
            Coordinator.FastForwardDays(days);
            OnHUDUpdated?.Invoke();
        }

        public void Tick(double deltaRealSeconds)
        {
            Coordinator.Tick(deltaRealSeconds);
        }
    }
}
