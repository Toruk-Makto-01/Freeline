using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Freeline
{
    [System.Serializable]
    public class PhoneBackgroundItem
    {
        public Button button;      // Tıklanacak buton
        public Sprite bgSprite;    // O butona ait arkaplan görseli
    }

    public class BackgroundPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform selectionOutline; // Yeşil çerçeve objen

        [Header("Backgrounds")]
        [SerializeField] private List<PhoneBackgroundItem> backgroundItems;

        // Tüm telefon panellerinin dinleyeceği evrensel olay (Event)
        public static event System.Action<Sprite> OnBackgroundChanged;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            // Butonlara tıklama görevlerini (Listener) atıyoruz
            for (int i = 0; i < backgroundItems.Count; i++)
            {
                int index = i; // Closure sorunu yaşamamak için index'i kopyalıyoruz
                backgroundItems[i].button.onClick.AddListener(() => SelectBackground(index, true));
            }
        }

        private void Start()
        {
            // Oyun başladığında kaydedilmiş arkaplanı yükle
            int savedIndex = GameManager.Instance?.SaveManager?.CurrentData?.selectedPhoneBgIndex ?? 0;
            
            // Kayıtlı bir arkaplan yoksa veya liste dışına çıkmışsa 0'ı seç
            if (savedIndex < 0 || savedIndex >= backgroundItems.Count) savedIndex = 0;
            
            SelectBackground(savedIndex, false); 
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void SelectBackground(int index, bool saveToDisk)
        {
            if (index < 0 || index >= backgroundItems.Count) return;

            var selectedItem = backgroundItems[index];

            // 1. Yeşil çerçeveyi seçilen butonun içine taşı ve merkeze oturt
            selectionOutline.SetParent(selectedItem.button.transform);
            selectionOutline.anchoredPosition = Vector2.zero;
            selectionOutline.gameObject.SetActive(true);

            // 2. Yeni arkaplanı abone olan tüm panellere duyur
            OnBackgroundChanged?.Invoke(selectedItem.bgSprite);

            // 3. Oyuncu kendi seçtiyse veriyi kaydet
            if (saveToDisk && GameManager.Instance != null && GameManager.Instance.SaveManager != null)
            {
                GameManager.Instance.SaveManager.CurrentData.selectedPhoneBgIndex = index;
            }
        }
    }
}