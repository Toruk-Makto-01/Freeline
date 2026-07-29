using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class ExhibitionScene : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image productImage;
        [SerializeField] private TextMeshProUGUI productNameText;
        [SerializeField] private TextMeshProUGUI offerPriceText;
        [SerializeField] private TextMeshProUGUI npcText;

        [SerializeField] private Button acceptButton;
        [SerializeField] private Button bargainButton;
        [SerializeField] private Button rejectButton;

        [SerializeField] private ExhibitionSummaryPanel summaryPanel;

        [SerializeField] private Image npcImage;

        [SerializeField] private Sprite[] npcSprites;

        [SerializeField]
        private string[] greetings =
        {
            "Merhaba!",
            "İyi günler.",
            "Şuna bir bakabilir miyim?",
            "Bu dikkatimi çekti.",
            "Oldukça hoş görünüyor.",
            "İlginç bir çalışma.",
            "Bunu satın almayı düşünüyorum.",
            "Vitrinden gözüme çarptı."
        };
        [SerializeField]
        private string[] acceptedTexts =
        {
            "Peki, kabul.",
            "Tamam, biraz artırabilirim.",
            "Senin için olur.",
            "Anlaştık.",
            "İkna oldum."
            };
        [SerializeField]
        private string[] rejectedTexts =
        {
            "Hayır, bu son teklifim.",
            "Daha fazlasını veremem.",
            "Maalesef olmaz.",
            "Bu fiyattan yukarı çıkamam.",
            "Teklifim değişmeyecek."
        };

        private ExhibitionStockItem currentStockItem;
        private int currentOffer;
        private bool bargainUsed;
        private int currentCustomer;
        [SerializeField] private int maxCustomers = 10;

        private int totalEarnedCoins;
        private int soldProducts;

        private void Awake()
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
            bargainButton.onClick.AddListener(OnBargainClicked);
            rejectButton.onClick.AddListener(OnRejectClicked);
        }

        private void Start()
        {
            panel.SetActive(false);
        }

        public void StartExhibition()
        {
            currentCustomer = 0;
            totalEarnedCoins = 0;
            soldProducts = 0;

            panel.SetActive(true);

            ShowNextCustomer();
        }
        private void EndExhibition()
        {
            Debug.Log("Sergi tamamlandı.");

            Debug.Log($"Toplam Kazanç : {totalEarnedCoins}");
            Debug.Log($"Satılan Ürün : {soldProducts}");

            int remainingProducts = 0;

            foreach (var item in GameManager.Instance.SaveManager.CurrentData.exhibitionStock)
            {
                remainingProducts += item.quantity;
            }

            Hide();

            summaryPanel.Open(
                soldProducts,
                totalEarnedCoins,
                remainingProducts);
        }

        public void Show()
        {
            Debug.Log("SHOW SCENE");
            panel.SetActive(true);
            bargainUsed = false;

            if (npcSprites.Length > 0)
                npcImage.sprite = npcSprites[Random.Range(0, npcSprites.Length)];

            string greeting =
                greetings.Length > 0
                    ? greetings[Random.Range(0, greetings.Length)]
                    : "Merhaba!";

            var stock = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;

            if (stock.Count == 0)
            {
                EndExhibition();
                return;
            }

            currentStockItem = stock[Random.Range(0, stock.Count)];

            ExhibitionProductData product = currentStockItem.product;

            productImage.sprite = product.icon;
            productNameText.text = product.productName;

            currentOffer = Random.Range(
                Mathf.RoundToInt(product.basePrice * 0.8f),
                Mathf.RoundToInt(product.basePrice * 1.2f));

            offerPriceText.text = currentOffer + " Coin";

            npcText.text =
                $"{greeting}\n\n{product.productName} için\n{currentOffer} Coin teklif ediyorum.";

            bargainButton.interactable = true;
        }

        public void Hide()
        {
            panel.SetActive(false);
        }
        private void ShowNextCustomer()
        {
            currentCustomer++;

            if (currentCustomer > maxCustomers)
            {
                EndExhibition();
                return;
            }

            Show();
        }

        //Kabul et Butonu
        private void OnAcceptClicked()
        {
            var save = GameManager.Instance.SaveManager.CurrentData;

            // Coin ver
            save.currentCoins += currentOffer;
            totalEarnedCoins += currentOffer;
            soldProducts++;

            // Stok azalt
            currentStockItem.quantity--;

            // Ürün bittiyse listeden sil
            if (currentStockItem.quantity <= 0)
            {
                save.exhibitionStock.Remove(currentStockItem);
            }

            Debug.Log($"{currentOffer} Coin kazanıldı.");

            if (ProductionPanel.Instance != null)
                ProductionPanel.Instance.RefreshList();

            // Sonraki müşteri
            ShowNextCustomer();
        }

        // Bargain Butonu
        private void OnBargainClicked()
        {
            if (bargainUsed)
                return;

            bargainUsed = true;

            bool accepted = Random.value < 0.5f;

            if (accepted)
            {
                currentOffer = Mathf.RoundToInt(currentOffer * 1.15f);

                offerPriceText.text = currentOffer + " Coin";

                npcText.text =
                    acceptedTexts[Random.Range(0, acceptedTexts.Length)]
                    + $"\n\n{currentStockItem.product.productName} için\n{currentOffer} Coin olur.";

                bargainButton.interactable = false;
            }
            else
            {
                npcText.text =
                    rejectedTexts[Random.Range(0, rejectedTexts.Length)] + $"\n\nTeklifim {currentOffer} Coin.";

                bargainButton.interactable = false;
            }
        }

        // Reddet Butonu
        private void OnRejectClicked()
        {
            ShowNextCustomer();
        }
    }
}