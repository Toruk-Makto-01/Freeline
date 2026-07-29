using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "DecorationItem", menuName = "Freeline/Decoration Item")]
    public class DecorationItemData : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public DecorationCategory category;
        public Sprite roomSprite;      // sahnede görünecek katman
        public Sprite shopIcon;        // mağaza listesinde görünecek ikon
        public int price;
        public int deliveryDays = 1;   // GDD: min 1 gün kargo
        public PassiveBonusType bonusType;
        public float bonusValue;
    }

    public enum PassiveBonusType
    {
        None,
        SleepEnergyBonus,      // kaliteli yatak
        WebtoonQualityBonus,   // kitaplık
        EnergyDrainReduction,  // ergonomik sandalye
        JobBuff,               // sarı lamba
        MoraleBonus             // saksı/halı
    }
}