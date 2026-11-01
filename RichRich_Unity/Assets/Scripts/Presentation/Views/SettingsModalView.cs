using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Domain.Persistence;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity view binding for the Settings modal dialog.
    /// Audio, haptics, reset save data, and game version display.
    /// </summary>
    public class SettingsModalView : MonoBehaviour
    {
        [Header("Toggles")]
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _hapticsToggle;

        [Header("Information")]
        [SerializeField] private Text _versionText;
        [SerializeField] private Text _packageIdText;

        [Header("Controls")]
        [SerializeField] private Button _resetSaveButton;
        [SerializeField] private Button _closeButton;

        private SettingsModalController _controller;
        private SaveDataService _saveService;

        public void Bind(SettingsModalController controller, SaveDataService saveService)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));

            _controller.OnSettingsChanged += RefreshUI;
            _controller.OnSaveReset += HandleSaveReset;

            if (_soundToggle != null)
            {
                _soundToggle.isOn = _controller.SoundEnabled;
                _soundToggle.onValueChanged.AddListener(OnSoundToggled);
            }

            if (_hapticsToggle != null)
            {
                _hapticsToggle.isOn = _controller.HapticsEnabled;
                _hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            }

            if (_resetSaveButton != null)
            {
                _resetSaveButton.onClick.AddListener(OnResetClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnSettingsChanged -= RefreshUI;
                _controller.OnSaveReset -= HandleSaveReset;
            }
        }

        public void RefreshUI()
        {
            if (_controller == null) return;

            if (_versionText != null) _versionText.text = _controller.VersionText;
            if (_packageIdText != null) _packageIdText.text = _controller.PackageIdText;
        }

        private void OnSoundToggled(bool value)
        {
            _controller?.SetSoundEnabled(value);
        }

        private void OnHapticsToggled(bool value)
        {
            _controller?.SetHapticsEnabled(value);
        }

        private void OnResetClicked()
        {
            _controller?.ResetProgress(_saveService);
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        private void HandleSaveReset()
        {
            // Reload scene to re-bootstrap into clean default state
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
