using UnityEngine;
using UnityEngine.UI;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Component attached to modal sheet roots.
    /// Connects header close button and backdrop click dismissal to ModalManager.
    /// </summary>
    public class ModalSheetView : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        private void Awake()
        {
            BindListeners();
        }

        public void BindButtons(Button closeButton, Button backdropButton)
        {
            _closeButton = closeButton;
            _backdropButton = backdropButton;
            BindListeners();
        }

        private void BindListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_backdropButton != null)
            {
                _backdropButton.onClick.RemoveListener(OnCloseClicked);
                _backdropButton.onClick.AddListener(OnCloseClicked);
            }
        }

        public void Close()
        {
            OnCloseClicked();
        }

        private void OnCloseClicked()
        {
            if (ModalManager.Instance != null)
            {
                ModalManager.Instance.CloseModal(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
