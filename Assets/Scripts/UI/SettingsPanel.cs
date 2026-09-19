using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Music Settings")]
        [SerializeField] private Button musicBtn;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;

        [Header("SFX Settings")]
        [SerializeField] private Button soundBtn;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Language Settings")]
        [SerializeField] private TextMeshProUGUI languageText;
        [SerializeField] private Button langLeftBtn;
        [SerializeField] private Button langRightBtn;

        // Desteklenen dillerin listesi (Ileride burayı artırabiliriz)
        private readonly string[] _languages = { "Türkçe", "English", "Español", "Deutsch" };

        [Header("About Us")]
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private Button openInfoBtn;
        [SerializeField] private Button closeInfoBtn;

        [Header("Social Media Buttons")]
        [SerializeField] private Button googlePlayBtn;
        [SerializeField] private Button appStoreBtn;
        [SerializeField] private Button instagramBtn;
        [SerializeField] private Button tiktokBtn;
        [SerializeField] private Button supportBtn;
        [SerializeField] private Button xBtn;
        [SerializeField] private Button youtubeBtn;
        [SerializeField] private Button ZenitoonBtn;

        [SerializeField] private string googlePlayUrl = "https://play.google.com/store/apps/dev?id=YOUR_DEVELOPER_ID";
        [SerializeField] private string appStoreUrl = "https://apps.apple.com/developer/YOUR_DEVELOPER_ID";
        [SerializeField] private string instagramUrl = "https://www.instagram.com/YOUR_INSTAGRAM_HANDLE";
        [SerializeField] private string tiktokUrl = "https://www.tiktok.com/@YOUR_TIKTOK_HANDLE";
        [SerializeField] private string supportUrl = "https://www.yourwebsite.com/support";
        [SerializeField] private string xUrl = "https://twitter.com/YOUR_TWITTER_HANDLE";
        [SerializeField] private string youtubeUrl = "https://www.youtube.com/channel/YOUR_CHANNEL_ID";
        [SerializeField] private string zenitoonUrl = "https://www.zenitoon.com";

        [Header("General Settings")]
        [SerializeField] private Button playBtn;

        // --- Mevcut durumlar (State) ---
        private bool _isMusicOn = true;
        private bool _isSoundOn = true;
        private int _currentLangIndex = 0;

        private void Awake()
        {
            //  --- Buton tıklamalarını (Listener) bağlıyoruz ---
            if (musicBtn != null) musicBtn.onClick.AddListener(ToggleMusic);
            if (soundBtn != null) soundBtn.onClick.AddListener(ToggleSound);

            // Dil değiştirme butonlarına fonksiyonları bağlıyoruz
            if (langLeftBtn != null) langLeftBtn.onClick.AddListener(() => ChangeLanguage(-1));
            if (langRightBtn != null) langRightBtn.onClick.AddListener(() => ChangeLanguage(1));

            // Play butonuna Close fonksiyonunu bağlıyoruz
            if (playBtn != null) playBtn.onClick.AddListener(Close);

            // Hakkımızda panelini açıp kapatma fonksiyonlarını bağlıyoruz
            if (openInfoBtn != null && infoPanel != null)
                openInfoBtn.onClick.AddListener(() => infoPanel.SetActive(true));
            if (closeInfoBtn != null && infoPanel != null)
                closeInfoBtn.onClick.AddListener(() => infoPanel.SetActive(false));

            // Sosyal medya butonlarına URL açma fonksiyonlarını bağlıyoruz
            if (googlePlayBtn != null) googlePlayBtn.onClick.AddListener(() => Application.OpenURL(googlePlayUrl));
            if (appStoreBtn != null) appStoreBtn.onClick.AddListener(() => Application.OpenURL(appStoreUrl));
            if (instagramBtn != null) instagramBtn.onClick.AddListener(() => Application.OpenURL(instagramUrl));
            if (tiktokBtn != null) tiktokBtn.onClick.AddListener(() => Application.OpenURL(tiktokUrl));
            if (supportBtn != null) supportBtn.onClick.AddListener(() => Application.OpenURL($"mailto:{supportUrl}"));
            if (xBtn != null) xBtn.onClick.AddListener(() => Application.OpenURL(xUrl));
            if (youtubeBtn != null) youtubeBtn.onClick.AddListener(() => Application.OpenURL(youtubeUrl));
            if (ZenitoonBtn != null) ZenitoonBtn.onClick.AddListener(() => Application.OpenURL(zenitoonUrl));
        }

        private void Start()
        {
            // Oyun başladığında kaydedilmiş ayarları yükle
            LoadSettings();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            LoadSettings(); // Pannel her açıldığında görselleri güncelle
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // --- Müzik ve ses fonksiyonları ---
        private void ToggleMusic()
        {
            if (AudioManager.Instance != null)
            {
                bool newState = !AudioManager.Instance.IsMusicOn();
                AudioManager.Instance.SetMusicMute(!newState);
                _isMusicOn = newState;
            }
            else
            {
                _isMusicOn = !_isMusicOn;
                PlayerPrefs.SetInt("Settings_Music", _isMusicOn ? 1 : 0);
                PlayerPrefs.Save();
            }

            UpdateAudioVisuals();
        }

        private void ToggleSound()
        {
            if (AudioManager.Instance != null)
            {
                bool newState = !AudioManager.Instance.IsSfxOn();
                AudioManager.Instance.SetSfxMute(!newState);
                _isSoundOn = newState;
            }
            else
            {
                _isSoundOn = !_isSoundOn;
                PlayerPrefs.SetInt("Settings_Sound", _isSoundOn ? 1 : 0);
                PlayerPrefs.Save();
            }

            UpdateAudioVisuals();
        }

        private void LoadSettings()
        {
            if (AudioManager.Instance != null)
            {
                _isMusicOn = AudioManager.Instance.IsMusicOn();
                _isSoundOn = AudioManager.Instance.IsSfxOn();
            }
            else
            {
                _isMusicOn = PlayerPrefs.GetInt("Settings_Music", 1) == 1;
                _isSoundOn = PlayerPrefs.GetInt("Settings_Sound", 1) == 1;
            }

            _currentLangIndex = PlayerPrefs.GetInt("Settings_Language", 0);

            UpdateAudioVisuals();
            UpdateLanguageVisuals();
        }

        private void UpdateAudioVisuals()
        {
            if (musicIcon != null)
                musicIcon.sprite = _isMusicOn ? musicOnSprite : musicOffSprite;

            if (soundIcon != null)
                soundIcon.sprite = _isSoundOn ? soundOnSprite : soundOffSprite;
        }

        // --- Dil Değiştirme Fonksiyonu ---
        private void ChangeLanguage(int direction)
        {
            _currentLangIndex += direction;

            // Liste dışına çıkmayı engelle (Sondayken sağa basarsa başa döner, baştayken sola basarsa sona döner)
            if (_currentLangIndex < 0)
                _currentLangIndex = _languages.Length - 1;
            else if (_currentLangIndex >= _languages.Length)
                _currentLangIndex = 0;

            PlayerPrefs.SetInt("Settings_Language", _currentLangIndex);
            PlayerPrefs.Save();

            UpdateLanguageVisuals();

            // ILERDE EKLENECEK: GameManager.Instance.LocalizationManager.ChangeLanguage(_language[_currentLangIndex]);
        }

        private void UpdateLanguageVisuals()
        {
            if (languageText != null)
                languageText.text = _languages[_currentLangIndex];
        }
    }
}
