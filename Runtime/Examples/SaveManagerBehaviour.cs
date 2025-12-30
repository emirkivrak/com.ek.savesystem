using UnityEngine;

namespace EK.SaveSystem.Examples
{
    /// <summary>
    /// Optional MonoBehaviour wrapper for SaveManager.
    /// Use this if you want to manage SaveManager lifecycle via Unity's component system.
    /// Alternatively, you can manage SaveManager directly in your DI container.
    /// </summary>
    public class SaveManagerBehaviour : MonoBehaviour
    {
        [Header("Save Settings")]
        [Tooltip("Enable automatic saving at regular intervals")]
        [SerializeField] private bool autoSaveEnabled = true;
        
        [Tooltip("Time in seconds between automatic saves")]
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        
        [Tooltip("Save all dirty data when application quits")]
        [SerializeField] private bool saveOnQuit = true;
        
        [Tooltip("Save all dirty data when application is paused (mobile background)")]
        [SerializeField] private bool saveOnPause = true;

        [Header("Encryption Settings")]
        [Tooltip("Enable AES encryption for all save files (default: OFF for plain JSON)")]
        [SerializeField] private bool enableEncryption = false;

        private SaveManager saveManager;

        /// <summary>
        /// Gets the managed SaveManager instance.
        /// </summary>
        public SaveManager SaveManager => saveManager;

        private void Awake()
        {
            // Create and initialize SaveManager
            saveManager = new SaveManager();
            
            var saveService = new LocalSaveService(enableEncryption: enableEncryption);
            
            saveManager.Initialize(
                saveService: saveService,
                autoSaveEnabled: autoSaveEnabled,
                autoSaveIntervalSeconds: autoSaveInterval,
                saveOnApplicationQuit: saveOnQuit,
                saveOnApplicationPause: saveOnPause
            );

            SaveSystemLogger.Log($"SaveManager initialized via MonoBehaviour. Encryption: {(enableEncryption ? "ON" : "OFF")}");
        }

        private void Update()
        {
            // Update auto-save timer
            if (saveManager != null && saveManager.IsInitialized)
            {
                saveManager.Update(Time.deltaTime);
            }
        }

        private void OnApplicationQuit()
        {
            saveManager?.OnApplicationQuit();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            saveManager?.OnApplicationPause(pauseStatus);
        }

        private void OnDestroy()
        {
            saveManager?.Dispose();
        }
    }
}

