using System;
using System.Collections.Generic;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Result outcome of an Android hardware/gesture Back or Desktop Escape key press.
    /// </summary>
    public enum BackPressResult
    {
        None,
        PoppedModal,
        ShowedExitDialog,
        DismissedExitDialog,
        ExitedApplication
    }

    /// <summary>
    /// Pure presentation controller managing a LIFO navigation stack of modal dialogs.
    /// Supports root modal exclusivity, child/nested modal stacking, back-press (Android/Esc/Backspace) dismissal,
    /// exit confirmation dialog management when on main dashboard, and lifecycle callbacks.
    /// Decoupled from UnityEngine for 100% automated testability.
    /// </summary>
    /// <typeparam name="T">Modal item reference type (e.g. GameObject or token)</typeparam>
    public class ModalStackController<T> where T : class
    {
        private readonly List<T> _stack = new List<T>();
        private readonly Action<T> _onActivate;
        private readonly Action<T> _onDeactivate;
        private readonly Action<bool> _onExitDialogToggle;
        private readonly Action _onQuitAction;

        public event Action<T> OnModalOpened;
        public event Action<T> OnModalClosed;
        public event Action<int> OnStackChanged;
        public event Action<bool> OnExitDialogToggled;
        public event Action OnApplicationQuitRequested;

        public int ActiveCount => _stack.Count;
        public bool HasActiveModals => _stack.Count > 0;
        public T CurrentTop => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public IReadOnlyList<T> Stack => _stack.AsReadOnly();
        public bool IsExitDialogOpen { get; private set; }

        public ModalStackController(
            Action<T> onActivate = null,
            Action<T> onDeactivate = null,
            Action<bool> onExitDialogToggle = null,
            Action onQuitAction = null)
        {
            _onActivate = onActivate;
            _onDeactivate = onDeactivate;
            _onExitDialogToggle = onExitDialogToggle;
            _onQuitAction = onQuitAction;
        }

        /// <summary>
        /// Opens a root-level modal from the dashboard.
        /// Dismisses any currently open modals and the exit confirmation dialog before pushing.
        /// </summary>
        public void PushRoot(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (IsExitDialogOpen)
            {
                DismissExitDialog();
            }

            // If this item is already the solitary active modal, ensure active and return
            if (_stack.Count == 1 && ReferenceEquals(_stack[0], item))
            {
                _onActivate?.Invoke(item);
                return;
            }

            // Close all active modals
            CloseAll();

            // Push and activate root
            _stack.Add(item);
            _onActivate?.Invoke(item);
            OnModalOpened?.Invoke(item);
            OnStackChanged?.Invoke(_stack.Count);
        }

        /// <summary>
        /// Pushes a child/nested modal on top of the current modal stack (e.g. Sub-inspectors, confirmation dialogs).
        /// Maintains full navigation history.
        /// </summary>
        public void PushChild(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (IsExitDialogOpen)
            {
                DismissExitDialog();
            }

            // If already on top, ensure active and return
            if (_stack.Count > 0 && ReferenceEquals(_stack[_stack.Count - 1], item))
            {
                _onActivate?.Invoke(item);
                return;
            }

            // If item already exists lower in stack, remove it from that position to prevent duplicate stacking
            int existingIndex = _stack.IndexOf(item);
            if (existingIndex >= 0)
            {
                _stack.RemoveAt(existingIndex);
            }

            _stack.Add(item);
            _onActivate?.Invoke(item);
            OnModalOpened?.Invoke(item);
            OnStackChanged?.Invoke(_stack.Count);
        }

        /// <summary>
        /// Pops the top-most modal from the stack.
        /// </summary>
        public T Pop()
        {
            if (_stack.Count == 0) return null;

            int lastIdx = _stack.Count - 1;
            T popped = _stack[lastIdx];
            _stack.RemoveAt(lastIdx);

            _onDeactivate?.Invoke(popped);
            OnModalClosed?.Invoke(popped);

            // If there is still a modal underneath, reactivate and restore focus to it
            if (_stack.Count > 0)
            {
                T underlying = _stack[_stack.Count - 1];
                _onActivate?.Invoke(underlying);
            }

            OnStackChanged?.Invoke(_stack.Count);
            return popped;
        }

        /// <summary>
        /// Closes a specific modal from the stack.
        /// </summary>
        public bool Close(T item)
        {
            if (item == null) return false;

            int index = _stack.IndexOf(item);
            if (index < 0) return false;

            if (index == _stack.Count - 1)
            {
                Pop();
                return true;
            }

            _stack.RemoveAt(index);
            _onDeactivate?.Invoke(item);
            OnModalClosed?.Invoke(item);
            OnStackChanged?.Invoke(_stack.Count);
            return true;
        }

        /// <summary>
        /// Closes all modals in LIFO order and clears the stack.
        /// </summary>
        public void CloseAll()
        {
            if (IsExitDialogOpen)
            {
                DismissExitDialog();
            }

            if (_stack.Count == 0) return;

            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                T item = _stack[i];
                _onDeactivate?.Invoke(item);
                OnModalClosed?.Invoke(item);
            }
            _stack.Clear();
            OnStackChanged?.Invoke(0);
        }

        /// <summary>
        /// Handles Android hardware/gesture back-button, Escape key, or Backspace.
        /// Predictable navigation hierarchy:
        /// 1. If Exit Dialog is open: dismisses the Exit Dialog.
        /// 2. If nested/modal screens are open: pops the top-most modal (returning to parent or dashboard).
        /// 3. If on main dashboard with no modals open: opens Exit Confirmation Dialog.
        /// </summary>
        public BackPressResult HandleBackPress()
        {
            // Case 1: Exit Dialog is currently open -> dismiss it
            if (IsExitDialogOpen)
            {
                DismissExitDialog();
                return BackPressResult.DismissedExitDialog;
            }

            // Case 2: At least one modal is open -> pop top modal
            if (_stack.Count > 0)
            {
                Pop();
                return BackPressResult.PoppedModal;
            }

            // Case 3: On main dashboard -> display Exit Confirmation Dialog
            ShowExitDialog();
            return BackPressResult.ShowedExitDialog;
        }

        /// <summary>
        /// Shows the application exit confirmation dialog.
        /// </summary>
        public void ShowExitDialog()
        {
            if (IsExitDialogOpen) return;
            IsExitDialogOpen = true;
            _onExitDialogToggle?.Invoke(true);
            OnExitDialogToggled?.Invoke(true);
        }

        /// <summary>
        /// Dismisses the application exit confirmation dialog without quitting.
        /// </summary>
        public void DismissExitDialog()
        {
            if (!IsExitDialogOpen) return;
            IsExitDialogOpen = false;
            _onExitDialogToggle?.Invoke(false);
            OnExitDialogToggled?.Invoke(false);
        }

        /// <summary>
        /// User confirmed application quit from the Exit Dialog.
        /// </summary>
        public void ConfirmExit()
        {
            DismissExitDialog();
            _onQuitAction?.Invoke();
            OnApplicationQuitRequested?.Invoke();
        }

        /// <summary>
        /// Checks whether a given modal item is currently active in the stack.
        /// </summary>
        public bool IsActive(T item)
        {
            if (item == null) return false;
            return _stack.Contains(item);
        }
    }
}
