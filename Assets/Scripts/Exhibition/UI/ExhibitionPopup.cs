using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    [RequireComponent(typeof(CanvasGroup))] // Unity'nin Canvas Group'u otomatik eklemesini sağlar
    public class ExhibitionPopup : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private ExhibitionScene exhibitionScene;
        [SerializeField] private TextMeshProUGUI dayText;
        
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
            // Oyun başladığında paneli görünmez yap ama scripti açık bırak!
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
            // Paneli %100 görünür ve tıklanabilir yap
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            int daysLeft = GameManager.Instance.ExhibitionManager.GetDaysUntilNextExhibition();
            bool isExhibitionActive = GameManager.Instance.ExhibitionManager.IsExhibitionDay;

            if (daysLeft == 0 && isExhibitionActive)
            {
                dayText.text = "Bugün sergi günü!";
                openButton.gameObject.SetActive(true);
                skipButton.gameObject.SetActive(true);
                closeButton.gameObject.SetActive(false);
            }
            else if (daysLeft == 0 && !isExhibitionActive)
            {
                dayText.text = "Bugünkü sergi tamamlandı. Sonraki sergi haftaya!";
                openButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);
            }
            else
            {
                dayText.text = $"Sonraki sergiye {daysLeft} gün kaldı...";
                openButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            // Paneli görünmez yap ve tıklamaları engelle
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