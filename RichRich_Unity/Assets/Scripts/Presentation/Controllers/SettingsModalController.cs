using System;
using RichRich.Domain.Persistence;

namespace RichRich.Presentation.Controllers
{
    /// <summary>
    /// Presentation controller for the Settings modal:
    /// Audio toggle, haptics toggle, save reset, and version diagnostics.
    /// Pure C# view-model controller with zero MonoBehaviour coupling.
    /// </summary>
    public class SettingsModalController
    {
        public bool SoundEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public string VersionText => "RichRich v0.2.0";
        public string PackageIdText => "com.bishal.richrich";

        public event Action OnSettingsChanged;
        public event Action OnSaveReset;

        public SettingsModalController(bool soundEnabled = true, bool hapticsEnabled = true)
        {
            SoundEnabled = soundEnabled;
            HapticsEnabled = hapticsEnabled;
        }

        public void SetSoundEnabled(bool enabled)
        {
            if (SoundEnabled == enabled) return;
            SoundEnabled = enabled;
            OnSettingsChanged?.Invoke();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            if (HapticsEnabled == enabled) return;
            HapticsEnabled = enabled;
            OnSettingsChanged?.Invoke();
        }

        public void ResetProgress(SaveDataService saveService)
        {
            if (saveService == null) throw new ArgumentNullException(nameof(saveService));
            saveService.DeleteAll();
            OnSaveReset?.Invoke();
        }
    }
}
