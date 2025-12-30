using System;

namespace EK.SaveSystem
{
    /// <summary>
    /// Base class for all save data.
    /// Provides versioning support and automatic dirty tracking.
    /// </summary>
    [Serializable]
    public abstract class SaveData
    {
        /// <summary>
        /// Version number for this save data.
        /// Increment this when making breaking changes to the data structure.
        /// </summary>
        public int version = 1;

        /// <summary>
        /// Timestamp of when this save was created (Unix time).
        /// Automatically set by the save system.
        /// </summary>
        public long timestamp;

        /// <summary>
        /// Unique key for this save data. Must be overridden.
        /// Example: "player_data", "game_settings", etc.
        /// </summary>
        public abstract string SaveKey { get; }

        /// <summary>
        /// Indicates if this data has been modified since last save.
        /// Not serialized - runtime only.
        /// </summary>
        [NonSerialized]
        private bool isDirty = false;

        /// <summary>
        /// Gets whether this data has unsaved changes.
        /// </summary>
        public bool IsDirty => isDirty;

        /// <summary>
        /// Marks this data as dirty (modified). Call this in property setters.
        /// Example: set { field = value; SetDirty(); }
        /// </summary>
        protected void SetDirty()
        {
            if (!isDirty)
            {
                isDirty = true;
                
                // Register with SaveManager if available
                if (SaveServiceLocator.HasManager)
                {
                    SaveServiceLocator.Current.RegisterDirty(this);
                }
                
                OnMarkedDirty();
            }
        }

        /// <summary>
        /// Clears the dirty flag after successful save. Called by SaveManager.
        /// </summary>
        internal void ClearDirty()
        {
            isDirty = false;
        }

        /// <summary>
        /// Forces this data to be marked as dirty. Useful for initial saves.
        /// </summary>
        public void ForceDirty()
        {
            SetDirty();
        }

        /// <summary>
        /// Helper to set a field and automatically mark dirty if value changed.
        /// Usage: set => SetField(ref _field, value);
        /// </summary>
        protected bool SetField<T>(ref T field, T value)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                return false; // No change
            }

            field = value;
            SetDirty();
            return true;
        }

        /// <summary>
        /// Called when data is marked dirty. Override for custom behavior.
        /// </summary>
        protected virtual void OnMarkedDirty()
        {
            // Override in derived classes if needed
        }

        /// <summary>
        /// Called after the data is loaded.
        /// Override this to perform data migration based on version.
        /// </summary>
        public virtual void OnAfterLoad()
        {
            // Override in derived classes for migration logic
        }

        /// <summary>
        /// Called before the data is saved.
        /// Override this to perform any pre-save operations.
        /// </summary>
        public virtual void OnBeforeSave()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}

