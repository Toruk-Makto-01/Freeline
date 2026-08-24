using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Freeline
{
    [CreateAssetMenu(fileName = "ConsumableCatalog", menuName = "Freeline/Market/Consumable Catalog")]
    public class ConsumableCatalog : ScriptableObject
    {
        [Tooltip("Oluşturduğunuz tüm yemek ve enerji Scriptable Object'lerini buraya atın")]
        public List<ConsumableItemData> allConsumables = new();

        // Sadece Yemek (Açlık giderici) olanalrı filtrele
        public List<ConsumableItemData> GetFoodItems()
        {
            return allConsumables.Where(i => i.effectType == ConsumableEffectType.HungerRelief).ToList();
        }

        // Yemek haricinmdeki (Enerji veren/azaltan) her şeyi filterele
        public List<ConsumableItemData> GetEnergyItems()
        {
            return allConsumables.Where(i => i.effectType != ConsumableEffectType.HungerRelief).ToList();
        }
    }
}