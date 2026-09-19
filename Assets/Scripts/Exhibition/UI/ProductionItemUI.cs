using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class ProductionItemUI : MonoBehaviour
    {
        [Header("Kart Elemanlari")]
        [SerializeField] private Image posterImage;
        [SerializeField] private GameObject lockOverlay; // Üzerindeki kilit görseli/katmanı
        [SerializeField] private TextMeshProUGUI stockText; // "2X" yazısı
        [SerializeField] private Button cardButton;

        private ExhibitionProductData _product;
        private bool _isUnlocked;

        public event Action<ExhibitionProductData> OnCardClicked;

        public void Setup(ExhibitionProductData product, bool isUnlocked, int stockQuantity)
        {
            _product = product;
            _isUnlocked = isUnlocked;

            if (posterImage != null) posterImage.sprite = product.icon;

            // Kilit durumu
            if (lockOverlay != null) lockOverlay.SetActive(!_isUnlocked);

            // Stok durumu (Görseldeki gibi: Stok varsa "2X", yoksa boş)
            if (stockText != null)
            {
                stockText.text = stockQuantity > 0 ? $"{stockQuantity}X" : "";
            }

            // Tıklama
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() =>
            {
                if (_isUnlocked)
                {
                    OnCardClicked?.Invoke(_product);
                }
                else
                {
                    Debug.Log("[Üretim] Bu ürün kilitli! Açmak için sergi tamamlayın.");
                }
            });
        }
    }
}