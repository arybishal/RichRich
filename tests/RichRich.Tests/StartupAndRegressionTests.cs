using System;
using System.IO;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;
using RichRich.Presentation.Controllers;

namespace RichRich.Tests
{
    [TestFixture]
    public class StartupAndRegressionTests
    {
        private string _testDir;
        private SaveDataService _saveService;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_StartupRegTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _saveService = new SaveDataService(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        [Test]
        public void SettingsModalController_Reports_CorrectVersionAndPackageId()
        {
            var controller = new SettingsModalController();
            Assert.That(controller.VersionText, Does.Contain("0.1.3"));
            Assert.That(controller.PackageIdText, Is.EqualTo("com.bishal.richrich"));
        }

        [Test]
        public void SaveDataService_CorruptedPrimaryJson_RecoversFromBackupWithoutException()
        {
            // 1. Write valid backup directly
            var validData = SaveData.CreateDefault(1700000000L, GameDifficulty.Comfortable);
            validData.BalanceCents = 99900L;
            _saveService.Save(validData);
            // Rotate so backup exists
            File.Copy(_saveService.PrimaryPath, _saveService.BackupPath, overwrite: true);

            // 2. Corrupt the primary file intentionally
            File.WriteAllText(_saveService.PrimaryPath, "{ corrupted json: !!! broken syntax");

            // 3. Load should recover from backup without throwing
            SaveData recovered = _saveService.Load();
            Assert.That(recovered, Is.Not.Null);
            Assert.That(recovered.BalanceCents, Is.EqualTo(99900L));
        }

        [Test]
        public void SaveDataService_BothFilesCorrupted_FallsBackToDefaultWithoutCrashing()
        {
            File.WriteAllText(_saveService.PrimaryPath, "INVALID_SYNTAX");
            File.WriteAllText(_saveService.BackupPath, "ALSO_INVALID");

            SaveData fallback = null;
            Assert.DoesNotThrow(() =>
            {
                fallback = _saveService.Load();
            });

            Assert.That(fallback, Is.Not.Null);
            Assert.That(fallback.SchemaVersion, Is.EqualTo(5));
            Assert.That(fallback.BalanceCents, Is.EqualTo(GameStartConfig.GetStartingCash(GameDifficulty.Standard).MinorUnits));
        }

        [Test]
        public void LifeSimulationCoordinator_SaveAndReload_PreservesAllMilestoneSystems()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // 1. Acquire business and change career
            coordinator.Wallet.Add(Currency.FromDollars(10000.00));
            bool bizBought = coordinator.BusinessService.TryPurchase("auto_repair", out string bizErr);
            Assert.That(bizBought, Is.True, bizErr);

            bool hired = coordinator.CareerService.TryApply("job_retail_assistant", out string careerErr);
            Assert.That(hired, Is.True, careerErr);

            // 2. Save
            coordinator.SaveCurrentState();

            // 3. Reload into fresh coordinator
            var freshCoordinator = new LifeSimulationCoordinator(_saveService);
            freshCoordinator.InitializeSession(GameDifficulty.Standard);

            Assert.That(freshCoordinator.BusinessService.GetBusiness("auto_repair").IsOwned, Is.True);
            Assert.That(freshCoordinator.CareerService.IsEmployed, Is.True);
            Assert.That(freshCoordinator.CareerService.CurrentEmployment.Job.Id, Is.EqualTo("job_retail_assistant"));
            Assert.That(freshCoordinator.BalanceSheet.IsNegativeNetWorth, Is.False);
        }

        [Test]
        public void LifeSimulationCoordinator_RapidSimulationAdvance_DoesNotThrowOrCorrupt()
        {
            var coordinator = new LifeSimulationCoordinator(_saveService);
            coordinator.InitializeSession(GameDifficulty.Standard);

            // Advance through 60 days (multiple pay periods and expense cycles)
            Assert.DoesNotThrow(() =>
            {
                for (int day = 0; day < 60; day++)
                {
                    coordinator.FastForwardDays(1);
                    coordinator.Tick(0.1);
                }
            });

            Assert.That(coordinator.Clock.CurrentDate.Day, Is.GreaterThan(0));
            Assert.That(coordinator.Wallet.Balance.MinorUnits, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void SaveDataService_BackwardCompatibility_Schema1ToSchema4()
        {
            // Simulate an existing player save from Schema 1 prototype
            var schema1Json = @"{
                ""SchemaVersion"": 1,
                ""CreatedAtUnix"": 1690000000,
                ""LastSavedUnix"": 1690001000,
                ""BalanceCents"": 150000,
                ""LifetimeEarnedCents"": 200000,
                ""CurrentLevel"": 2,
                ""BusinessId"": ""auto_repair"",
                ""IsBusinessOwned"": true,
                ""BusinessLevel"": 2,
                ""HapticsEnabled"": true,
                ""SoundEnabled"": true
            }";

            File.WriteAllText(_saveService.PrimaryPath, schema1Json);

            var coordinator = new LifeSimulationCoordinator(_saveService);
            Assert.DoesNotThrow(() =>
            {
                coordinator.InitializeSession(GameDifficulty.Standard);
            });

            Assert.That(coordinator.Wallet.Balance, Is.EqualTo(Currency.FromCents(150000)));
            Assert.That(coordinator.BusinessService.GetBusiness("auto_repair").IsOwned, Is.True);
        }
    }
}
