using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Interactive Job Board, vacancy inspector,
    /// application results, and career profile.
    /// Follows premium dark FinTech design direction.
    /// </summary>
    public class JobBoardView : MonoBehaviour
    {
        [Header("Career Profile")]
        [SerializeField] private Text _currentRoleText;
        [SerializeField] private Text _currentCompanyText;
        [SerializeField] private Text _currentSalaryText;
        [SerializeField] private Text _careerTenureText;
        [SerializeField] private Text _trackExperienceSummaryText;

        [Header("Filters")]
        [SerializeField] private Button _filterAllButton;
        [SerializeField] private Button _filterRetailButton;
        [SerializeField] private Button _filterTechButton;
        [SerializeField] private Button _filterBusinessButton;

        [Header("Selected Vacancy Details")]
        [SerializeField] private Text _selectedJobTitleText;
        [SerializeField] private Text _selectedJobCompanyText;
        [SerializeField] private Text _selectedJobSalaryText;
        [SerializeField] private Text _selectedJobHoursText;
        [SerializeField] private Text _selectedJobRequirementsText;
        [SerializeField] private Text _selectedJobDescriptionText;

        [Header("Actions")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _promotionButton;
        [SerializeField] private Button _resignButton;
        [SerializeField] private Text _applicationFeedbackText;

        private JobBoardController _controller;

        public void Bind(JobBoardController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnJobBoardUpdated += RefreshUI;
            _controller.OnApplicationProcessed += OnApplicationResult;

            if (_filterAllButton != null) _filterAllButton.onClick.AddListener(() => _controller.SetTrackFilter(null));
            if (_filterRetailButton != null) _filterRetailButton.onClick.AddListener(() => _controller.SetTrackFilter(CareerTrack.Retail));
            if (_filterTechButton != null) _filterTechButton.onClick.AddListener(() => _controller.SetTrackFilter(CareerTrack.Technology));
            if (_filterBusinessButton != null) _filterBusinessButton.onClick.AddListener(() => _controller.SetTrackFilter(CareerTrack.Business));

            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(() => _controller.ApplyForSelected());
            }

            if (_promotionButton != null)
            {
                _promotionButton.onClick.AddListener(() =>
                {
                    bool ok = _controller.TryApplyForPromotion(out string msg);
                    if (_applicationFeedbackText != null)
                    {
                        _applicationFeedbackText.text = ok ? "Promotion approved!" : msg;
                    }
                });
            }

            if (_resignButton != null)
            {
                _resignButton.onClick.AddListener(() => _controller.ResignCurrentJob());
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnJobBoardUpdated -= RefreshUI;
                _controller.OnApplicationProcessed -= OnApplicationResult;
            }
        }

        private void OnApplicationResult(JobApplicationResult result)
        {
            if (_applicationFeedbackText != null)
            {
                _applicationFeedbackText.text = result.Message;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            // Career Profile
            if (_currentRoleText != null) _currentRoleText.text = _controller.CurrentJobTitle;
            if (_currentCompanyText != null) _currentCompanyText.text = _controller.CurrentCompany;
            if (_currentSalaryText != null) _currentSalaryText.text = _controller.CurrentSalaryFormatted;
            if (_careerTenureText != null) _careerTenureText.text = $"Tenure: {_controller.CurrentTenureMonths} mo | Total: {_controller.TotalMonthsWorked} mo";
            if (_trackExperienceSummaryText != null)
            {
                _trackExperienceSummaryText.text = $"Retail: {_controller.RetailExperienceText} | Tech: {_controller.TechExperienceText} | Biz: {_controller.BusinessExperienceText}";
            }

            // Selected Job Details
            var selected = _controller.SelectedJob;
            if (selected != null)
            {
                if (_selectedJobTitleText != null) _selectedJobTitleText.text = selected.Title;
                if (_selectedJobCompanyText != null) _selectedJobCompanyText.text = selected.Company;
                if (_selectedJobSalaryText != null) _selectedJobSalaryText.text = $"{selected.MonthlySalary}/mo";
                if (_selectedJobHoursText != null) _selectedJobHoursText.text = $"{selected.WeeklyHours} hrs/week (Tier {selected.Tier})";
                if (_selectedJobDescriptionText != null) _selectedJobDescriptionText.text = selected.Description;

                if (_selectedJobRequirementsText != null)
                {
                    string reqQual = !string.IsNullOrEmpty(selected.RequiredQualification) ? $"Qual: {selected.RequiredQualification}" : "No degree req";
                    string expReq = selected.MinExperienceMonths > 0 ? $"{selected.MinExperienceMonths} mo exp" : "0 mo exp";
                    _selectedJobRequirementsText.text = $"{reqQual} | {expReq}";
                }

                if (_applyButton != null)
                {
                    bool canApply = _controller.CanApplySelected(out string reason);
                    _applyButton.interactable = canApply;
                    if (!canApply && _applicationFeedbackText != null)
                    {
                        _applicationFeedbackText.text = reason;
                    }
                }
            }

            if (_promotionButton != null)
            {
                bool canPromote = _controller.CheckPromotionAvailability(out string promoText);
                _promotionButton.interactable = canPromote;
            }

            if (_resignButton != null)
            {
                _resignButton.interactable = _controller.IsEmployed;
            }
        }
    }
}
