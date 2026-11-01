using System;
using UnityEngine;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Presentation.Views
{
    /// <summary>
    /// Unity lifecycle entry-point and scene bootstrapper for the Life & Wealth simulation.
    /// Wires together the persistent life simulation coordinator, controllers, and UI views.
    /// Handles frame updates, application pause/resume, and crash-resilient auto-saving.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private MainHUDView _hudView;
        [SerializeField] private BusinessShopView _shopView;
        [SerializeField] private BusinessTaskView _taskView;
        [SerializeField] private SettingsModalView _settingsView;

        public LifeSimulationCoordinator LifeCoordinator { get; private set; }
        public LifeSimulationHUDController LifeHUDController { get; private set; }
        public GameSessionCoordinator LegacyCoordinator { get; private set; }

        private void Awake()
        {
            // Configure Android target framerate and screen sleep
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Initialize Persistence with platform persistent data path
            string saveDir = Application.persistentDataPath;
            var saveService = new SaveDataService(saveDir);

            // Initialize Life & Wealth Simulation Master Coordinator
            LifeCoordinator = new LifeSimulationCoordinator(saveService);
            LifeCoordinator.InitializeSession(GameDifficulty.Standard);

            // Life Simulation HUD Controller
            LifeHUDController = new LifeSimulationHUDController(LifeCoordinator);

            // Bind Views to Life Simulation Controllers
            if (_hudView != null)
            {
                _hudView.Bind(LifeHUDController);
            }

            // Legacy business / shop views wired through session coordinator if present in scene
            var timeProvider = new SystemTimeProvider();
            LegacyCoordinator = new GameSessionCoordinator(saveService, timeProvider);
            LegacyCoordinator.InitializeSession();

            if (_shopView != null)
            {
                _shopView.Bind(LegacyCoordinator.BusinessController);
            }

            if (_taskView != null)
            {
                _taskView.Bind(LegacyCoordinator.TaskController);
            }

            if (_settingsView != null)
            {
                _settingsView.Bind(LegacyCoordinator.SettingsController, saveService);
            }
        }

        private void Update()
        {
            LifeCoordinator?.Tick(Time.deltaTime);
            LegacyCoordinator?.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LifeCoordinator?.SaveCurrentState();
                LegacyCoordinator?.SaveCurrentState();
            }
        }

        private void OnApplicationQuit()
        {
            LifeCoordinator?.SaveCurrentState();
            LegacyCoordinator?.SaveCurrentState();
        }
    }
}
