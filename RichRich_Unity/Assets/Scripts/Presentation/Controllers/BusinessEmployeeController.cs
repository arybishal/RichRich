using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for enterprise employee and staffing management.
    /// Manages candidate recruitment, monthly payroll projections, productivity boosts,
    /// and staff dismissals across all owned commercial businesses.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class BusinessEmployeeController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public BusinessService BusinessService => Coordinator.BusinessService;

        public string SelectedBusinessId { get; private set; }
        public Business SelectedBusiness => BusinessService.GetBusiness(SelectedBusinessId);

        public IReadOnlyList<Employee> CurrentStaff => SelectedBusiness != null
            ? SelectedBusiness.Employees.Where(e => e.IsActive).ToList()
            : new List<Employee>();

        public IReadOnlyList<Employee> AvailableCandidates => BusinessService.CandidatePool
            .Where(e => SelectedBusinessId == null || e.BusinessId == SelectedBusinessId)
            .ToList();

        public Currency TotalMonthlyPayroll => BusinessService.OwnedBusinesses
            .SelectMany(b => b.Employees.Where(e => e.IsActive))
            .Aggregate(Currency.Zero, (acc, e) => acc + e.MonthlySalary);

        public string LastErrorMessage { get; private set; }

        public event Action OnStaffingUpdated;
        public event Action<Employee> OnEmployeeHired;
        public event Action<Employee> OnEmployeeDismissed;
        public event Action<string> OnActionFailed;

        public BusinessEmployeeController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.BusinessService.OnEmployeeHired += emp =>
            {
                OnEmployeeHired?.Invoke(emp);
                OnStaffingUpdated?.Invoke();
            };

            Coordinator.BusinessService.OnEmployeeDismissed += emp =>
            {
                OnEmployeeDismissed?.Invoke(emp);
                OnStaffingUpdated?.Invoke();
            };

            SelectedBusinessId = BusinessService.OwnedBusinesses.FirstOrDefault()?.Id ?? "auto_repair";
        }

        public void SelectBusiness(string businessId)
        {
            SelectedBusinessId = businessId;
            LastErrorMessage = null;
            OnStaffingUpdated?.Invoke();
        }

        public bool TryHireCandidate(string employeeId, out string error)
        {
            if (string.IsNullOrEmpty(SelectedBusinessId))
            {
                error = "No business selected for hiring.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            bool success = BusinessService.TryHireEmployee(SelectedBusinessId, employeeId, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnStaffingUpdated?.Invoke();
            return true;
        }

        public bool TryDismissEmployee(string employeeId, out string error)
        {
            if (string.IsNullOrEmpty(SelectedBusinessId))
            {
                error = "No business selected.";
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            bool success = BusinessService.TryDismissEmployee(SelectedBusinessId, employeeId, out error);
            if (!success)
            {
                LastErrorMessage = error;
                OnActionFailed?.Invoke(error);
                return false;
            }

            LastErrorMessage = null;
            OnStaffingUpdated?.Invoke();
            return true;
        }
    }
}
