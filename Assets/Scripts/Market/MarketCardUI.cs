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
            int price,
            bool canAfford)
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

            buyButton.interactable = canAfford;
        }
    }
}