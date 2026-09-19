using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ExhibitionPopup : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private ExhibitionScene exhibitionScene;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI stockInfoText; // Ürünlerin ve sayıların yazılacağı Text alanı

        [Header("Butonlar")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button closeButton;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (openButton != null) openButton.onClick.AddListener(OnOpenClicked);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void Start()
        {
            Hide();

            if (GameManager.Instance != null && GameManager.Instance.ExhibitionManager != null)
            {
                GameManager.Instance.ExhibitionManager.OnExhibitionDay += Show;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.ExhibitionManager != null)
            {
                GameManager.Instance.ExhibitionManager.OnExhibitionDay -= Show;
            }
        }

        public void Show()
        {
            // Paneli görünür ve tıklanabilir yap
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            // 1. Gün Durumunu Hesapla ve Yazdır
            int daysLeft = GameManager.Instance.ExhibitionManager.GetDaysUntilNextExhibition();
            bool isExhibitionActive = GameManager.Instance.ExhibitionManager.IsExhibitionDay;

            if (daysLeft == 0 && isExhibitionActive)
            {
                if (dayText != null) dayText.text = "Bugün sergi günü!";
                if (openButton != null) openButton.gameObject.SetActive(true);
                if (skipButton != null) skipButton.gameObject.SetActive(true);
                if (closeButton != null) closeButton.gameObject.SetActive(false);
            }
            else if (daysLeft == 0 && !isExhibitionActive)
            {
                if (dayText != null) dayText.text = "Bugünkü sergi tamamlandı. Sonraki sergi haftaya!";
                if (openButton != null) openButton.gameObject.SetActive(false);
                if (skipButton != null) skipButton.gameObject.SetActive(false);
                if (closeButton != null) closeButton.gameObject.SetActive(true);
            }
            else
            {
                if (dayText != null) dayText.text = $"Sonraki sergiye {daysLeft} gün kaldı...";
                if (openButton != null) openButton.gameObject.SetActive(false);
                if (skipButton != null) skipButton.gameObject.SetActive(false);
                if (closeButton != null) closeButton.gameObject.SetActive(true);
            }

            // 2. KİLİT NOKTA: Stoktaki Güncel Ürünleri ve Miktarları Yazdır
            RefreshStockDisplay();
        }

        private void RefreshStockDisplay()
        {
            if (stockInfoText == null) return;

            var stock = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;

            if (stock == null || stock.Count == 0)
            {
                stockInfoText.text = "<color=white>Sergi için henüz ürün üretilmedi.\n(Üretim masasından poster hazırlayın!)</color>";
                return;
            }

            StringBuilder sb = new StringBuilder();
            int totalCount = 0;

            foreach (var item in stock)
            {
                if (item.product != null && item.quantity > 0)
                {
                    sb.AppendLine($"• {item.product.productName}: {item.quantity} Adet");
                    totalCount += item.quantity;
                }
            }

            if (totalCount == 0)
            {
                stockInfoText.text = "<color=white>Stokta hiç ürün kalmadı!</color>";
            }
            else
            {
                stockInfoText.text = sb.ToString();
            }
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnOpenClicked()
        {
            GameManager.Instance.ExhibitionManager.StartExhibition();
            Hide();
            if (exhibitionScene != null) exhibitionScene.StartExhibition();
        }

        private void OnSkipClicked()
        {
            GameManager.Instance.ExhibitionManager.SkipExhibition();
            Hide();
        }
    }
}