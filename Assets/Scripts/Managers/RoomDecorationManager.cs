using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    public class RoomDecorationManager : MonoBehaviour
    {
        [SerializeField] private List<RoomDecorationSlot> slots;
        private Dictionary<DecorationCategory, RoomDecorationSlot> _slotLookup;

        private Dictionary<DecorationCategory, string> _equipped = new();

        private void Awake()
        {
            _slotLookup = new Dictionary<DecorationCategory, RoomDecorationSlot>();
            foreach (var slot in slots)
                _slotLookup[slot.Category] = slot;
        }

        public void EquipItem(DecorationItemData item)
        {
            if (!_slotLookup.TryGetValue(item.category, out var slot))
            {
                Debug.LogWarning($"No slot found for category {item.category}");
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