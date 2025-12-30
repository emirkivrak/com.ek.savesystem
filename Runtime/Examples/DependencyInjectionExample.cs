using Cysharp.Threading.Tasks;

namespace EK.SaveSystem.Examples
{
    /// <summary>
    /// Example showing how to integrate SaveManager with Dependency Injection (e.g., VContainer, Zenject).
    /// This is a pseudo-code example showing the pattern - adapt to your specific DI framework.
    /// </summary>
    public class DependencyInjectionExample
    {
        // Example 1: VContainer Integration
        /*
        public class GameLifetimeScope : LifetimeScope
        {
            protected override void Configure(IContainerBuilder builder)
            {
                // Register ISaveService
                builder.Register<ISaveService>(resolver => 
                    new LocalSaveService(enableEncryption: false), 
                    Lifetime.Singleton
                );

                // Register SaveManager
                builder.Register<SaveManager>(resolver =>
                {
                    var saveManager = new SaveManager();
                    var saveService = resolver.Resolve<ISaveService>();
                    
                    saveManager.Initialize(
                        saveService: saveService,
                        autoSaveEnabled: true,
                        autoSaveIntervalSeconds: 300f,
                        saveOnApplicationQuit: true,
                        saveOnApplicationPause: true
                    );
                    
                    return saveManager;
                }, Lifetime.Singleton);

                // Register PlayerSave (loaded/created on demand)
                builder.Register<PlayerSave>(async resolver =>
                {
                    var saveManager = resolver.Resolve<SaveManager>();
                    var playerSave = await saveManager.LoadAsync<PlayerSave>("player_data");
                    
                    if (playerSave == null)
                    {
                        playerSave = new PlayerSave
                        {
                            PlayerName = "NewPlayer",
                            Level = 1,
                            IsAlive = true
                        };
                        saveManager.Register(playerSave);
                        await saveManager.SaveAsync(playerSave);
                    }
                    
                    return playerSave;
                }, Lifetime.Singleton);
            }
        }
        */

        // Example 2: Manual Service Locator Pattern
        public class GameBootstrapper
        {
            private SaveManager saveManager;

            public void Initialize()
            {
                // Create and configure SaveManager
                saveManager = new SaveManager();
                
                var saveService = new LocalSaveService(enableEncryption: false);
                
                saveManager.Initialize(
                    saveService: saveService,
                    autoSaveEnabled: true,
                    autoSaveIntervalSeconds: 300f
                );

                // SaveManager is now accessible via SaveServiceLocator.Current
                // or you can inject it directly into classes
            }

            public void Update(float deltaTime)
            {
                // Call from your game loop
                saveManager?.Update(deltaTime);
            }

            public void OnApplicationQuit()
            {
                saveManager?.OnApplicationQuit();
                saveManager?.Dispose();
            }
        }

        // Example 3: Injected into Game Classes
        public class PlayerController
        {
            private readonly SaveManager saveManager;
            private PlayerSave playerSave;

            // Constructor injection
            public PlayerController(SaveManager saveManager)
            {
                this.saveManager = saveManager;
            }

            public async UniTask InitializeAsync()
            {
                // Load player save
                playerSave = await saveManager.LoadAsync<PlayerSave>("player_data");
                
                if (playerSave == null)
                {
                    playerSave = new PlayerSave
                    {
                        PlayerName = "Hero",
                        Level = 1,
                        IsAlive = true
                    };
                    saveManager.Register(playerSave);
                    await saveManager.SaveAsync(playerSave);
                }
            }

            public void LevelUp()
            {
                playerSave.Level++; // Automatically marked dirty!
                // SaveManager will auto-save
            }

            public async UniTask SaveNow()
            {
                await saveManager.SaveAsync(playerSave);
            }
        }

        // Example 4: Direct ISaveService Usage (Without SaveManager)
        public class SimplePlayerManager
        {
            private readonly ISaveService saveService;
            private PlayerSave playerSave;

            public SimplePlayerManager(ISaveService saveService)
            {
                this.saveService = saveService;
            }

            public async UniTask LoadAsync()
            {
                playerSave = await saveService.LoadAsync<PlayerSave>("player_data");
                // Note: Dirty tracking won't work without SaveManager
                // You'll need to call SaveAsync manually
            }

            public async UniTask SaveAsync()
            {
                await saveService.SaveAsync("player_data", playerSave);
            }
        }
    }
}

