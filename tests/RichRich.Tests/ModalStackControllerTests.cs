using System;
using System.Collections.Generic;
using NUnit.Framework;
using RichRich.Presentation.Controllers;

namespace RichRich.Tests
{
    [TestFixture]
    public class ModalStackControllerTests
    {
        private List<string> _activated;
        private List<string> _deactivated;
        private List<bool> _exitDialogToggles;
        private bool _quitInvoked;
        private ModalStackController<string> _controller;

        private static readonly string[] All12Subsystems = new[]
        {
            "JobBoardModal",
            "EducationModal",
            "HousingModal",
            "LifestyleModal",
            "MarketplaceModal",
            "DashboardModal",
            "AutoRepairModal",
            "ConvenienceStoreModal",
            "DigitalAgencyModal",
            "StaffModal",
            "FinanceModal",
            "SettingsModal"
        };

        [SetUp]
        public void SetUp()
        {
            _activated = new List<string>();
            _deactivated = new List<string>();
            _exitDialogToggles = new List<bool>();
            _quitInvoked = false;

            _controller = new ModalStackController<string>(
                onActivate: s => _activated.Add(s),
                onDeactivate: s => _deactivated.Add(s),
                onExitDialogToggle: v => _exitDialogToggles.Add(v),
                onQuitAction: () => _quitInvoked = true
            );
        }

        [Test]
        public void InitialState_IsEmpty()
        {
            Assert.That(_controller.ActiveCount, Is.EqualTo(0));
            Assert.That(_controller.HasActiveModals, Is.False);
            Assert.That(_controller.CurrentTop, Is.Null);
            Assert.That(_controller.Stack, Is.Empty);
            Assert.That(_controller.IsExitDialogOpen, Is.False);
        }

        [Test]
        public void All12Subsystems_CanBeOpenedAndClosedAsRoot()
        {
            foreach (var modalId in All12Subsystems)
            {
                _controller.PushRoot(modalId);
                Assert.That(_controller.ActiveCount, Is.EqualTo(1));
                Assert.That(_controller.CurrentTop, Is.EqualTo(modalId));
                Assert.That(_controller.IsActive(modalId), Is.True);

                bool closed = _controller.Close(modalId);
                Assert.That(closed, Is.True);
                Assert.That(_controller.ActiveCount, Is.EqualTo(0));
                Assert.That(_controller.HasActiveModals, Is.False);
                Assert.That(_controller.IsActive(modalId), Is.False);
            }
        }

        [Test]
        public void SwitchingBetweenSubsystems_EnforcesMutualExclusivityAtRoot()
        {
            for (int i = 0; i < All12Subsystems.Length; i++)
            {
                var modal = All12Subsystems[i];
                _controller.PushRoot(modal);

                Assert.That(_controller.ActiveCount, Is.EqualTo(1));
                Assert.That(_controller.CurrentTop, Is.EqualTo(modal));
            }
        }

        [Test]
        public void PushChild_MaintainsNestedNavigationHistory()
        {
            _controller.PushRoot("DashboardModal");
            _controller.PushChild("StaffModal");
            _controller.PushChild("FinanceModal");

            Assert.That(_controller.ActiveCount, Is.EqualTo(3));
            Assert.That(_controller.CurrentTop, Is.EqualTo("FinanceModal"));
            Assert.That(_controller.Stack, Is.EqualTo(new[] { "DashboardModal", "StaffModal", "FinanceModal" }));

            // Pop top (Finance) -> returns to Staff
            var popped1 = _controller.Pop();
            Assert.That(popped1, Is.EqualTo("FinanceModal"));
            Assert.That(_controller.ActiveCount, Is.EqualTo(2));
            Assert.That(_controller.CurrentTop, Is.EqualTo("StaffModal"));

            // Pop top (Staff) -> returns to Dashboard
            var popped2 = _controller.Pop();
            Assert.That(popped2, Is.EqualTo("StaffModal"));
            Assert.That(_controller.ActiveCount, Is.EqualTo(1));
            Assert.That(_controller.CurrentTop, Is.EqualTo("DashboardModal"));

            // Pop top (Dashboard) -> returns to HUD
            var popped3 = _controller.Pop();
            Assert.That(popped3, Is.EqualTo("DashboardModal"));
            Assert.That(_controller.ActiveCount, Is.EqualTo(0));
            Assert.That(_controller.HasActiveModals, Is.False);
        }

