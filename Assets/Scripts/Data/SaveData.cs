using System;
using System.Collections.Generic;

namespace Freeline
{
    [Serializable]
    public class EquippedDecoration
    {
        public DecorationCategory category;
        public string itemId;
    }
    [Serializable]
    public class SaveData
    {
        // Increment this when the schema changes so old saves can be migrated.
        public int saveVersion = 1;

        // --- Time ---
        public int currentDay = 1;
        public float currentHour = 9f;

        // --- Energy / Hunger ---
        public float currentEnergy = 100f;
        public float hoursSinceLastFood = 0f;

        // --- Economy ---
        public float currentCoins = 0f;
        public int currentGems = 0;

        // --- Career ---
        public WebtoonData webtoonData = new WebtoonData();
        public int totalJobsCompleted = 0;
        public int playerLevel = 1;

        // --- Exhibition ---
        public List<ExhibitionStockItem> exhibitionStock = new();

        // Decoration
        public List<string> ownedDecorations = new();
        public List<EquippedDecoration> equippedDecorations = new();
    }
}
