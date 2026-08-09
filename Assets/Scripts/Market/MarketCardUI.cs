using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class MarketCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;

        public Button BuyButton => buyButton;

        public void Setup(
            Sprite sprite,
            Color fallbackColor,
            string name,
            string desc,
            int price)
        {
            itemName.text = name;
            description.text = desc;
            priceText.text = $"{price} Coin";

            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                icon.color = fallbackColor;
            }
        }

        // Butonun görünümünü duruma göre güncelleyen metot
        public void SetButtonState(bool isOwned, bool isEquipped, int currentCoins, int price)
        {
            Image btnImg = buyButton.GetComponent<Image>();

            if (isEquipped)
            {
                buyButtonText.text = "Kullaniliyor";
                btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Yeşil tonu
                buyButton.interactable = false; // Zaten kullanılıyorsa tıklanamaz
            }
            else if (isOwned)
            {
                buyButtonText.text = "Kullan";
                btnImg.color = new Color(0.2f, 0.4f, 0.8f, 1f); // Mavi tonu
                buyButton.interactable = true; // Her zaman tıklanabilir
            }
            else
            {
                buyButtonText.text = "Satin Al";
                btnImg.color = new Color(0.8f, 0.3f, 0.3f, 1f); // Kırmızımsı standart buton
                buyButton.interactable = currentCoins >= price;
            }
        }
    }
}