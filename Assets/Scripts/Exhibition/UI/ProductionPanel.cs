using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    
    public class ProductionPanel : MonoBehaviour
    {
        public static ProductionPanel Instance { get; private set; }

        [SerializeField] private Button backButton;

        [SerializeField] private ExhibitionProductData[] products;
        [SerializeField] private ProductionItemUI itemPrefab;
        [SerializeField] private Transform content;

        private void Awake()
        {
            Instance = this;

            if (backButton != null)
                backButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);

            RefreshList();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
        private void ProduceItem(ExhibitionProductData product)
        {
            // Enerji 
            var energyManager = GameManager.Instance.EnergyManager;

            if (energyManager.CurrentEnergy < product.energyCost)
            {
                Debug.Log("Yeterli enerji yok.");
                return;
            }

            energyManager.ConsumeEnergy(product.energyCost);

            // Time 
            GameManager.Instance.TimeManager.AdvanceTime(product.productionHours);


            // Stock 
            var stock = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;

            ExhibitionStockItem existing =
                stock.Find(item => item.product == product);

            if (existing != null)
            {
                existing.quantity++;
            }
            else
            {
                stock.Add(new ExhibitionStockItem
                {
                    product = product,
                    quantity = 1
                });
            }

            Debug.Log(product.productName + " stoğa eklendi.");
            RefreshList();
        }

        public void RefreshList()
        {
            // Eski elemanları sil
            foreach (Transform child in content)
                Destroy(child.gameObject);

            // Tüm ürünleri oluştur
            foreach (ExhibitionProductData product in products)
            {
                ProductionItemUI item = Instantiate(itemPrefab, content);

                item.Setup(product);
                item.OnProduceClicked += ProduceItem;
            }
        }
    }
}