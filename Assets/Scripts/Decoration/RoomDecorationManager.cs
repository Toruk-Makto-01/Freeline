using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Hangi kategorinin hangi UI Slot'una (Image) denk geldiğini eşleştiren sınıf
    [System.Serializable]
    public class DecorationSlot
    {
        public DecorationCategory category;
        public Image slotImage; // ss1.png'deki Slot_Zemin, Slot_Duvar vb. objelerin Image bileşeni
    }

    public class RoomDecorationManager : MonoBehaviour
    {
        [Header("Oda Katmanları (Slotlar)")]
        [Tooltip("ss1.png'deki Slot objelerini buraya tanımlayın")]
        [SerializeField] private List<DecorationSlot> roomSlots = new();

        [Header("Katalog")]
        [SerializeField] private DecorationCatalog catalog;

        private void Start()
        {
            // Oyun başladığında odayı kayıt dosyasına göre inşa et!
            InitializeRoomFromSave();
        }

        public void InitializeRoomFromSave()
        {
            var equippedItems = GameManager.Instance.SaveManager.CurrentData.equippedDecorations;

            // Önce tüm slotları temizle (Gizle)
            foreach (var slot in roomSlots)
            {
                if (slot.slotImage != null)
                {
                    slot.slotImage.sprite = null;
                    slot.slotImage.enabled = false; // Beyaz ekranı önler!
                }
            }

            // Kayıt dosyasındaki takılı eşyaları bul ve ilgili slotta göster
            foreach (var equipped in equippedItems)
            {
                var itemData = catalog.GetItemById(equipped.itemId);
                if (itemData != null)
                {
                    EquipItemVisual(itemData);
                }
            }
        }

        // Sadece görseli güncelleyen metod (Kayıt işlemi MarketPanel'de yapılıyor)
        public void EquipItemVisual(DecorationItemData item)
        {
            var slot = roomSlots.Find(s => s.category == item.category);
            if (slot != null && slot.slotImage != null)
            {
                slot.slotImage.sprite = item.roomSprite; // Tam ekran olan oda görseli
                slot.slotImage.enabled = true; // Görsel atandı, görünür yap
            }
        }

        // Görseli odadan kaldıran metod
        public void UnequipItemVisual(DecorationItemData item)
        {
            var slot = roomSlots.Find(s => s.category == item.category);
            if (slot != null && slot.slotImage != null)
            {
                slot.slotImage.sprite = null;
                slot.slotImage.enabled = false; // Eşya çıktı, Image'i kapat (Beyaz ekranı engelle)
            }
        }
    }
}