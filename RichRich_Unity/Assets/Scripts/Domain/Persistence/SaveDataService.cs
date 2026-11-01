using System;
using System.IO;
using System.Text.Json;

namespace RichRich.Domain.Persistence
{
    public interface ISaveDataService
    {
        void Save(SaveData data);
        SaveData Load();
        void DeleteAll();
    }

    /// <summary>
    /// Robust, dual-file atomic persistence service.
    /// Writes via temporary staging files and maintains an automatic .backup.json
    /// to guarantee zero data loss upon crash, unexpected power cut, or corruption.
    /// </summary>
    public class SaveDataService : ISaveDataService
    {
        public string StorageDirectory { get; }
        public string PrimaryPath => Path.Combine(StorageDirectory, "richrich_save.json");
        public string BackupPath => Path.Combine(StorageDirectory, "richrich_save.backup.json");
        public string TempPath => Path.Combine(StorageDirectory, "richrich_save.tmp");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        public SaveDataService(string storageDirectory)
        {
            StorageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            if (!Directory.Exists(StorageDirectory))
            {
                Directory.CreateDirectory(StorageDirectory);
            }
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonSerializer.Serialize(data, JsonOptions);

            // 1. Write staged content to temporary file
            File.WriteAllText(TempPath, json);

            // 2. Rotate existing primary save to backup
            if (File.Exists(PrimaryPath))
            {
                try
                {
                    File.Copy(PrimaryPath, BackupPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    OnLogWarning?.Invoke($"Failed to rotate primary save to backup: {ex.Message}");
                }
            }

            // 3. Atomically replace primary save with staged file
            File.Move(TempPath, PrimaryPath, overwrite: true);
        }

        public SaveData Load()
        {
            // 1. Attempt loading primary save
            if (File.Exists(PrimaryPath))
            {
                try
                {
                    string json = File.ReadAllText(PrimaryPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                        if (data != null && data.SchemaVersion >= 1)
                        {
                            return data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLogWarning?.Invoke($"Primary save file is corrupted or unreadable: {ex.Message}. Falling back to backup.");
                }
            }

            // 2. Fallback to backup save
            if (File.Exists(BackupPath))
            {
                try
                {
                    string json = File.ReadAllText(BackupPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                        if (data != null && data.SchemaVersion >= 1)
                        {
                            OnLogWarning?.Invoke("Successfully recovered player save state from backup file.");
                            return data;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLogError?.Invoke($"Backup save file is also corrupted: {ex.Message}. Resetting to safe defaults.");
                }
            }

            // 3. Both missing or corrupted -> Return clean default state
            return SaveData.CreateDefault();
        }

        public void DeleteAll()
        {
            if (File.Exists(PrimaryPath)) File.Delete(PrimaryPath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
    }
}
