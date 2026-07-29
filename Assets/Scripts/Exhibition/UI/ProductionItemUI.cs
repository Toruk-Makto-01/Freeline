using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Freeline
{
    public class ProductionItemUI : MonoBehaviour
    {
        [SerializeField] private Image productImage;
        [SerializeField] private TextMeshProUGUI productNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI stockText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button produceButton;


        private ExhibitionProductData currentProduct;

        public event Action<ExhibitionProductData> OnProduceClicked;

        public void Setup(ExhibitionProductData product)
        {
            currentProduct = product;

            productImage.sprite = product.icon;
            productNameText.text = product.productName;
            priceText.text = product.basePrice + " Coin";

            infoText.text =
                $"Enerji: {product.energyCost}\n" +
                $"Süre: {product.productionHours} Saat";


            int stock = 0;

            foreach (var item in GameManager.Instance.SaveManager.CurrentData.exhibitionStock)
            {
                if (item.product == product)
                {
                    stock = item.quantity;
                    break;
                }
            }

            stockText.text = "Stok : x" + stock;

            produceButton.onClick.RemoveAllListeners();
            produceButton.onClick.AddListener(Produce);
        }

        private void Produce()
        {
            OnProduceClicked?.Invoke(currentProduct);
        }
    }
}