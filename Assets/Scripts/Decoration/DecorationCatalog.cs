using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Verileri hızlıca filtrelemek için ekledik

namespace Freeline
{
    [CreateAssetMenu(fileName = "DecorationCatalog", menuName = "Freeline/Decoration Catalog")]
    public class DecorationCatalog : ScriptableObject
    {
        [Tooltip("Oyundaki tüm dekorasyon ürünlerini buraya sürükleyin")]
        public List<DecorationItemData> allItems = new List<DecorationItemData>();

        // ID'ye göre ürünü bulur (Oda yüklenirken RoomDecorationManager kullanır)
        public DecorationItemData GetItemById(string id)
        {
            return allItems.FirstOrDefault(i => i.itemId == id);
        }

        // Kategoriye göre ürünleri liste halinde getirir (MarketPanel kullanır)
        public List<DecorationItemData> GetByCategory(DecorationCategory category)
        {
            return allItems.Where(i => i.category == category).ToList();
        }
    }
}