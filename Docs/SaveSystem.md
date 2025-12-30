# EK Save System - Documentation

## Overview

Production-ready save system for Unity mobile games with automatic dirty tracking, optional AES encryption, async operations, automatic backups, and versioning support. Designed for Dependency Injection and Service Locator patterns.

---

## Dependencies

- **Unity** 2022.3+
- **UniTask** 2.5.0+ - High-performance async/await for Unity
- **Newtonsoft.Json** 3.2.1+ - JSON serialization

```json
"dependencies": {
  "com.cysharp.unitask": "2.5.0",
  "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

---

## Features

- ✅ **Automatic Dirty Tracking** - Only saves data that has changed
- ✅ **Plain JSON by Default** - Easy debugging, optional AES-256 encryption
- ✅ **Auto-Save System** - Configurable intervals, on quit, on pause
- ✅ **Async & Sync Operations** - Non-blocking saves with UniTask
- ✅ **Automatic Backups** - Keeps last valid save before overwriting
- ✅ **Data Versioning** - Built-in migration system
- ✅ **DI-Friendly** - Works with VContainer, Zenject, Service Locator
- ✅ **Editor Tools** - Debug window for testing and inspection
- ✅ **Type-Safe** - Generic methods with compile-time checking

---

## Quick Start

### 1. Define Save Data

```csharp
using System;
using EK.SaveSystem;

[Serializable]
public class PlayerSave : SaveData
{
    public override string SaveKey => "player_data";

    private int level;
    private int gold;
    private bool isAlive;

    public int Level
    {
        get => level;
        set { level = value; SetDirty(); } // Automatically tracked
    }

    public int Gold
    {
        get => gold;
        set => SetField(ref gold, value); // Alternative syntax
    }

    public bool IsAlive
    {
        get => isAlive;
        set { isAlive = value; SetDirty(); }
    }
}
```

### 2. Initialize SaveManager

```csharp
using EK.SaveSystem;

// Manual initialization
var saveManager = new SaveManager();
saveManager.Initialize(
    saveService: new LocalSaveService(enableEncryption: false),
    autoSaveEnabled: true,
    autoSaveIntervalSeconds: 300f
);

// Or use MonoBehaviour wrapper
// Add SaveManagerBehaviour component to GameObject
```

### 3. Load and Use

```csharp
using Cysharp.Threading.Tasks;

public class GameController
{
    private SaveManager saveManager;
    private PlayerSave playerSave;

    public async UniTask InitializeAsync()
    {
        saveManager = SaveServiceLocator.Current;
        
        playerSave = await saveManager.LoadAsync<PlayerSave>("player_data");
        if (playerSave == null)
        {
            playerSave = new PlayerSave { Level = 1, Gold = 0, IsAlive = true };
            saveManager.Register(playerSave);
            await saveManager.SaveAsync(playerSave);
        }
    }

    public void OnLevelUp()
    {
        playerSave.Level++; // Automatically marked dirty, auto-saves later
    }

    public async UniTask SaveNow()
    {
        await saveManager.SaveAllDirtyAsync();
    }
}
```

### 4. Update Loop (for auto-save)

```csharp
void Update()
{
    saveManager?.Update(Time.deltaTime);
}

void OnApplicationQuit()
{
    saveManager?.OnApplicationQuit();
    saveManager?.Dispose();
}
```

---

## Dependency Injection Integration

### VContainer Example

```csharp
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISaveService>(resolver => 
            new LocalSaveService(enableEncryption: false), 
            Lifetime.Singleton
        );

        builder.Register<SaveManager>(resolver =>
        {
            var saveManager = new SaveManager();
            saveManager.Initialize(resolver.Resolve<ISaveService>());
            return saveManager;
        }, Lifetime.Singleton);
    }
}
```

---

## Configuration

```csharp
// Plain JSON (default - recommended for development)
var service = new LocalSaveService(enableEncryption: false);

// With encryption (production)
var service = new LocalSaveService(enableEncryption: true);

// Custom encryption key
var service = new LocalSaveService(
    enableEncryption: true, 
    customEncryptionKey: "YourSecretKey"
);

