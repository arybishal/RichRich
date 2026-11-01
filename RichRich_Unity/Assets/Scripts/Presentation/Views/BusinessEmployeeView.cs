using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for enterprise employee and staffing management.
    /// Manages candidate recruitment, monthly payroll projections, productivity boosts,
    /// and staff dismissals across all owned commercial businesses.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class BusinessEmployeeView : MonoBehaviour
    {
        [Header("Staffing Summary")]
        [SerializeField] private Text _totalPayrollText;
        [SerializeField] private Text _staffCountText;

        [Header("First Hired Staff Member")]
        [SerializeField] private Text _employee1NameText;
        [SerializeField] private Text _employee1RoleText;
        [SerializeField] private Text _employee1SalaryText;
        [SerializeField] private Button _employee1DismissButton;

        [Header("Candidate Recruitment")]
        [SerializeField] private Text _candidate1NameText;
        [SerializeField] private Text _candidate1RoleText;
        [SerializeField] private Text _candidate1SalaryText;
        [SerializeField] private Text _candidate1ProductivityText;
        [SerializeField] private Button _candidate1HireButton;

        [Header("Feedback")]
        [SerializeField] private Text _feedbackText;

        private BusinessEmployeeController _controller;

        public void Bind(BusinessEmployeeController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnStaffingUpdated += RefreshUI;
            _controller.OnEmployeeHired += OnHired;
            _controller.OnEmployeeDismissed += OnDismissed;
            _controller.OnActionFailed += OnFailed;

            if (_employee1DismissButton != null)
            {
                _employee1DismissButton.onClick.AddListener(() =>
                {
                    var staff = _controller.CurrentStaff;
                    if (staff.Count > 0) _controller.TryDismissEmployee(staff[0].Id, out _);
                });
            }

            if (_candidate1HireButton != null)
            {
                _candidate1HireButton.onClick.AddListener(() =>
                {
                    var cands = _controller.AvailableCandidates;
                    if (cands.Count > 0) _controller.TryHireCandidate(cands[0].Id, out _);
                });
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            if (_totalPayrollText != null)
                _totalPayrollText.text = $"Total Staff Payroll: {_controller.TotalMonthlyPayroll}/mo";

            var staff = _controller.CurrentStaff;
            if (_staffCountText != null)
                _staffCountText.text = $"Active Staff: {staff.Count} employees";

            if (staff.Count > 0)
            {
                var e1 = staff[0];
                if (_employee1NameText != null) _employee1NameText.text = e1.Name;
                if (_employee1RoleText != null) _employee1RoleText.text = $"Role: {e1.Role}";
                if (_employee1SalaryText != null) _employee1SalaryText.text = $"Salary: {e1.MonthlySalary}/mo";
                if (_employee1DismissButton != null) _employee1DismissButton.interactable = true;
            }
            else
            {
                if (_employee1NameText != null) _employee1NameText.text = "No staff currently employed.";
                if (_employee1RoleText != null) _employee1RoleText.text = string.Empty;
                if (_employee1SalaryText != null) _employee1SalaryText.text = string.Empty;
                if (_employee1DismissButton != null) _employee1DismissButton.interactable = false;
            }

            var cands = _controller.AvailableCandidates;
            if (cands.Count > 0)
            {
                var c1 = cands[0];
                if (_candidate1NameText != null) _candidate1NameText.text = c1.Name;
                if (_candidate1RoleText != null) _candidate1RoleText.text = $"Role: {c1.Role}";
                if (_candidate1SalaryText != null) _candidate1SalaryText.text = $"Asking Salary: {c1.MonthlySalary}/mo";
                if (_candidate1ProductivityText != null) _candidate1ProductivityText.text = $"Productivity: {c1.Productivity:P0}";
                if (_candidate1HireButton != null) _candidate1HireButton.interactable = true;
            }
            else
            {
                if (_candidate1NameText != null) _candidate1NameText.text = "No candidates currently available.";
                if (_candidate1RoleText != null) _candidate1RoleText.text = string.Empty;
                if (_candidate1SalaryText != null) _candidate1SalaryText.text = string.Empty;
                if (_candidate1ProductivityText != null) _candidate1ProductivityText.text = string.Empty;
                if (_candidate1HireButton != null) _candidate1HireButton.interactable = false;
            }
        }

        private void OnHired(Employee emp)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Successfully hired {emp.Name} as {emp.Role}!";
        }

        private void OnDismissed(Employee emp)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Dismissed {emp.Name}. Payroll adjusted.";
        }

        private void OnFailed(string error)
        {
            if (_feedbackText != null)
                _feedbackText.text = $"Staffing action failed: {error}";
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnStaffingUpdated -= RefreshUI;
                _controller.OnEmployeeHired -= OnHired;
                _controller.OnEmployeeDismissed -= OnDismissed;
                _controller.OnActionFailed -= OnFailed;
            }
        }
    }
}
