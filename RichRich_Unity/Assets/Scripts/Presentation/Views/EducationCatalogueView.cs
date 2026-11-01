using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for Higher Education, training courses, and degrees.
    /// Manages tuition payments, degree selection, and part-time/full-time pace.
    /// </summary>
    public class EducationCatalogueView : MonoBehaviour
    {
        [Header("Active Study Overview")]
        [SerializeField] private Text _activeCourseTitleText;
        [SerializeField] private Text _activeProgressPercentText;
        [SerializeField] private Text _activeHoursText;
        [SerializeField] private Slider _activeProgressBar;
        [SerializeField] private Text _currentPaceText;

        [Header("Pace Toggle")]
        [SerializeField] private Button _partTimeButton;
        [SerializeField] private Button _fullTimeButton;

        [Header("Selected Program Details")]
        [SerializeField] private Text _selectedCourseTitleText;
        [SerializeField] private Text _selectedTuitionText;
        [SerializeField] private Text _selectedDurationText;
        [SerializeField] private Text _selectedPrerequisitesText;
        [SerializeField] private Text _selectedDescriptionText;

        [Header("Actions")]
        [SerializeField] private Button _enrollButton;
        [SerializeField] private Button _dropButton;
        [SerializeField] private Text _feedbackText;

        private EducationCatalogueController _controller;

        public void Bind(EducationCatalogueController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnCatalogueUpdated += RefreshUI;

            if (_partTimeButton != null) _partTimeButton.onClick.AddListener(() => _controller.SetStudyPace(StudyPace.PartTime));
            if (_fullTimeButton != null) _fullTimeButton.onClick.AddListener(() => _controller.SetStudyPace(StudyPace.FullTime));

            if (_enrollButton != null)
            {
                _enrollButton.onClick.AddListener(() =>
                {
                    bool ok = _controller.EnrollSelected(out string err);
                    if (_feedbackText != null)
                    {
                        _feedbackText.text = ok ? "Successfully enrolled!" : err;
                    }
                });
            }

            if (_dropButton != null)
            {
                _dropButton.onClick.AddListener(() => _controller.DropActiveEnrollment());
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnCatalogueUpdated -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            // Active Enrollment
            if (_activeCourseTitleText != null) _activeCourseTitleText.text = _controller.ActiveCourseTitle;
            if (_activeProgressPercentText != null) _activeProgressPercentText.text = _controller.ActiveProgressPercentText;
            if (_activeHoursText != null) _activeHoursText.text = _controller.ActiveHoursText;
            if (_activeProgressBar != null) _activeProgressBar.value = _controller.ActiveProgressNormalized;
            if (_currentPaceText != null) _currentPaceText.text = _controller.ActiveStudyPaceText;

            // Selected Course
            var selected = _controller.SelectedCourse;
            if (selected != null)
            {
                if (_selectedCourseTitleText != null) _selectedCourseTitleText.text = selected.Title;
                if (_selectedTuitionText != null) _selectedTuitionText.text = selected.TuitionCost.MinorUnits == 0 ? "FREE ($0.00)" : $"{selected.TuitionCost}";
                if (_selectedDurationText != null) _selectedDurationText.text = $"{selected.TotalStudyHoursRequired} total study hours ({selected.Type})";
                if (_selectedDescriptionText != null) _selectedDescriptionText.text = selected.Description;

                if (_selectedPrerequisitesText != null)
                {
                    _selectedPrerequisitesText.text = !string.IsNullOrEmpty(selected.PrerequisiteQualification)
                        ? $"Prerequisite: {selected.PrerequisiteQualification}"
                        : "Prerequisite: None";
                }

                if (_enrollButton != null)
                {
                    bool canEnroll = _controller.CanEnrollSelected(out string reason);
                    _enrollButton.interactable = canEnroll;
                    if (!canEnroll && _feedbackText != null)
                    {
                        _feedbackText.text = reason;
                    }
                }
            }

            if (_dropButton != null)
            {
                _dropButton.interactable = _controller.HasActiveEnrollment;
            }
        }
    }
}
