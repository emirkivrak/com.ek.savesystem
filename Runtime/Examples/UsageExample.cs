using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EK.SaveSystem.Examples
{
    /// <summary>
    /// Example MonoBehaviour showing how to use the Save System with dirty tracking.
    /// Requires a SaveManager to be initialized (either via SaveManagerBehaviour or DI container).
    /// </summary>
    public class UsageExample : MonoBehaviour
    {
        private SaveManager saveManager;
        private PlayerSave playerSave;
        private GameSettingsSave settingsSave;

        async void Start()
        {
            // Get SaveManager from service locator
            // (It should be initialized by SaveManagerBehaviour or your DI container)
            saveManager = SaveServiceLocator.Current;

            if (saveManager == null || !saveManager.IsInitialized)
            {
                Debug.LogError("SaveManager not initialized! Add SaveManagerBehaviour to your scene or initialize via DI.");
                return;
            }

            // Load or create player save
            playerSave = await saveManager.LoadAsync<PlayerSave>("player_data");
            if (playerSave == null)
            {
                // Create new player save
                playerSave = new PlayerSave
                {
                    PlayerName = "NewPlayer",
                    IsAlive = true,
                    Level = 1,
                    Gold = 100,
                    Health = 100f,
                    Position = new float[] { 0f, 0f, 0f }
                };
                saveManager.Register(playerSave);
                
                // Force save for new data
                await saveManager.SaveAsync(playerSave);
            }

            // Load or create settings
            settingsSave = await saveManager.LoadAsync<GameSettingsSave>("game_settings");
            if (settingsSave == null)
            {
                settingsSave = new GameSettingsSave(); // Uses default values
                saveManager.Register(settingsSave);
                await saveManager.SaveAsync(settingsSave);
            }

            Debug.Log($"Loaded player: {playerSave.PlayerName}, Level {playerSave.Level}");
            Debug.Log($"Settings - Music: {settingsSave.MusicVolume}, Language: {settingsSave.Language}");
        }

        void Update()
        {
            if (saveManager == null || playerSave == null || settingsSave == null) return;

            // Example: Modify player data (automatically marked as dirty)
            if (Input.GetKeyDown(KeyCode.G))
            {
                playerSave.AddGold(10);
                Debug.Log($"Added gold! Total: {playerSave.Gold} (Dirty: {playerSave.IsDirty})");
                // SaveManager will auto-save this in 5 minutes, or on quit/pause
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                playerSave.Level++;
                Debug.Log($"Leveled up! Level: {playerSave.Level} (Dirty: {playerSave.IsDirty})");
            }

            // Example: Modify settings (automatically marked as dirty)
            if (Input.GetKeyDown(KeyCode.M))
            {
                settingsSave.MusicVolume = Random.Range(0f, 1f);
                Debug.Log($"Changed music volume: {settingsSave.MusicVolume} (Dirty: {settingsSave.IsDirty})");
            }

            // Manual save (saves all dirty data immediately)
            if (Input.GetKeyDown(KeyCode.S))
            {
                saveManager.SaveAllDirtyAsync().Forget();
                Debug.Log("Manually saving all dirty data...");
            }

            // Debug info
            if (Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log($"SaveManager: {saveManager.GetDebugInfo()}");
                Debug.Log($"Player dirty: {playerSave.IsDirty}, Settings dirty: {settingsSave.IsDirty}");
            }
        }

        // Example: Save on demand
        public async void SavePlayerNow()
        {
            if (saveManager != null && playerSave != null)
            {
                await saveManager.SaveAsync(playerSave);
                Debug.Log("Player saved!");
            }
        }

        // Example: Check if data needs saving
        public void CheckDirtyState()
        {
            if (saveManager != null && saveManager.HasDirtyData)
            {
                Debug.Log($"{saveManager.DirtyCount} save objects have unsaved changes");
            }
        }
    }
}

