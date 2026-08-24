using UnityEngine;

namespace Freeline
{
    public enum CurrencyType { Coin, Gem }
    public enum ConsumableEffectType
    {
        InstantEnergy,        // Anında enerji doldurur (Örn: Enerji İçeceği)
        HungerRelief,         // Anında açlık giderir (Örn: Hamburger)
        EnergyCostReduction,  // Belirli süre boyunca enerji harcamasını düşürür (Örn: Kahve)
        EnergyRegenOverTime   // Belirli süre boyunca yavaş yavaş enerji doldurur
    }

    [CreateAssetMenu(fileName = "New Consumable", menuName = "Freeline/Market/Consumable Item")]
    public class ConsumableItemData : ScriptableObject
    {
        [Header("Temel Bilgiler")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite shopIcon;

        [Header("Ekonomi ve Limitler")]
        public CurrencyType currencyType; // Coin mi, Gem mi
        public int price;
        public int dailyLimit = 3; // Oyun içi günce en fazla kaç tane alınabilir

        [Header("Etki (Buff) Ayarları")]
        public ConsumableEffectType effectType;
        public float effectValue; // Örn: 30 (Enerji verir) veya 2 (Enerji maliyetini düşürür)

        [Tooltip("Anlık etkiler için 0 bırakın. Süreli bufflar için dakika cinsinden yazın (Örn: 60)")]
        public int durationInMinutes;
    }
}

