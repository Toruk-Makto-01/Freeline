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

    [System.Serializable]
    public class DailyTransaction
    {
        public string itemName;      // Eşyanın veya işin adı (Örn: "Hamburger")
        public int amount;           // Kaç adet olduğu (Örn: 3)
        public float totalPrice;     // Toplam tutar
        public bool isIncome;        // Gelir mi, gider mi? (True = Gelir, False = Gider)
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
        // Artık saat tutmuyoruz, %0-%100 arası açlık puanı tutuyoruz. (Oyuna başlarken tok başlasın diye 100 verdik)
        public float currentHunger = 100f;
        public List<ActiveBuffSaveData> activeRealTimeBuffs = new();

        // --- YENİ ZAMAN VE KİRA SİSTEMİ ---
        public float hoursAwake = 0f; // Karakterin kaç saattir uyanık olduğu
        public int currentRentDay = 1; // 1'den 30'a kadar sayacak (Kira döngüsü)
        public bool isGracePeriodActive = false; // 10 günlük ek süre (iflas öncesi) devrede mi?
        public int rentGraceDaysLeft = 10; // Kalan ek süre gün sayısı

        // --- Economy ---
        public float currentCoins = 0f;
        public int currentGems = 0;

        // --- GÜNLÜK EKONOMİ TAKİBİ ---
        public float dailyIncome = 0f;
        public float dailyExpense = 0f;
        public List<DailyTransaction> dailyTransactions = new List<DailyTransaction>();

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