// Initialize with options
saveManager.Initialize(
    saveService: service,
    autoSaveEnabled: true,
    autoSaveIntervalSeconds: 300f,        // 5 minutes
    saveOnApplicationQuit: true,
    saveOnApplicationPause: true          // Mobile
);
```

---

## API Reference

### SaveManager

| Method | Description |
|--------|-------------|
| `Initialize(...)` | Initialize with save service and configuration |
| `LoadAsync<T>(key)` | Load save data, returns null if not found |
| `SaveAsync(data)` | Save specific data immediately |
| `SaveAllDirtyAsync()` | Save all modified data |
| `SaveAllDirtySync()` | Blocking save (use on quit) |
| `Register(data)` | Register save data for tracking |
| `Update(deltaTime)` | Update auto-save timer |
| `OnApplicationQuit()` | Handle quit event |
| `Dispose()` | Clean up resources |

### SaveData (Base Class)

| Member | Description |
|--------|-------------|
| `SaveKey` | Unique identifier (must override) |
| `IsDirty` | Check if has unsaved changes |
| `SetDirty()` | Mark as modified (call in setters) |
| `SetField<T>(ref field, value)` | Helper to set field and mark dirty |
| `version` | Version number for migrations |

---

## Architecture Overview

```
SaveManager (Orchestrator)
    ↓
ISaveService (Interface)
    ↓
LocalSaveService (Implementation)
    ↓
    ├─→ EncryptionHelper (AES-256)
    ├─→ SaveSystemLogger (Logging)
    └─→ SaveData (Base Class with Dirty Tracking)
```

**Design Principles:**
- **Single Responsibility** - Each component has one clear purpose
- **Open/Closed** - Easy to extend (e.g., CloudSaveService)
- **Dependency Inversion** - Interface-based for DI compatibility
- **Dirty Tracking** - Only saves changed data for performance

---

## Core Components

### SaveManager
Central orchestrator for dirty tracking and auto-save. Registers with `SaveServiceLocator` for global access.

### ISaveService
Interface for save operations. Supports both async (UniTask) and sync methods.

### LocalSaveService
File-based implementation with:
- JSON serialization (Newtonsoft.Json)
- Optional AES-256 encryption
- Automatic `.backup` file creation
- Background thread I/O (non-blocking)
- Save location: `Application.persistentDataPath`

### SaveData (Base Class)
Abstract base for all save data with:
- `SaveKey` - Unique identifier (must override)
- `IsDirty` - Dirty state tracking
- `SetDirty()` - Mark as modified
- `SetField<T>()` - Helper for property setters
- `version` - For data migration
- `OnAfterLoad()` / `OnBeforeSave()` - Lifecycle hooks

---

## Encryption (Optional)

### Algorithm: AES-256-CBC with PBKDF2

| Feature | Value |
|---------|-------|
| Algorithm | AES-256 |
| Mode | CBC |
| Key Derivation | PBKDF2 (10,000 iterations) |
| Salt | 16 bytes (random per save) |
| IV | Random per encryption |
| Output | Base64(salt + encrypted_data) |

### Configuration

```csharp
// Plain JSON (default - recommended for development)
new LocalSaveService(enableEncryption: false);

// With encryption (production)
new LocalSaveService(enableEncryption: true);

// Custom key (for save portability)
new LocalSaveService(
    enableEncryption: true,
    customEncryptionKey: "YourSecretKey"
);
```

**Default Key:** `SystemInfo.deviceUniqueIdentifier` (device-specific)

**Security Notes:**
- Prevents casual tampering, not determined attacks
- For critical data, use server-side validation
- Custom key enables cross-device save transfer

---

## Data Versioning & Migration

### Basic Migration

```csharp
[Serializable]
public class PlayerSave : SaveData
{
    public override string SaveKey => "player_data";
    
    public string playerName;
    public int level;
    public int experience; // Added in v2
    
