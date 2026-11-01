using System;
using System.IO;
using NUnit.Framework;
using RichRich.Domain.Models;
using RichRich.Domain.Persistence;
using RichRich.Domain.Services;

namespace RichRich.Tests
{
    [TestFixture]
    public class PersistenceTests
    {
        private string _testDir;
        private SaveDataService _service;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "RichRich_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _service = new SaveDataService(_testDir);
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
        public void SaveDataService_SaveAndLoad_PreservesAllFields()
        {
            var save = new SaveData
            {
                SchemaVersion = 1,
                CreatedAtUnix = 1700000000L,
                LastSavedUnix = 1700000500L,
                BalanceCents = 2500000L, // $25,000.00
                LifetimeEarnedCents = 5000000L, // $50,000.00
                CurrentLevel = 3,
                Tickets = 5,
                BusinessId = "auto_repair",
                IsBusinessOwned = true,
                BusinessLevel = 4,
                BusinessTasksCompleted = 12,
                HapticsEnabled = false,
                SoundEnabled = true
            };

            _service.Save(save);
            Assert.That(File.Exists(_service.PrimaryPath), Is.True);

            var loaded = _service.Load();
            Assert.That(loaded.BalanceCents, Is.EqualTo(2500000L));
            Assert.That(loaded.LifetimeEarnedCents, Is.EqualTo(5000000L));
            Assert.That(loaded.CurrentLevel, Is.EqualTo(3));
            Assert.That(loaded.Tickets, Is.EqualTo(5));
            Assert.That(loaded.IsBusinessOwned, Is.True);
            Assert.That(loaded.BusinessLevel, Is.EqualTo(4));
            Assert.That(loaded.BusinessTasksCompleted, Is.EqualTo(12));
            Assert.That(loaded.HapticsEnabled, Is.False);
        }

        [Test]
        public void SaveDataService_PrimaryCorrupted_RecoversFromBackup()
        {
            // 1. Initial valid save
            var initial = new SaveData
            {
                BalanceCents = 100000L,
                BusinessLevel = 2,
                IsBusinessOwned = true
            };
            _service.Save(initial);

            // 2. Second valid save (creates backup of state 1)
            var updated = new SaveData
            {
                BalanceCents = 200000L,
                BusinessLevel = 3,
                IsBusinessOwned = true
            };
            _service.Save(updated);

            Assert.That(File.Exists(_service.PrimaryPath), Is.True);
            Assert.That(File.Exists(_service.BackupPath), Is.True);

            // 3. Corrupt primary file with garbage bytes
            File.WriteAllText(_service.PrimaryPath, "{ corrupted json !@#$%^&* }");

            // 4. Load should catch corruption and recover from backup
            var recovered = _service.Load();
            Assert.That(recovered, Is.Not.Null);
            // Backup contains the state from save 1
            Assert.That(recovered.BalanceCents, Is.EqualTo(100000L));
            Assert.That(recovered.BusinessLevel, Is.EqualTo(2));
        }

        [Test]
        public void SaveDataService_BothCorrupted_ReturnsSafeDefaultState()
        {
            // Corrupt both files
            File.WriteAllText(_service.PrimaryPath, "GARBAGE_DATA_1");
            File.WriteAllText(_service.BackupPath, "GARBAGE_DATA_2");

            var loaded = _service.Load();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.BalanceCents, Is.EqualTo(GameStartConfig.GetStartingCash(GameDifficulty.Standard).MinorUnits));
            Assert.That(loaded.CurrentLevel, Is.EqualTo(1));
            Assert.That(loaded.IsBusinessOwned, Is.False);
            Assert.That(loaded.Tickets, Is.EqualTo(EconomyConfig.StartingTickets));
        }

        [Test]
        public void OfflineIncome_NormalDuration_ComputesExpectedYield()
        {
            long lastSaved = 1000;
            long now = 1000 + 3600; // 1 hour later
            var hourlyIncome = Currency.FromDollars(120.00); // $120.00/hr

            var result = OfflineIncomeCalculator.Calculate(lastSaved, now, hourlyIncome);

            Assert.That(result.IsClockTampered, Is.False);
            Assert.That(result.WasCapped, Is.False);
            Assert.That(result.SecondsClaimed, Is.EqualTo(3600L));
            Assert.That(result.AmountEarned, Is.EqualTo(Currency.FromDollars(120.00)));
        }

        [Test]
        public void OfflineIncome_ExceedsCap_EnforcesExact3HourLimit()
        {
            long lastSaved = 1000;
            long now = 1000 + (10 * 3600); // 10 hours later!
            var hourlyIncome = Currency.FromDollars(100.00);

            var result = OfflineIncomeCalculator.Calculate(lastSaved, now, hourlyIncome, maxOfflineSeconds: 3 * 3600);

            Assert.That(result.IsClockTampered, Is.False);
            Assert.That(result.WasCapped, Is.True);
            Assert.That(result.SecondsClaimed, Is.EqualTo(3 * 3600L)); // Exactly 3 hours
            Assert.That(result.AmountEarned, Is.EqualTo(Currency.FromDollars(300.00))); // 3 * $100
        }

        [Test]
        public void OfflineIncome_ClockRollback_DetectsTamperAndAwardsZero()
        {
            long lastSaved = 5000;
            long now = 4000; // Player rolled clock backward by 1000 seconds!
            var hourlyIncome = Currency.FromDollars(500.00);

            var result = OfflineIncomeCalculator.Calculate(lastSaved, now, hourlyIncome);

            Assert.That(result.IsClockTampered, Is.True);
            Assert.That(result.AmountEarned, Is.EqualTo(Currency.Zero));
            Assert.That(result.SecondsClaimed, Is.EqualTo(0L));
        }
    }
}
