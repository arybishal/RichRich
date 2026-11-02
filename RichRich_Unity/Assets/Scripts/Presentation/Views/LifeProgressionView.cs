using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Models;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for Life Progression, Milestone Goals, FIRE metrics,
    /// and dynamic macroeconomic life events.
    /// Provides visual goal progress bars, achievement states, rewards, and decision dialogue.
    /// Follows dark FinTech styling.
    /// </summary>
    public class LifeProgressionView : MonoBehaviour
    {
        [Header("Header & FIRE Metrics")]
        [SerializeField] private Text _milestonesSummaryText;
        [SerializeField] private Text _fireRatioText;
        [SerializeField] private Text _fireSummaryText;
        [SerializeField] private Slider _fireSlider;

        [Header("Goal Card 1 (Wealth Foundation)")]
        [SerializeField] private Text _goal1TitleText;
        [SerializeField] private Text _goal1DescText;
        [SerializeField] private Slider _goal1Slider;
        [SerializeField] private Text _goal1ProgressText;
        [SerializeField] private Text _goal1RewardText;

        [Header("Goal Card 2 (Corporate Career)")]
        [SerializeField] private Text _goal2TitleText;
        [SerializeField] private Text _goal2DescText;
        [SerializeField] private Slider _goal2Slider;
        [SerializeField] private Text _goal2ProgressText;
        [SerializeField] private Text _goal2RewardText;

        [Header("Goal Card 3 (Property Investor)")]
        [SerializeField] private Text _goal3TitleText;
        [SerializeField] private Text _goal3DescText;
        [SerializeField] private Slider _goal3Slider;
        [SerializeField] private Text _goal3ProgressText;
        [SerializeField] private Text _goal3RewardText;

        [Header("Goal Card 4 (Enterprise)")]
        [SerializeField] private Text _goal4TitleText;
        [SerializeField] private Text _goal4DescText;
        [SerializeField] private Slider _goal4Slider;
        [SerializeField] private Text _goal4ProgressText;
        [SerializeField] private Text _goal4RewardText;

        [Header("Goal Card 5 (Wall Street Portfolio)")]
        [SerializeField] private Text _goal5TitleText;
        [SerializeField] private Text _goal5DescText;
        [SerializeField] private Slider _goal5Slider;
        [SerializeField] private Text _goal5ProgressText;
        [SerializeField] private Text _goal5RewardText;

        [Header("Goal Card 6 (FIRE Freedom)")]
        [SerializeField] private Text _goal6TitleText;
        [SerializeField] private Text _goal6DescText;
        [SerializeField] private Slider _goal6Slider;
        [SerializeField] private Text _goal6ProgressText;
        [SerializeField] private Text _goal6RewardText;

        [Header("Goal Card 7 (Millionaire)")]
        [SerializeField] private Text _goal7TitleText;
        [SerializeField] private Text _goal7DescText;
        [SerializeField] private Slider _goal7Slider;
        [SerializeField] private Text _goal7ProgressText;
        [SerializeField] private Text _goal7RewardText;

        [Header("Dynamic Life Event Dialogue")]
        [SerializeField] private GameObject _eventPanel;
        [SerializeField] private Text _eventTitleText;
        [SerializeField] private Text _eventDescriptionText;
        [SerializeField] private Button _choice1Button;
        [SerializeField] private Text _choice1Text;
        [SerializeField] private Button _choice2Button;
        [SerializeField] private Text _choice2Text;
        [SerializeField] private Text _eventFeedbackText;

        private LifeProgressionController _controller;
        private string _choice1Id;
        private string _choice2Id;

        public void Bind(LifeProgressionController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.OnStateChanged += RefreshUI;
            _controller.OnGoalCompleted += OnGoalCompleted;
            _controller.OnEventTriggered += OnEventTriggered;
            _controller.OnEventOutcome += OnEventOutcome;

            if (_choice1Button != null)
            {
                _choice1Button.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_choice1Id))
                    {
                        _controller.ChooseEventOption(_choice1Id, out string msg, out string err);
                    }
                });
            }

            if (_choice2Button != null)
            {
                _choice2Button.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_choice2Id))
                    {
                        _controller.ChooseEventOption(_choice2Id, out string msg, out string err);
                    }
                });
            }

            RefreshUI();
        }

        private void OnGoalCompleted(MilestoneGoal goal)
        {
            if (_eventFeedbackText != null)
            {
                _eventFeedbackText.text = $"★ MILESTONE ACHIEVED: {goal.Title}! Reward: {goal.RewardCash}";
                _eventFeedbackText.color = new Color(0.35f, 0.95f, 0.55f);
            }
        }

        private void OnEventTriggered(DynamicLifeEvent ev)
        {
            RefreshEventUI();
        }

        private void OnEventOutcome(string outcomeMessage)
        {
            if (_eventFeedbackText != null)
            {
                _eventFeedbackText.text = outcomeMessage;
                _eventFeedbackText.color = new Color(0.95f, 0.85f, 0.35f);
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            // 1. Header & FIRE
            if (_milestonesSummaryText != null) _milestonesSummaryText.text = _controller.GoalsAchievedSummary;
            if (_fireRatioText != null) _fireRatioText.text = _controller.FireProgressText;
            if (_fireSummaryText != null) _fireSummaryText.text = _controller.FireSummaryText;
            if (_fireSlider != null) _fireSlider.value = (float)Math.Clamp(_controller.FireRatio, 0.0, 1.0);

            // 2. Goal Cards
            var goals = _controller.Goals;
            if (goals != null && goals.Count > 0)
            {
                UpdateCard(0, _goal1TitleText, _goal1DescText, _goal1Slider, _goal1ProgressText, _goal1RewardText);
                UpdateCard(1, _goal2TitleText, _goal2DescText, _goal2Slider, _goal2ProgressText, _goal2RewardText);
                UpdateCard(2, _goal3TitleText, _goal3DescText, _goal3Slider, _goal3ProgressText, _goal3RewardText);
                UpdateCard(3, _goal4TitleText, _goal4DescText, _goal4Slider, _goal4ProgressText, _goal4RewardText);
                UpdateCard(4, _goal5TitleText, _goal5DescText, _goal5Slider, _goal5ProgressText, _goal5RewardText);
                UpdateCard(5, _goal6TitleText, _goal6DescText, _goal6Slider, _goal6ProgressText, _goal6RewardText);
                UpdateCard(6, _goal7TitleText, _goal7DescText, _goal7Slider, _goal7ProgressText, _goal7RewardText);
            }

            // 3. Pending Event
            RefreshEventUI();
        }

        private void UpdateCard(int index, Text title, Text desc, Slider slider, Text progress, Text reward)
        {
            var goals = _controller.Goals;
            if (index >= goals.Count) return;
            var g = goals[index];

            if (title != null)
            {
                title.text = g.IsCompleted ? $"✓ {g.Title}" : g.Title;
                title.color = g.IsCompleted ? new Color(0.35f, 0.95f, 0.55f) : Color.white;
            }
            if (desc != null) desc.text = g.Description;
            if (slider != null) slider.value = (float)g.ProgressFraction;
            if (progress != null)
            {
                progress.text = g.IsCompleted ? "COMPLETED" : g.ProgressFormatted;
                progress.color = g.IsCompleted ? new Color(0.35f, 0.95f, 0.55f) : new Color(0.85f, 0.85f, 0.85f);
            }
            if (reward != null) reward.text = $"Award: {g.RewardCash}";
        }

        private void RefreshEventUI()
        {
            if (_controller == null) return;
            var ev = _controller.CurrentPendingEvent;

            if (ev == null)
            {
                if (_eventPanel != null) _eventPanel.SetActive(false);
                return;
            }

            if (_eventPanel != null) _eventPanel.SetActive(true);
            if (_eventTitleText != null) _eventTitleText.text = ev.Title;
            if (_eventDescriptionText != null) _eventDescriptionText.text = ev.Description;

            if (ev.Choices.Count > 0)
            {
                _choice1Id = ev.Choices[0].Id;
                if (_choice1Button != null) _choice1Button.gameObject.SetActive(true);
                if (_choice1Text != null) _choice1Text.text = ev.Choices[0].Label;
            }
            else
            {
                _choice1Id = null;
                if (_choice1Button != null) _choice1Button.gameObject.SetActive(false);
            }

            if (ev.Choices.Count > 1)
            {
                _choice2Id = ev.Choices[1].Id;
                if (_choice2Button != null) _choice2Button.gameObject.SetActive(true);
                if (_choice2Text != null) _choice2Text.text = ev.Choices[1].Label;
            }
            else
            {
                _choice2Id = null;
                if (_choice2Button != null) _choice2Button.gameObject.SetActive(false);
            }
        }
    }
}
