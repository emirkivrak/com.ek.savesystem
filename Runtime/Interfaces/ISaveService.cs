using Cysharp.Threading.Tasks;

namespace EK.SaveSystem
{
    /// <summary>
    /// Interface for save service operations.
    /// Provides methods for saving, loading, and managing save data with optional encryption.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Saves data asynchronously with optional encryption and automatic backup.
        /// Recommended for runtime saves to avoid blocking the main thread.
        /// </summary>
        /// <typeparam name="T">The type of data to save (must be serializable)</typeparam>
        /// <param name="key">Unique identifier for the save data</param>
        /// <param name="data">The data to save</param>
        /// <returns>A UniTask representing the async operation</returns>
        UniTask SaveAsync<T>(string key, T data);

        /// <summary>
        /// Loads data asynchronously with optional decryption.
        /// Recommended for runtime loads to avoid blocking the main thread.
        /// </summary>
        /// <typeparam name="T">The type of data to load</typeparam>
        /// <param name="key">Unique identifier for the save data</param>
        /// <returns>A UniTask containing the loaded data, or default(T) if not found</returns>
        UniTask<T> LoadAsync<T>(string key);

        /// <summary>
        /// Saves data synchronously. Blocks until complete.
        /// Use for critical moments like application quit or when async is not available.
        /// </summary>
        /// <typeparam name="T">The type of data to save (must be serializable)</typeparam>
        /// <param name="key">Unique identifier for the save data</param>
        /// <param name="data">The data to save</param>
        void Save<T>(string key, T data);

        /// <summary>
        /// Loads data synchronously. Blocks until complete.
        /// Use when async is not available or for editor scripts.
        /// </summary>
        /// <typeparam name="T">The type of data to load</typeparam>
        /// <param name="key">Unique identifier for the save data</param>
        /// <returns>The loaded data, or default(T) if not found</returns>
        T Load<T>(string key);

        /// <summary>
        /// Deletes a save file and its backup.
        /// </summary>
        /// <param name="key">Unique identifier for the save data to delete</param>
        void DeleteSave(string key);

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        /// <param name="key">Unique identifier for the save data</param>
        /// <returns>True if the save exists, false otherwise</returns>
        bool HasSave(string key);

        /// <summary>
        /// Deletes all save files and backups.
        /// </summary>
        void ClearAllSaves();

        /// <summary>
        /// Gets all save keys currently stored.
        /// </summary>
        /// <returns>Array of save keys</returns>
        string[] GetAllSaveKeys();
    }
}

