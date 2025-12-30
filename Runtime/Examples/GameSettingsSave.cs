using System;

namespace EK.SaveSystem.Examples
{
    /// <summary>
    /// Example game settings save data with automatic dirty tracking.
    /// Shows how to use SetField helper for cleaner property syntax.
    /// </summary>
    [Serializable]
    public class GameSettingsSave : SaveData
    {
        public override string SaveKey => "game_settings";

        // Private backing fields
        private float musicVolume;
        private float sfxVolume;
        private bool notificationsEnabled;
        private bool vibrationEnabled;
        private string language;
        private int graphicsQuality;

        // Properties using SetField helper (automatically marks dirty if value changes)
        public float MusicVolume
        {
            get => musicVolume;
            set => SetField(ref musicVolume, value);
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set => SetField(ref sfxVolume, value);
        }

        public bool NotificationsEnabled
        {
            get => notificationsEnabled;
            set => SetField(ref notificationsEnabled, value);
        }

        public bool VibrationEnabled
        {
            get => vibrationEnabled;
            set => SetField(ref vibrationEnabled, value);
        }

        public string Language
        {
            get => language;
            set => SetField(ref language, value);
        }

        public int GraphicsQuality
        {
            get => graphicsQuality;
            set => SetField(ref graphicsQuality, value);
        }

        // Constructor with default values
        public GameSettingsSave()
        {
            musicVolume = 0.8f;
            sfxVolume = 1.0f;
            notificationsEnabled = true;
            vibrationEnabled = true;
            language = "en";
            graphicsQuality = 1; // Medium
        }

        // Helper methods
        public void SetAllVolumes(float volume)
        {
            musicVolume = volume;
            sfxVolume = volume;
            SetDirty(); // Mark dirty after batch changes
        }

        public void ResetToDefaults()
        {
            musicVolume = 0.8f;
            sfxVolume = 1.0f;
            notificationsEnabled = true;
            vibrationEnabled = true;
            graphicsQuality = 1;
            SetDirty();
        }
    }
}

