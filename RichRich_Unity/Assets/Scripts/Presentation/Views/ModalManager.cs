using System;
using UnityEngine;
using UnityEngine.UI;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Centralized Unity navigation manager for modal dialogs and overlay sheets.
    /// Provides LIFO modal stacking, nested sub-modal history, Android hardware/gesture Back button handling,
    /// Desktop Escape/Backspace testing support, Canvas z-index ordering (SetAsLastSibling),
    /// and application exit confirmation dialog on the main dashboard.
    /// </summary>
    public class ModalManager : MonoBehaviour
    {
        private static ModalManager _instance;

        public static ModalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ModalManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ModalManager");
                        _instance = go.AddComponent<ModalManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Back Button Settings")]
        [SerializeField] private bool _enableBackButton = true;

        [Header("Exit Confirmation Dialog")]
        [SerializeField] private GameObject _exitDialogRoot;
        [SerializeField] private Button _exitCancelButton;
        [SerializeField] private Button _exitConfirmButton;

        private ModalStackController<GameObject> _stackController;

        public int ActiveCount
        {
            get
            {
                EnsureController();
                return _stackController.ActiveCount;
            }
        }

        public bool HasActiveModals
        {
            get
            {
                EnsureController();
                return _stackController.HasActiveModals;
            }
        }

        public bool IsExitDialogOpen
        {
            get
            {
                EnsureController();
                return _stackController.IsExitDialogOpen;
            }
        }

        public GameObject CurrentTopModal
        {
            get
            {
                EnsureController();
                return _stackController.CurrentTop;
            }
        }

        public event Action<GameObject> OnModalOpened;
        public event Action<GameObject> OnModalClosed;
        public event Action<int> OnStackChanged;
        public event Action<bool> OnExitDialogToggled;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            EnsureController();
            BindExitDialogButtons();
        }

        private void EnsureController()
        {
            if (_stackController == null)
            {
                _stackController = new ModalStackController<GameObject>(
                    onActivate: go =>
                    {
                        if (go != null)
                        {
                            go.transform.SetAsLastSibling();
                            go.SetActive(true);
                        }
                    },
                    onDeactivate: go =>
                    {
                        if (go != null)
                        {
                            go.SetActive(false);
                        }
                    },
                    onExitDialogToggle: isVisible =>
                    {
                        if (_exitDialogRoot != null)
                        {
                            if (isVisible) _exitDialogRoot.transform.SetAsLastSibling();
                            _exitDialogRoot.SetActive(isVisible);
                        }
                    },
                    onQuitAction: () =>
                    {
                        Debug.Log("[ModalManager] Quitting application via Exit Confirmation.");
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                );

                _stackController.OnModalOpened += modal => OnModalOpened?.Invoke(modal);
                _stackController.OnModalClosed += modal => OnModalClosed?.Invoke(modal);
                _stackController.OnStackChanged += count => OnStackChanged?.Invoke(count);
                _stackController.OnExitDialogToggled += visible => OnExitDialogToggled?.Invoke(visible);
            }
        }

        public void BindExitDialog(GameObject exitDialogRoot, Button cancelButton, Button confirmButton)
        {
            _exitDialogRoot = exitDialogRoot;
            _exitCancelButton = cancelButton;
            _exitConfirmButton = confirmButton;
            BindExitDialogButtons();
        }

        private void BindExitDialogButtons()
        {
            if (_exitCancelButton != null)
            {
                _exitCancelButton.onClick.RemoveListener(DismissExitDialog);
                _exitCancelButton.onClick.AddListener(DismissExitDialog);
            }

            if (_exitConfirmButton != null)
            {
                _exitConfirmButton.onClick.RemoveListener(ConfirmExit);
                _exitConfirmButton.onClick.AddListener(ConfirmExit);
            }

            // Ensure initially inactive
            if (_exitDialogRoot != null && (_stackController == null || !_stackController.IsExitDialogOpen))
            {
                _exitDialogRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_enableBackButton) return;

            // Intercept Android hardware Back button (Escape) and Editor Backspace/Escape
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                HandleBackPress();
            }
        }

        /// <summary>
        /// Handles Android hardware/gesture back navigation or Escape/Backspace key.
        /// </summary>
        public BackPressResult HandleBackPress()
        {
            EnsureController();
            return _stackController.HandleBackPress();
        }

        /// <summary>
        /// Opens a root-level modal from the HUD. Automatically closes all active modals.
        /// </summary>
        public void OpenRoot(GameObject modalRoot)
        {
            if (modalRoot == null) return;
            EnsureController();
            _stackController.PushRoot(modalRoot);
        }

        /// <summary>
        /// Pushes a child/nested modal on top of the current modal without closing underlying modals.
        /// </summary>
        public void PushChild(GameObject modalRoot)
        {
            if (modalRoot == null) return;
            EnsureController();
            _stackController.PushChild(modalRoot);
        }

        /// <summary>
        /// Standard push method.
        /// </summary>
        public void Push(GameObject modalRoot, bool isRoot = false)
        {
            if (isRoot)
                OpenRoot(modalRoot);
            else
                PushChild(modalRoot);
        }

        /// <summary>
        /// Pops the top-most modal.
        /// </summary>
        public GameObject Pop()
        {
            EnsureController();
            return _stackController.Pop();
        }

        /// <summary>
        /// Closes a specific modal dialog.
        /// </summary>
        public void CloseModal(GameObject modalRoot)
        {
            if (modalRoot == null) return;
            EnsureController();
            _stackController.Close(modalRoot);
        }

        /// <summary>
        /// Closes all active modals.
        /// </summary>
        public void CloseAll()
        {
            EnsureController();
            _stackController.CloseAll();
        }

        /// <summary>
        /// Displays the exit confirmation dialog.
        /// </summary>
        public void ShowExitDialog()
        {
            EnsureController();
            _stackController.ShowExitDialog();
        }

        /// <summary>
        /// Dismisses the exit confirmation dialog.
        /// </summary>
        public void DismissExitDialog()
        {
            EnsureController();
            _stackController.DismissExitDialog();
        }

        /// <summary>
        /// Confirms quitting the application.
        /// </summary>
        public void ConfirmExit()
        {
            EnsureController();
            _stackController.ConfirmExit();
        }

        /// <summary>
        /// Checks whether a given modal is currently open in the stack.
        /// </summary>
        public bool IsModalOpen(GameObject modalRoot)
        {
            if (modalRoot == null) return false;
            EnsureController();
            return _stackController.IsActive(modalRoot);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
