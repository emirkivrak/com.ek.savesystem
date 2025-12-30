namespace EK.SaveSystem
{
    /// <summary>
    /// Service locator for SaveManager. Allows SaveData to access the manager without direct coupling.
    /// The SaveManager registers itself here during initialization.
    /// </summary>
    public static class SaveServiceLocator
    {
        private static SaveManager currentManager;

        /// <summary>
        /// Gets the currently registered SaveManager.
        /// </summary>
        public static SaveManager Current => currentManager;

        /// <summary>
        /// Checks if a SaveManager is currently registered.
        /// </summary>
        public static bool HasManager => currentManager != null;

        /// <summary>
        /// Registers a SaveManager. Called automatically by SaveManager.Initialize().
        /// </summary>
        internal static void RegisterManager(SaveManager manager)
        {
            if (currentManager != null && currentManager != manager)
            {
                SaveSystemLogger.LogWarning("Replacing existing SaveManager in service locator");
            }

            currentManager = manager;
            SaveSystemLogger.Log("SaveManager registered in service locator");
        }

        /// <summary>
        /// Unregisters the current SaveManager. Called automatically by SaveManager.Dispose().
        /// </summary>
        internal static void UnregisterManager()
        {
            currentManager = null;
            SaveSystemLogger.Log("SaveManager unregistered from service locator");
        }

        /// <summary>
        /// Manually set a SaveManager. Use this if you want to control registration yourself.
        /// </summary>
        public static void SetManager(SaveManager manager)
        {
            RegisterManager(manager);
        }

        /// <summary>
        /// Clear the current manager reference.
        /// </summary>
        public static void Clear()
        {
            UnregisterManager();
        }
    }
}

