using System;

namespace EK.SaveSystem.Examples
{
    /// <summary>
    /// Example player save data with automatic dirty tracking.
    /// Shows how to use properties with SetDirty() for automatic save tracking.
    /// </summary>
    [Serializable]
    public class PlayerSave : SaveData
    {
        // Unique key for this save type
        public override string SaveKey => "player_data";

        // Private backing fields
        private string playerName;
        private bool isAlive;
        private int level;
        private int gold;
        private int experience;
        private float health;
        private float[] position;

        // Properties with automatic dirty tracking
        // When any property is set, it automatically marks this save as dirty

        public string PlayerName
        {
            get => playerName;
            set
            {
                playerName = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public bool IsAlive
        {
            get => isAlive;
            set
            {
                isAlive = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public int Level
        {
            get => level;
            set
            {
                level = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public int Gold
        {
            get => gold;
            set
            {
                gold = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public int Experience
        {
            get => experience;
            set
            {
                experience = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public float Health
        {
            get => health;
            set
            {
                health = value;
                SetDirty(); // Marks save as dirty
            }
        }

        public float[] Position
        {
            get => position;
            set
            {
                position = value;
                SetDirty(); // Marks save as dirty
            }
        }

        // Alternative: Use SetField helper (cleaner syntax)
        // public int Gold
        // {
        //     get => gold;
        //     set => SetField(ref gold, value);
        // }

        // Methods that modify data should also mark as dirty
        public void AddGold(int amount)
        {
            gold += amount;
            SetDirty();
        }

        public void AddExperience(int amount)
        {
            experience += amount;
            
            // Level up logic
            if (experience >= level * 100)
            {
                level++;
                experience = 0;
            }
            
            SetDirty(); // Mark dirty after modifications
        }

        public void TakeDamage(float damage)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
                isAlive = false;
            }
            SetDirty();
        }

        public void Heal(float amount)
        {
            health += amount;
            if (health > 100f)
            {
                health = 100f;
            }
            SetDirty();
        }

        public void MoveTo(float x, float y, float z)
        {
            position = new float[] { x, y, z };
            SetDirty();
        }

        // Optional: Override OnBeforeSave for custom pre-save logic
        public override void OnBeforeSave()
        {
            base.OnBeforeSave();
            // Custom logic before saving
            UnityEngine.Debug.Log($"Saving player: {playerName}, Level {level}");
        }

        // Optional: Override OnAfterLoad for data migration
        public override void OnAfterLoad()
        {
            base.OnAfterLoad();
            
            // Example migration from version 1 to 2
            if (version < 2)
            {
                // Add default position if upgrading from v1
                if (position == null || position.Length == 0)
                {
                    position = new float[] { 0f, 0f, 0f };
                }
                version = 2;
            }
        }
    }
}

