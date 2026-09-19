using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("Ana Menü Butonları")]
        [SerializeField] private Button newGameBtn;
        [SerializeField] private Button continueBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button creditsBtn;

        [Header("Bağlantılı Paneller")]
        [SerializeField] private GameObject hudCanvas;       // Oyunun ana HUD arayüzü
        [SerializeField] private SettingsPanel settingsPanel; // Mevcut ayarlar panelin
        [SerializeField] private GameObject creditsPanel;    // Ekibimiz paneli
        [SerializeField] private Button closeCreditsBtn;     // Ekibimiz panelinin X butonu

        private void Awake()
        {
            if (newGameBtn != null) newGameBtn.onClick.AddListener(OnNewGameClicked);
            if (continueBtn != null) continueBtn.onClick.AddListener(OnContinueClicked);
            if (settingsBtn != null) settingsBtn.onClick.AddListener(OnSettingsClicked);
            if (creditsBtn != null) creditsBtn.onClick.AddListener(OnCreditsClicked);

            if (closeCreditsBtn != null) closeCreditsBtn.onClick.AddListener(CloseCredits);
        }

        private void Start()
        {
            // Oyun açıldığında HUD gizli, MainMenu açık olsun
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            gameObject.SetActive(true);

            CheckSaveFile();
        }

        private void CheckSaveFile()
        {
            // Cihazda daha önce kaydedilmiş bir oyun var mı?
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "freeline_save.json");
            bool hasSave = System.IO.File.Exists(savePath);

            if (continueBtn != null)
            {
                // Kayıt yoksa "Devam Et" butonuna basılamaz
                continueBtn.interactable = hasSave;
            }
        }

        private void OnNewGameClicked()
        {
            // 1. Verileri sıfırla ve diske yaz
            if (GameManager.Instance != null && GameManager.Instance.SaveManager != null)
            {
                GameManager.Instance.SaveManager.StartNewGame();
            }

            // 2. Odayı sıfırdan varsayılan eşyalarla kur
            var roomManager = FindAnyObjectByType<RoomDecorationManager>(FindObjectsInactive.Include);
            if (roomManager != null)
            {
                roomManager.InitializeRoomFromSave();
            }

            // 3. Ana menüyü kapat ve oyunu başlat!
            StartGame();
        }

        private void OnContinueClicked()
        {
            // 2. Mevcut kaydı yükle ve yöneticilere uygula
            if (GameManager.Instance != null && GameManager.Instance.SaveManager != null)
            {
                GameManager.Instance.SaveManager.LoadGame();
                GameManager.Instance.SaveManager.ApplyToManagers();
            }

            StartGame();
        }

        private void StartGame()
        {
            // Ana menüyü kapat, oyun içi HUD'ı aç ve verileri ekrana bas
            gameObject.SetActive(false);

            if (hudCanvas != null)
            {
                hudCanvas.SetActive(true);
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.RefreshAllUI();
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.Open();
            }
        }

        private void OnCreditsClicked()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        private void CloseCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }
    }
}