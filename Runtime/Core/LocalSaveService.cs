using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace EK.SaveSystem
{
    /// <summary>
    /// Local file-based save service implementation with optional encryption and backup support.
    /// </summary>
    public class LocalSaveService : ISaveService
    {
        private const string SaveExtension = ".sav";
        private const string BackupExtension = ".backup";
        
        private readonly string saveDirectory;
        private readonly string encryptionKey;
        private readonly bool useEncryption;

        /// <summary>
        /// Creates a new LocalSaveService instance.
        /// </summary>
        /// <param name="customDirectory">Optional custom save directory. Uses Application.persistentDataPath if null.</param>
        /// <param name="enableEncryption">Enable AES encryption for save files. Default is false (plain JSON).</param>
        /// <param name="customEncryptionKey">Optional custom encryption key. Uses device identifier if null.</param>
        public LocalSaveService(string customDirectory = null, bool enableEncryption = false, string customEncryptionKey = null)
        {
            saveDirectory = customDirectory ?? Application.persistentDataPath;
            useEncryption = enableEncryption;

            if (useEncryption)
            {
                encryptionKey = customEncryptionKey ?? GenerateDeviceKey();
                SaveSystemLogger.Log($"Save service initialized with ENCRYPTION. Directory: {saveDirectory}");
            }
            else
            {
                SaveSystemLogger.Log($"Save service initialized with PLAIN JSON. Directory: {saveDirectory}");
            }

            // Ensure save directory exists
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
        }

        /// <summary>
        /// Saves data asynchronously with encryption and automatic backup.
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
            {
                SaveSystemLogger.LogError("Save key cannot be null or empty");
                throw new ArgumentException("Save key cannot be null or empty", nameof(key));
            }

            if (data == null)
            {
                SaveSystemLogger.LogError("Cannot save null data");
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                string savePath = GetSavePath(key);
                string backupPath = GetBackupPath(key);

                // Call OnBeforeSave if data inherits from SaveData
                if (data is SaveData saveData)
                {
                    saveData.OnBeforeSave();
                }

                // Serialize to JSON
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                SaveSystemLogger.Log($"Serialized data for key '{key}': {json.Length} characters");

                // Encrypt the JSON if encryption is enabled
                string content = useEncryption 
                    ? EncryptionHelper.Encrypt(json, encryptionKey)
                    : json;

                // Create backup of existing save if it exists
                if (File.Exists(savePath))
                {
                    await UniTask.RunOnThreadPool(() =>
                    {
                        File.Copy(savePath, backupPath, true);
                    });
                    SaveSystemLogger.Log($"Backup created for key '{key}'");
                }

                // Write data to file
                await UniTask.RunOnThreadPool(() =>
                {
                    File.WriteAllText(savePath, content);
                });

                SaveSystemLogger.Log($"Successfully saved data for key '{key}'");
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to save data for key '{key}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads data asynchronously with decryption.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                SaveSystemLogger.LogError("Load key cannot be null or empty");
                throw new ArgumentException("Load key cannot be null or empty", nameof(key));
            }

            try
            {
                string savePath = GetSavePath(key);
                string backupPath = GetBackupPath(key);

                // Try to load from main save file
                if (File.Exists(savePath))
                {
                    try
                    {
                        return await LoadFromFile<T>(savePath, key);
                    }
                    catch (Exception ex)
                    {
                        SaveSystemLogger.LogWarning($"Failed to load from main save, trying backup: {ex.Message}");
                        
                        // Try backup if main file fails
                        if (File.Exists(backupPath))
                        {
                            SaveSystemLogger.Log($"Attempting to restore from backup for key '{key}'");
                            return await LoadFromFile<T>(backupPath, key);
                        }
                        throw;
                    }
                }
                else if (File.Exists(backupPath))
                {
                    SaveSystemLogger.LogWarning($"Main save not found, loading from backup for key '{key}'");
                    return await LoadFromFile<T>(backupPath, key);
                }
                else
                {
                    SaveSystemLogger.LogWarning($"No save file found for key '{key}'");
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to load data for key '{key}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a save file and its backup.
        /// </summary>
        public void DeleteSave(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                SaveSystemLogger.LogError("Delete key cannot be null or empty");
                throw new ArgumentException("Delete key cannot be null or empty", nameof(key));
            }

            try
            {
                string savePath = GetSavePath(key);
                string backupPath = GetBackupPath(key);

                bool deleted = false;

                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    deleted = true;
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    deleted = true;
                }

                if (deleted)
                {
                    SaveSystemLogger.Log($"Deleted save for key '{key}'");
                }
                else
                {
                    SaveSystemLogger.LogWarning($"No save file found to delete for key '{key}'");
                }
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to delete save for key '{key}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public bool HasSave(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string savePath = GetSavePath(key);
            return File.Exists(savePath);
        }

        /// <summary>
        /// Deletes all save files and backups.
        /// </summary>
        public void ClearAllSaves()
        {
            try
            {
                var saveFiles = Directory.GetFiles(saveDirectory, $"*{SaveExtension}");
                var backupFiles = Directory.GetFiles(saveDirectory, $"*{BackupExtension}");

                foreach (var file in saveFiles.Concat(backupFiles))
                {
                    File.Delete(file);
                }

                SaveSystemLogger.Log($"Cleared all saves. Deleted {saveFiles.Length} save files and {backupFiles.Length} backup files.");
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to clear all saves: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all save keys currently stored.
        /// </summary>
        public string[] GetAllSaveKeys()
        {
            try
            {
                var saveFiles = Directory.GetFiles(saveDirectory, $"*{SaveExtension}");
                return saveFiles
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToArray();
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to get save keys: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Saves data synchronously. Blocks until complete.
        /// </summary>
        public void Save<T>(string key, T data)
        {
            SaveAsync(key, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Loads data synchronously. Blocks until complete.
        /// </summary>
        public T Load<T>(string key)
        {
            return LoadAsync<T>(key).GetAwaiter().GetResult();
        }

        #region Private Methods

        private async UniTask<T> LoadFromFile<T>(string filePath, string key)
        {
            // Read data
            string content = await UniTask.RunOnThreadPool(() =>
            {
                return File.ReadAllText(filePath);
            });

            // Decrypt if encryption is enabled
            string json = useEncryption
                ? EncryptionHelper.Decrypt(content, encryptionKey)
                : content;
            
            SaveSystemLogger.Log($"Loaded data for key '{key}': {json.Length} characters");

            // Deserialize
            T data = JsonConvert.DeserializeObject<T>(json);

            // Call OnAfterLoad if data inherits from SaveData
            if (data is SaveData saveData)
            {
                saveData.OnAfterLoad();
            }

            SaveSystemLogger.Log($"Successfully loaded data for key '{key}'");
            return data;
        }

        private string GetSavePath(string key)
        {
            return Path.Combine(saveDirectory, key + SaveExtension);
        }

        private string GetBackupPath(string key)
        {
            return Path.Combine(saveDirectory, key + BackupExtension);
        }

        private string GenerateDeviceKey()
        {
            // Use device identifier for basic encryption
            // In production, consider more sophisticated key management
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            
            // Fallback if device ID is not available
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = "DefaultEncryptionKey_ChangeInProduction";
                SaveSystemLogger.LogWarning("Device ID not available, using default encryption key");
            }

            return deviceId;
        }

        #endregion
    }
}