        [Test]
        public void PushChild_DuplicateModal_RepositionedToTopWithoutDuplication()
        {
            _controller.PushRoot("DashboardModal");
            _controller.PushChild("StaffModal");
            _controller.PushChild("FinanceModal");

            // Re-push StaffModal
            _controller.PushChild("StaffModal");

            Assert.That(_controller.ActiveCount, Is.EqualTo(3));
            Assert.That(_controller.CurrentTop, Is.EqualTo("StaffModal"));
            Assert.That(_controller.Stack, Is.EqualTo(new[] { "DashboardModal", "FinanceModal", "StaffModal" }));
        }

        [Test]
        public void BackPress_OnNestedModal_ReturnsToParent()
        {
            _controller.PushRoot("DashboardModal");
            _controller.PushChild("StaffModal");

            var result = _controller.HandleBackPress();

            Assert.That(result, Is.EqualTo(BackPressResult.PoppedModal));
            Assert.That(_controller.ActiveCount, Is.EqualTo(1));
            Assert.That(_controller.CurrentTop, Is.EqualTo("DashboardModal"));
        }

        [Test]
        public void BackPress_OnRootModal_ReturnsToDashboard()
        {
            _controller.PushRoot("JobBoardModal");

            var result = _controller.HandleBackPress();

            Assert.That(result, Is.EqualTo(BackPressResult.PoppedModal));
            Assert.That(_controller.ActiveCount, Is.EqualTo(0));
            Assert.That(_controller.HasActiveModals, Is.False);
        }

        [Test]
        public void BackPress_OnMainDashboard_ShowsExitConfirmationDialog()
        {
            // When no modals are open
            Assert.That(_controller.HasActiveModals, Is.False);
            Assert.That(_controller.IsExitDialogOpen, Is.False);

            var result = _controller.HandleBackPress();

            Assert.That(result, Is.EqualTo(BackPressResult.ShowedExitDialog));
            Assert.That(_controller.IsExitDialogOpen, Is.True);
            Assert.That(_exitDialogToggles, Contains.Item(true));
        }

        [Test]
        public void BackPress_WhileExitDialogIsOpen_DismissesExitDialog()
        {
            _controller.ShowExitDialog();
            Assert.That(_controller.IsExitDialogOpen, Is.True);

            var result = _controller.HandleBackPress();

            Assert.That(result, Is.EqualTo(BackPressResult.DismissedExitDialog));
            Assert.That(_controller.IsExitDialogOpen, Is.False);
            Assert.That(_exitDialogToggles, Contains.Item(false));
        }

        [Test]
        public void ExitDialog_ConfirmExit_TriggersApplicationQuit()
        {
            _controller.ShowExitDialog();
            _controller.ConfirmExit();

            Assert.That(_controller.IsExitDialogOpen, Is.False);
            Assert.That(_quitInvoked, Is.True);
        }

        [Test]
        public void ExitDialog_Dismiss_ClosesWithoutQuitting()
        {
            _controller.ShowExitDialog();
            _controller.DismissExitDialog();

            Assert.That(_controller.IsExitDialogOpen, Is.False);
            Assert.That(_quitInvoked, Is.False);
        }

        [Test]
        public void OpeningModal_WhenExitDialogIsOpen_AutomaticallyDismissesExitDialog()
        {
            _controller.ShowExitDialog();
            Assert.That(_controller.IsExitDialogOpen, Is.True);

            _controller.PushRoot("SettingsModal");

            Assert.That(_controller.IsExitDialogOpen, Is.False);
            Assert.That(_controller.ActiveCount, Is.EqualTo(1));
            Assert.That(_controller.CurrentTop, Is.EqualTo("SettingsModal"));
        }

        [Test]
        public void RepeatedOpeningAndClosing_LeavesStateClean()
        {
            for (int round = 0; round < 5; round++)
            {
                foreach (var modal in All12Subsystems)
                {
                    _controller.PushRoot(modal);
                    Assert.That(_controller.ActiveCount, Is.EqualTo(1));

                    _controller.HandleBackPress();
                    Assert.That(_controller.ActiveCount, Is.EqualTo(0));
                }
            }
        }

        [Test]
        public void CloseAll_ClearsAllInReverseOrder()
        {
            _controller.PushRoot("Modal1");
            _controller.PushChild("Modal2");
            _controller.PushChild("Modal3");

            _controller.CloseAll();

            Assert.That(_controller.ActiveCount, Is.EqualTo(0));
            Assert.That(_controller.HasActiveModals, Is.False);
            Assert.That(_controller.CurrentTop, Is.Null);
            Assert.That(_deactivated, Is.EqualTo(new[] { "Modal3", "Modal2", "Modal1" }));
        }

        [Test]
        public void NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _controller.PushRoot(null));
            Assert.Throws<ArgumentNullException>(() => _controller.PushChild(null));
        }
    }
}
