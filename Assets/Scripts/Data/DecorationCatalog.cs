using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "DecorationCatalog", menuName = "Freeline/Decoration Catalog")]
    public class DecorationCatalog : ScriptableObject
    {
        public List<DecorationItemData> allItems;

        public DecorationItemData GetById(string id) =>
            allItems.Find(i => i.itemId == id);

        public List<DecorationItemData> GetByCategory(DecorationCategory category) =>
            allItems.FindAll(i => i.category == category);
    }
}