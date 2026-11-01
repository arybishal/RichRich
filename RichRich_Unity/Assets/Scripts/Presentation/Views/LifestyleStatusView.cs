using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for Player Lifestyle Status, energy meter, fatigue badge,
    /// rest breaks, and sleep recovery.
    /// </summary>
    public class LifestyleStatusView : MonoBehaviour
    {
        [Header("Energy & Fatigue")]
        [SerializeField] private Slider _energyBar;
        [SerializeField] private Text _energyValueText;
        [SerializeField] private Text _fatigueBadgeText;

        [Header("Housing Environment")]
        [SerializeField] private Text _housingTierText;
        [SerializeField] private Text _overnightRecoveryText;

        [Header("Time Allocation")]
        [SerializeField] private Text _weeklyScheduleText;

        [Header("Actions")]
        [SerializeField] private Button _restBreakButton;
        [SerializeField] private Button _sleepToMorningButton;

        private LifestyleStatusController _controller;

        public void Bind(LifestyleStatusController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnLifestyleUpdated += RefreshUI;

            if (_restBreakButton != null)
            {
                _restBreakButton.onClick.AddListener(() => _controller.TakeRest());
            }

            if (_sleepToMorningButton != null)
            {
                _sleepToMorningButton.onClick.AddListener(() => _controller.SleepToMorning());
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnLifestyleUpdated -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            if (_energyBar != null) _energyBar.value = _controller.EnergyNormalized;
            if (_energyValueText != null) _energyValueText.text = _controller.EnergyBarText;
            if (_fatigueBadgeText != null) _fatigueBadgeText.text = $"Status: {_controller.FatigueBadgeText}";

            if (_housingTierText != null) _housingTierText.text = $"Residence: {_controller.HousingNameText} ({_controller.HousingComfortRating}/100)";
            if (_overnightRecoveryText != null) _overnightRecoveryText.text = $"+{_controller.OvernightRecoveryAmount} energy / night";

            if (_weeklyScheduleText != null) _weeklyScheduleText.text = _controller.ScheduleSummaryText;
        }
    }
}
