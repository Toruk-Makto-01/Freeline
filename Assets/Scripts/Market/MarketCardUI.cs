using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class MarketCardUI : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buyButtonImage;

        [Header("Seçim Çerçevesi (Yeşil Outline)")]
        [SerializeField] private GameObject selectionOutline; // Kartın kendi içindeki yeşil çerçeve objesi

        [Header("Buton Görselleri")]
        [SerializeField] private Sprite defaultButtonSprite; // Satın alınmamış (Fiyatlı) buton görseli
        [SerializeField] private Sprite boughtButtonSprite;  // Satın alındı (tik/check) görseli

        public Button BuyButton => buyButton;

        public void Setup(Sprite sprite, Color fallbackColor, string name, string desc)
        {
            if (itemName != null) itemName.text = name;
            if (description != null) description.text = desc;

            if (sprite != null)
            {
                if (icon != null)
                {
                    icon.sprite = sprite;
                    icon.color = Color.white;
                }
            }
            else
            {
                if (icon != null) icon.color = fallbackColor;
            }
        }

        // Kartın görünümünü ve durumunu ayarlayan ana metodumuz
        public void SetCardState(bool isOwned, bool isEquipped, int price, bool canAfford, Sprite customBoughtSprite = null)
        {
            // 1. Yeşil Çerçeve (Outline) Yönetimi
            if (selectionOutline != null)
            {
                selectionOutline.SetActive(isEquipped); // Sadece odada takılı olan eşyada yeşil çerçeve yanar!
            }

            // 2. Buton Görseli ve Fiyat Yazısı Yönetimi
            if (isOwned)
            {
                // Satın alınmış ürün: Fiyat yazısını gizle, tik/satın alındı görselini koy
                if (priceText != null) priceText.text = "";

                Sprite iconToUse = customBoughtSprite != null ? customBoughtSprite : boughtButtonSprite;
                if (buyButtonImage != null && iconToUse != null)
                {
                    buyButtonImage.sprite = iconToUse;
                }

                if (buyButton != null) buyButton.interactable = true;
            }
            else
            {
                // Satın alınmamış ürün: Fiyatı yaz, varsayılan buton görselini koy
                if (priceText != null) priceText.text = $"{price}";

                if (buyButtonImage != null && defaultButtonSprite != null)
                {
                    buyButtonImage.sprite = defaultButtonSprite;
                }

                if (buyButton != null) buyButton.interactable = canAfford;
            }
        }
    }
}