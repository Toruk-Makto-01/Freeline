using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class ExhibitionPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private ExhibitionScene exhibitionScene;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            panel.SetActive(false);

            GameManager.Instance.ExhibitionManager.OnExhibitionDay += Show;

            openButton.onClick.AddListener(OnOpenClicked);
            skipButton.onClick.AddListener(OnSkipClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ExhibitionManager.OnExhibitionDay -= Show;
        }

        public void Show()
        {
            panel.SetActive(true);

            Debug.Log("Popup Açıldı");
            Debug.Log("IsExhibitionDay = " + GameManager.Instance.ExhibitionManager.IsExhibitionDay);

            bool exhibitionDay = GameManager.Instance.ExhibitionManager.IsExhibitionDay;

            if (exhibitionDay)
            {
                dayText.text = "Bugün sergi günü!";

                openButton.gameObject.SetActive(true);
                skipButton.gameObject.SetActive(true);
                closeButton.gameObject.SetActive(false);
            }
            else
            {
                dayText.text = "Sonraki sergiye yakında...";

                openButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public void OnOpenClicked()
        {
            Debug.Log("OPEN BUTTON");
            GameManager.Instance.ExhibitionManager.StartExhibition();
            Hide();

            exhibitionScene.StartExhibition();
        }

        public void OnSkipClicked()
        {
            GameManager.Instance.ExhibitionManager.SkipExhibition();
            Hide();
        }

        private void OnCloseClicked()
        {
            Hide();
        }
    }
}

