using System;
using System.Collections.Generic;
using System.Linq;
using RichRich.Domain.Models;
using RichRich.Domain.Services;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation view-model controller for the Higher Education and Training Catalogue.
    /// Manages programme filtering, prerequisite inspection, study pace selection (Part-Time / Full-Time),
    /// tuition payments, and live academic progress.
    /// Pure C# controller with zero MonoBehaviour dependencies.
    /// </summary>
    public class EducationCatalogueController
    {
        public LifeSimulationCoordinator Coordinator { get; }
        public EducationService EducationService => Coordinator.EducationService;

        public EducationType? FilterType { get; private set; } = null;
        public EducationCourse SelectedCourse { get; private set; }
        public StudyPace SelectedPace { get; private set; } = StudyPace.PartTime;

        public IReadOnlyList<EducationCourse> FilteredCourses
        {
            get
            {
                if (FilterType.HasValue)
                {
                    return EducationService.GetCoursesByType(FilterType.Value);
                }
                return EducationService.Catalog;
            }
        }

        public bool HasActiveEnrollment => EducationService.ActiveEnrollment != null && !EducationService.ActiveEnrollment.IsCompleted;
        public string ActiveCourseTitle => HasActiveEnrollment ? EducationService.ActiveEnrollment.Course.Title : "None";
        public float ActiveProgressNormalized => HasActiveEnrollment ? EducationService.ActiveEnrollment.ProgressNormalized : 0f;
        public string ActiveProgressPercentText => $"{(int)(ActiveProgressNormalized * 100)}%";
        public string ActiveHoursText => HasActiveEnrollment
            ? $"{EducationService.ActiveEnrollment.HoursCompleted:F0} / {EducationService.ActiveEnrollment.Course.TotalStudyHoursRequired} hrs"
            : "0 / 0 hrs";
        public string ActiveStudyPaceText => EducationService.CurrentStudyPace == StudyPace.FullTime ? "Full-Time (6 hrs/day)" : "Part-Time (2 hrs/day)";

        public IReadOnlyCollection<string> EarnedQualifications => EducationService.EarnedQualifications;

        public event Action OnCatalogueUpdated;

        public EducationCatalogueController(LifeSimulationCoordinator coordinator)
        {
            Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            Coordinator.EducationService.OnEnrolled += _ => OnCatalogueUpdated?.Invoke();
            Coordinator.EducationService.OnCourseProgressed += _ => OnCatalogueUpdated?.Invoke();
            Coordinator.EducationService.OnCourseCompleted += _ => OnCatalogueUpdated?.Invoke();

            SelectedCourse = FilteredCourses.FirstOrDefault();
        }

        public void SetTypeFilter(EducationType? type)
        {
            FilterType = type;
            SelectedCourse = FilteredCourses.FirstOrDefault();
            OnCatalogueUpdated?.Invoke();
        }

        public void SelectCourse(string courseId)
        {
            SelectedCourse = EducationService.Catalog.FirstOrDefault(c => c.Id == courseId);
            OnCatalogueUpdated?.Invoke();
        }

        public void SetStudyPace(StudyPace pace)
        {
            SelectedPace = pace;
            EducationService.SetStudyPace(pace);
            OnCatalogueUpdated?.Invoke();
        }

        public bool CanEnrollSelected(out string reason)
        {
            if (SelectedCourse == null)
            {
                reason = "No course selected.";
                return false;
            }

            return EducationService.CanEnroll(SelectedCourse.Id, out reason);
        }

        public bool EnrollSelected(out string error)
        {
            if (SelectedCourse == null)
            {
                error = "No course selected.";
                return false;
            }

            bool success = EducationService.TryEnroll(SelectedCourse.Id, SelectedPace, out error);
            OnCatalogueUpdated?.Invoke();
            return success;
        }

        public void DropActiveEnrollment()
        {
            EducationService.DropActiveEnrollment();
            OnCatalogueUpdated?.Invoke();
        }
    }
}
