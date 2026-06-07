using System;
using System.Collections.Generic;

namespace Freeline
{
    [Serializable]
    public class SaveData
    {
        // Increment this when the schema changes so old saves can be migrated.
        public int saveVersion = 1;

        // --- Time ---
        public int   currentDay  = 1;
        public float currentHour = 9f;

        // --- Energy / Hunger ---
        public float currentEnergy      = 100f;
        public float hoursSinceLastFood = 0f;

        // --- Economy ---
        public float currentCoins = 0f;
        public int   currentGems  = 0;

        // --- Career ---
        public WebtoonData webtoonData      = new WebtoonData();
        public int         totalJobsCompleted = 0;
        public int         playerLevel        = 1;

        // --- Exhibition ---
        public List<ExhibitionStock> exhibitionStock = new List<ExhibitionStock>();

        // --- Expansion slots (populate as systems are built) ---
        // public List<string>            unlockedDecorationIds = new();
        // public List<string>            unlockedSkillIds      = new();
        // public List<InventoryItemData> inventory             = new();
    }
}
