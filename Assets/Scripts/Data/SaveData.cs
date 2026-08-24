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
    [System.Serializable]
    public class DailyPurchaseRecord
    {
        public string itemId;
        public int boughtCount;
    }

    [System.Serializable]
    public class ActiveBuffSaveData
    {
        public ConsumableEffectType effectType;
        public float effectValue;
        public string endTimeString; // DateTime'ı string olarak kaydedeceğiz (JSON serileştirmesi için)
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
        public List<ActiveBuffSaveData> activeRealTimeBuffs = new();

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

        // --- Market Günlük Limit Sistemi ---
        public string lastRealTimeDate = ""; // Örn: "25-10-2023" (Oyuna en son girilen gerçek tarih)
        public List<DailyPurchaseRecord> dailyPurchases = new(); // O gün alınan ürünlerin listesi
        // Marketin en son sıfırlandığı oyun içi gün (Bunu yeni ekliyoruz)
        public int lastMarketResetDay = 0;

        // --- Phone Settings ---
        public int selectedPhoneBgIndex = 0; // Seçilen arkaplanın sırası
    }

}
