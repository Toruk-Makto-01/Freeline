using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    public class RoomDecorationManager : MonoBehaviour
    {
        // Artik Inspector'da liste atamana gerek yok, her sey otomatik!
        private Dictionary<DecorationCategory, RoomDecorationSlot> _slotLookup = new();
        private Dictionary<DecorationCategory, string> _equipped = new();

        // Slotlarin kendini kaydetmesi icin yeni metod
        public void RegisterSlot(RoomDecorationSlot slot)
        {
            if (!_slotLookup.ContainsKey(slot.Category))
            {
                _slotLookup[slot.Category] = slot;
                // Debug.Log($"{slot.Category} slotu basariyla sisteme kaydedildi.");
            }
        }

        public void EquipItem(DecorationItemData item)
        {
            if (!_slotLookup.TryGetValue(item.category, out var slot))
            {
                Debug.LogWarning($"Hata: {item.category} kategorisi icin sahnede bir Slot bulunamadi! (Slot objesinin aktif oldugundan emin ol)");
                return;
            }

            slot.ApplySprite(item.roomSprite);
            _equipped[item.category] = item.itemId;
        }

        public void OnCargoArrived(DecorationItemData item)
        {
            EquipItem(item);
        }

        public Dictionary<DecorationCategory, string> GetEquippedState() => _equipped;

        public void ApplyFromSave(Dictionary<DecorationCategory, string> savedState, DecorationCatalog catalog)
        {
            foreach (var kvp in savedState)
            {
                var item = catalog.GetById(kvp.Value);
                if (item != null) EquipItem(item);
            }
        }
    }
}