using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace EK.SaveSystem
{
    /// <summary>
    /// Centralized manager for tracking and saving dirty save data.
    /// Designed for use with Dependency Injection or Service Locator patterns.
    /// Call Initialize() after construction to set up the manager.
    /// </summary>
    public class SaveManager
    {
        // Configuration
        private bool autoSaveEnabled = true;
        private float autoSaveInterval = 300f; // 5 minutes
        private bool saveOnQuit = true;
        private bool saveOnPause = true;

        private ISaveService saveService;
        private HashSet<SaveData> dirtyData = new HashSet<SaveData>();
        private Dictionary<string, SaveData> registeredData = new Dictionary<string, SaveData>();
        private float timeSinceLastSave;
        private bool isSaving;
        private bool isInitialized;

        /// <summary>
        /// Whether a save operation is currently in progress.
        /// </summary>
        public bool IsSaving => isSaving;

        /// <summary>
        /// Number of save data objects that have unsaved changes.
        /// </summary>
        public int DirtyCount => dirtyData.Count;

        /// <summary>
        /// Whether there are any unsaved changes.
        /// </summary>
        public bool HasDirtyData => dirtyData.Count > 0;

        /// <summary>
        /// Whether the SaveManager has been initialized.
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Initializes the SaveManager with the provided save service and configuration.
        /// Call this after construction, typically in your game's initialization code or DI container setup.
        /// </summary>
        /// <param name="saveService">The save service to use. If null, creates a LocalSaveService with default settings.</param>
        /// <param name="autoSaveEnabled">Enable automatic saving at regular intervals.</param>
        /// <param name="autoSaveIntervalSeconds">Time in seconds between automatic saves.</param>
        /// <param name="saveOnApplicationQuit">Save all dirty data when application quits.</param>
        /// <param name="saveOnApplicationPause">Save all dirty data when application is paused (mobile).</param>
        public void Initialize(
            ISaveService saveService = null,
            bool autoSaveEnabled = true,
            float autoSaveIntervalSeconds = 300f,
            bool saveOnApplicationQuit = true,
            bool saveOnApplicationPause = true)
        {
            if (isInitialized)
            {
                SaveSystemLogger.LogWarning("SaveManager already initialized. Skipping.");
                return;
            }

            this.saveService = saveService ?? new LocalSaveService(enableEncryption: false);
            this.autoSaveEnabled = autoSaveEnabled;
            this.autoSaveInterval = autoSaveIntervalSeconds;
            this.saveOnQuit = saveOnApplicationQuit;
            this.saveOnPause = saveOnApplicationPause;

            isInitialized = true;

            // Register with SaveServiceLocator for SaveData access
            SaveServiceLocator.RegisterManager(this);

            SaveSystemLogger.Log("SaveManager initialized successfully");
        }

        /// <summary>
        /// Updates the auto-save timer. Call this from your game loop (e.g., MonoBehaviour.Update).
        /// Only needed if auto-save is enabled.
        /// </summary>
        /// <param name="deltaTime">Time since last update (typically Time.deltaTime).</param>
        public void Update(float deltaTime)
        {
            if (!isInitialized)
            {
                SaveSystemLogger.LogWarning("SaveManager not initialized. Call Initialize() first.");
                return;
            }

            if (autoSaveEnabled && !isSaving && HasDirtyData)
            {
                timeSinceLastSave += deltaTime;

                if (timeSinceLastSave >= autoSaveInterval)
                {
                    SaveAllDirtyAsync().Forget();
                    timeSinceLastSave = 0f;
                }
            }
        }

        /// <summary>
        /// Handles application quit event. Call this from your quit handler.
        /// </summary>
        public void OnApplicationQuit()
        {
            if (!isInitialized) return;

            if (saveOnQuit && HasDirtyData)
            {
                SaveSystemLogger.Log("Application quitting - saving all dirty data...");
                SaveAllDirtySync();
            }
        }

        /// <summary>
        /// Handles application pause event. Call this from your pause handler.
        /// </summary>
        /// <param name="pauseStatus">True if application is pausing, false if resuming.</param>
        public void OnApplicationPause(bool pauseStatus)
        {
            if (!isInitialized) return;

            if (saveOnPause && pauseStatus && HasDirtyData && !isSaving)
            {
                SaveSystemLogger.Log("Application paused - saving all dirty data...");
                SaveAllDirtyAsync().Forget();
            }
        }

        /// <summary>
        /// Disposes the SaveManager and unregisters from the service locator.
        /// </summary>
        public void Dispose()
        {
            if (HasDirtyData)
            {
                SaveSystemLogger.LogWarning("SaveManager disposed with unsaved data!");
            }

            SaveServiceLocator.UnregisterManager();
            isInitialized = false;
            SaveSystemLogger.Log("SaveManager disposed");
        }

        /// <summary>
        /// Internal method called by SaveData when marked dirty.
        /// Do not call this directly - use SetDirty() in your SaveData classes.
        /// </summary>
        internal void RegisterDirty(SaveData data)
        {
            if (data != null && !dirtyData.Contains(data))
            {
                dirtyData.Add(data);
                SaveSystemLogger.Log($"'{data.SaveKey}' marked dirty (Total dirty: {dirtyData.Count})");
            }
        }

        /// <summary>
        /// Registers a SaveData instance for automatic management.
        /// Call this after loading or creating new save data.
        /// </summary>
        public void Register(SaveData data)
        {
            if (data == null)
            {
                SaveSystemLogger.LogError("Cannot register null data");
                return;
            }

            string key = data.SaveKey;
            if (!registeredData.ContainsKey(key))
            {
                registeredData[key] = data;
                SaveSystemLogger.Log($"Registered save data: {key}");
            }
        }

        /// <summary>
        /// Unregisters a SaveData instance from management.
        /// </summary>
        public void Unregister(SaveData data)
        {
            if (data == null) return;

            string key = data.SaveKey;
            registeredData.Remove(key);
            dirtyData.Remove(data);
            SaveSystemLogger.Log($"Unregistered save data: {key}");
        }

        /// <summary>
        /// Saves all dirty data asynchronously (non-blocking).
        /// Recommended for runtime use.
        /// </summary>
        public async UniTask SaveAllDirtyAsync()
        {
            if (isSaving)
            {
                SaveSystemLogger.LogWarning("Save already in progress, skipping");
                return;
            }

            if (!HasDirtyData)
            {
                SaveSystemLogger.Log("No dirty data to save");
                return;
            }

            isSaving = true;
            int savedCount = 0;
            int errorCount = 0;

            try
            {
                SaveSystemLogger.Log($"Saving {dirtyData.Count} dirty save objects...");

                // Create a copy to avoid modification during iteration
                var dataToSave = dirtyData.ToList();

                foreach (var data in dataToSave)
                {
                    try
                    {
                        await saveService.SaveAsync(data.SaveKey, data);
                        data.ClearDirty();
                        dirtyData.Remove(data);
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        SaveSystemLogger.LogError($"Failed to save '{data.SaveKey}': {ex.Message}");
                        errorCount++;
                    }
                }

                SaveSystemLogger.Log($"✓ Save complete: {savedCount} saved, {errorCount} errors");
            }
            finally
            {
                isSaving = false;
                timeSinceLastSave = 0f; // Reset auto-save timer
            }
        }

        /// <summary>
        /// Saves all dirty data synchronously (blocking).
        /// Use for critical moments like application quit.
        /// </summary>
        public void SaveAllDirtySync()
        {
            if (!HasDirtyData)
            {
                SaveSystemLogger.Log("No dirty data to save");
                return;
            }

            SaveSystemLogger.Log($"Sync saving {dirtyData.Count} dirty save objects...");
            int savedCount = 0;
            int errorCount = 0;

            var dataToSave = dirtyData.ToList();
            foreach (var data in dataToSave)
            {
                try
                {
                    saveService.Save(data.SaveKey, data);
                    data.ClearDirty();
                    dirtyData.Remove(data);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    SaveSystemLogger.LogError($"Failed to sync save '{data.SaveKey}': {ex.Message}");
                    errorCount++;
                }
            }

            SaveSystemLogger.Log($"✓ Sync save complete: {savedCount} saved, {errorCount} errors");
        }

        /// <summary>
        /// Saves a specific SaveData object immediately, regardless of dirty state.
        /// </summary>
        public async UniTask SaveAsync(SaveData data)
        {
            if (data == null)
            {
                SaveSystemLogger.LogError("Cannot save null data");
                return;
            }

            try
            {
                await saveService.SaveAsync(data.SaveKey, data);
                data.ClearDirty();
                dirtyData.Remove(data);
                SaveSystemLogger.Log($"✓ Saved: {data.SaveKey}");
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to save '{data.SaveKey}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads SaveData by type. Automatically registers it after loading.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key) where T : SaveData
        {
            try
            {
                T data = await saveService.LoadAsync<T>(key);
                if (data != null)
                {
                    Register(data);
                    SaveSystemLogger.Log($"✓ Loaded and registered: {key}");
                }
                else
                {
                    SaveSystemLogger.LogWarning($"No save found for key: {key}");
                }
                return data;
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to load '{key}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads SaveData synchronously. Automatically registers it after loading.
        /// </summary>
        public T Load<T>(string key) where T : SaveData
        {
            try
            {
                T data = saveService.Load<T>(key);
                if (data != null)
                {
                    Register(data);
                    SaveSystemLogger.Log($"✓ Loaded and registered: {key}");
                }
                else
                {
                    SaveSystemLogger.LogWarning($"No save found for key: {key}");
                }
                return data;
            }
            catch (Exception ex)
            {
                SaveSystemLogger.LogError($"Failed to load '{key}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Forces all registered data to be marked dirty and saved.
        /// </summary>
        public async UniTask SaveAllAsync()
        {
            foreach (var data in registeredData.Values)
            {
                data.ForceDirty();
            }
            await SaveAllDirtyAsync();
        }

        /// <summary>
        /// Deletes a save file.
        /// </summary>
        public void DeleteSave(string key)
        {
            saveService.DeleteSave(key);
            
            // Remove from registered data if it exists
            if (registeredData.TryGetValue(key, out var data))
            {
                Unregister(data);
            }
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public bool HasSave(string key)
        {
            return saveService.HasSave(key);
        }

        /// <summary>
        /// Gets debug information about current state.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Registered: {registeredData.Count} | Dirty: {dirtyData.Count} | Saving: {isSaving}";
        }
    }
}

