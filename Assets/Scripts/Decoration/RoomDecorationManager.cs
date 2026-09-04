using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    [System.Serializable]
    public class DecorationSlot
    {
        public DecorationCategory category;
        public Image slotImage;
    }

    public class RoomDecorationManager : MonoBehaviour
    {
        [Header("Oda Katmanları (Slotlar)")]
        [SerializeField] private List<DecorationSlot> roomSlots = new();

        [Header("Başlangıç Odası (Default)")]
        [Tooltip("Kayıt sıfırlandığında (veya yeni oyunda) otomatik eklenecek temel eşyalar")]
        [SerializeField] private List<DecorationItemData> defaultStarterItems = new();

        [Header("Katalog")]
        [SerializeField] private DecorationCatalog catalog;

        private void Start()
        {
            InitializeRoomFromSave();
        }

        public void InitializeRoomFromSave()
        {
            var data = GameManager.Instance.SaveManager.CurrentData;

            // 1. EĞER KAYIT DOSYASI BOŞSA BAŞLANGIÇ EŞYALARINI DİZ
            if (data.equippedDecorations == null || data.equippedDecorations.Count == 0)
            {
                SetupDefaultRoom(data);
            }

            // 2. Önce tüm slotları temizle (Gizle)
            foreach (var slot in roomSlots)
            {
                if (slot.slotImage != null)
                {
                    slot.slotImage.sprite = null;
                    slot.slotImage.enabled = false;
                }
            }

            // 3. Kayıt dosyasındaki takılı eşyaları bul ve göster
            foreach (var equipped in data.equippedDecorations)
            {
                var itemData = catalog.GetItemById(equipped.itemId);
                if (itemData != null)
                {
                    EquipItemVisual(itemData);
                }
            }
        }

        private void SetupDefaultRoom(SaveData data)
        {
            foreach (var item in defaultStarterItems)
            {
                // Başlangıç eşyasını envantere ekle
                if (data.ownedDecorations != null && !data.ownedDecorations.Contains(item.itemId))
                {
                    data.ownedDecorations.Add(item.itemId);
                }

                // Başlangıç eşyasını odaya tak 
                data.equippedDecorations.Add(new EquippedDecoration { category = item.category, itemId = item.itemId });
            }
            
            GameManager.Instance.SaveManager.SaveGame();
        }

        public void EquipItemVisual(DecorationItemData item)
        {
            var slot = roomSlots.Find(s => s.category == item.category);
            if (slot != null && slot.slotImage != null)
            {
                slot.slotImage.sprite = item.roomSprite;
                slot.slotImage.enabled = true;
            }
        }

        public void UnequipItemVisual(DecorationItemData item)
        {
            var slot = roomSlots.Find(s => s.category == item.category);
            if (slot != null && slot.slotImage != null)
            {
                slot.slotImage.sprite = null;
                slot.slotImage.enabled = false; 
            }
        }
    }
}