    public override void OnAfterLoad()
    {
        base.OnAfterLoad();
        
        if (version < 2)
        {
            experience = level * 100; // Migrate from v1
            version = 2;
        }
    }
}
```

### Migration Strategies

| Strategy | Use Case | Example |
|----------|----------|---------|
| **Incremental** | Multiple versions | `if (version < 2) { ... }` |
| **Default Values** | New fields | `public int newField = 100;` |
| **Nullable Types** | Optional fields | `public int? optionalField;` |

### Best Practices

- Increment `version` for breaking changes
- Test all migration paths
- Keep migration logic in `OnAfterLoad()`
- Document version changes

---

## Error Handling

### Exception Types

| Exception | Cause | Handling |
|-----------|-------|----------|
| `ArgumentException` | Invalid key/null data | Validate inputs |
| `IOException` | Disk full, permissions | Retry, show error |
| `CryptographicException` | Encryption failed | Check key, try backup |
| `JsonException` | Serialization failed | Validate data structure |

### Automatic Backup Recovery

LocalSaveService automatically attempts backup if main save fails:

```csharp
// Automatic fallback
1. Try main save file
2. If fails → Try backup file
3. If both fail → Return null
```

### Retry Pattern

```csharp
async UniTask<bool> SaveWithRetry(PlayerSave data, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await saveManager.SaveAsync(data);
            return true;
        }
        catch (Exception ex)
        {
            SaveSystemLogger.LogWarning($"Retry {i + 1}/{maxRetries}: {ex.Message}");
            if (i < maxRetries - 1) await UniTask.Delay(1000);
        }
    }
    return false;
}
```

---

## Performance

### Benchmarks (Mid-range Mobile)

| Operation | Size | Time | Notes |
|-----------|------|------|-------|
| Save (Plain) | 1KB | ~3-5ms | Background thread |
| Save (Encrypted) | 1KB | ~5-10ms | +AES overhead |
| Load (Plain) | 1KB | ~3-5ms | Background thread |
| Load (Encrypted) | 1KB | ~5-10ms | +AES overhead |
| Save (Plain) | 10KB | ~10-15ms | Background thread |
| Save (Encrypted) | 10KB | ~15-25ms | +AES overhead |

### Optimization Tips

**1. Dirty Tracking** - Only saves changed data
```csharp
playerSave.Level++; // Marked dirty, saves only this object
```

**2. Batch Related Data**
```csharp
// Combine related data into one save
public class GameSave : SaveData
{
    public PlayerData player;
    public SettingsData settings;
}
```

**3. Strategic Save Timing**
- On level complete
- On checkpoint
- On application pause/quit
- Avoid saving every frame

**4. Use Simple Data Structures**
- Prefer arrays over dictionaries
- Avoid deep nesting
- Keep save data flat when possible

---

## Best Practices

### 1. Always Call SetDirty()
```csharp
public int Level
{
    get => level;
    set { level = value; SetDirty(); } // Required!
}
```

### 2. Initialize SaveManager Early
```csharp
void Awake()
{
    var saveManager = new SaveManager();
    saveManager.Initialize();
}
```

### 3. Save Strategically
- ✅ On level complete, checkpoint, settings change
- ✅ On application pause/quit
- ❌ Every frame, during gameplay, multiple times/second

### 4. Separate Concerns
```csharp
// Good: Different save types
public class PlayerSave : SaveData { }
public class SettingsSave : SaveData { }
public class ProgressSave : SaveData { }
```

### 5. Handle Null Returns
```csharp
var data = await saveManager.LoadAsync<PlayerSave>("player");
if (data == null)
{
    data = new PlayerSave(); // Create default
}
```

### 6. Use DI for Testability
```csharp
public class GameController
{
    private readonly SaveManager saveManager;
    
    public GameController(SaveManager saveManager)
    {
        this.saveManager = saveManager; // Injected
    }
}
```

---

## Editor Tools

Access via **Tools > EK > Save System Debug**

Features:
- View all save files with metadata
- Preview decrypted JSON contents
- Test save/load operations
- Delete individual or all saves
- Open save directory in explorer

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Save not loading | Check key, verify file exists with `HasSave()` |
| Encryption errors | Verify encryption key hasn't changed |
| Performance issues | Reduce save frequency, optimize data size |
| Dirty tracking not working | Ensure `SetDirty()` called in setters |
| SaveManager null | Call `Initialize()` before use |

---

## Advanced Scenarios

### Cloud Save Integration
```csharp
public class HybridSaveService : ISaveService
{
    private readonly LocalSaveService local;
    private readonly CloudSaveService cloud;
    
    public async UniTask SaveAsync<T>(string key, T data)
    {
        await local.SaveAsync(key, data); // Always save locally first
        try { await cloud.SaveAsync(key, data); } // Try cloud
        catch { /* Log but don't fail */ }
    }
}
```

### Multiple Save Slots
```csharp
public class SaveSlotManager
{
    private int currentSlot = 0;
    
    private string GetKey(string baseKey) => $"slot_{currentSlot}_{baseKey}";
    
    public async UniTask<PlayerSave> LoadSlot(int slot)
    {
        currentSlot = slot;
        return await saveManager.LoadAsync<PlayerSave>(GetKey("player"));
    }
}
```

### Data Validation
```csharp
public class PlayerSave : SaveData
{
    public int level;
    public int gold;
    
    public override void OnAfterLoad()
    {
        base.OnAfterLoad();
        level = Mathf.Clamp(level, 1, 100); // Validate range
        gold = Mathf.Max(0, gold); // Prevent negative
    }
}
```

---

## Summary

**EK Save System** provides production-ready data persistence for Unity mobile games with:
- Automatic dirty tracking for performance
- Optional AES-256 encryption
- Async operations via UniTask
- DI/Service Locator compatibility
- Automatic backups and versioning

**Save Location:** `Application.persistentDataPath/{key}.sav`  
**Format:** Plain JSON (default) or AES-256 encrypted  
**Thread Safety:** All I/O on background threads

For complete examples, see `Runtime/Examples/` folder.

