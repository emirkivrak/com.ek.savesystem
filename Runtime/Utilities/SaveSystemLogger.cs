using UnityEngine;

namespace EK.SaveSystem
{
    /// <summary>
    /// Logger utility for the Save System.
    /// Provides conditional logging based on Unity's debug mode.
    /// </summary>
    public static class SaveSystemLogger
    {
        private const string LogPrefix = "[SaveSystem]";
        private static bool enableLogging = true;

        /// <summary>
        /// Enable or disable logging for the save system.
        /// </summary>
        public static bool EnableLogging
        {
            get => enableLogging;
            set => enableLogging = value;
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"{LogPrefix} {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"{LogPrefix} {message}");
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError($"{LogPrefix} {message}");
            }
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        public static void LogException(System.Exception exception)
        {
            if (enableLogging)
            {
                Debug.LogError($"{LogPrefix} Exception: {exception.Message}\n{exception.StackTrace}");
            }
        }
    }
}

