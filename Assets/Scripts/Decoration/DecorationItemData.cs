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
        public int deliveryDays = 1;   
        public PassiveBonusType bonusType;
        public float bonusValue;
    }

    public enum PassiveBonusType
    {
        None,
        SleepEnergyBonus,      
        WebtoonQualityBonus,   
        EnergyDrainReduction,  
        JobBuff,               
        MoraleBonus             
    }
}