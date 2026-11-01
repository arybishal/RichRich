using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the offline catch-up earnings modal dialog.
    /// Automatically activates on launch when offline passive income was accumulated.
    /// </summary>
    public class OfflineEarningsModalView : MonoBehaviour
    {
        [Header("Displays")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _amountText;
        [SerializeField] private Text _durationSubtitleText;

        [Header("Controls")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private Text _claimButtonText;

        private OfflineEarningsModalController _controller;

        public void Bind(OfflineEarningsModalController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            if (!_controller.HasClaimableEarnings)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_amountText != null) _amountText.text = _controller.AmountEarnedText;
            if (_durationSubtitleText != null) _durationSubtitleText.text = _controller.SubtitleText;

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(OnClaimClicked);
            }
        }

        private void OnClaimClicked()
        {
            if (_controller == null) return;

            if (_controller.TryClaim(out _, out _))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
