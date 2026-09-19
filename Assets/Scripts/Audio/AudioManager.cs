using System.Collections;
using UnityEngine;

namespace Freeline
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Ses Kaynakları (Audio Sources)")]
        [SerializeField] private AudioSource musicSource;     // Arka plan müziği için (BGM)
        [SerializeField] private AudioSource sfxSource;       // Tıklama, pop, panel açılış sesleri için
        [SerializeField] private AudioSource loopBrushSource; // Fırça ve kalem sürtünme sesi için

        [Header("Müzik Dosyası")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("UI Ses Dosyaları")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip panelOpenClip;
        [SerializeField] private AudioClip panelCloseClip;
        [SerializeField] private AudioClip errorClip;

        [Header("Çizim Ses Dosyaları")]
        [SerializeField] private AudioClip brushLoopClip;
        [SerializeField] private AudioClip pencilLoopClip;

        [Header("Müzik Ses Seviyeleri")]
        [Range(0f, 1f)][SerializeField] private float normalMusicVolume = 0.8f;
        [Range(0f, 1f)][SerializeField] private float duckedMusicVolume = 0.35f; // Panel açıkken kısılacak seviye

        public bool isVibrationEnabled = true;

        private int _openPanelsCount = 0;
        private Coroutine _fadeCoroutine;
        private bool _isMusicMuted = false;
        private bool _isSfxMuted = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Kaydedilmiş ayarları yükle
            _isMusicMuted = PlayerPrefs.GetInt("Settings_Music", 1) == 0;
            _isSfxMuted = PlayerPrefs.GetInt("Settings_Sound", 1) == 0;

            SetupMusicSource();
        }

        private void Start()
        {
            ApplyAudioSettings();
            PlayBackgroundMusic();
        }

        private void SetupMusicSource()
        {
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = normalMusicVolume;
                if (backgroundMusic != null) musicSource.clip = backgroundMusic;
            }

            if (loopBrushSource != null)
            {
                loopBrushSource.loop = true;
                loopBrushSource.playOnAwake = false;
            }
        }

        public void PlayBackgroundMusic()
        {
            if (musicSource != null && backgroundMusic != null && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        // ==========================================
        // --- MÜZİK KISMA / DUCKING MEKANİĞİ ---
        // ==========================================

        public void RegisterPanelOpen()
        {
            _openPanelsCount++;
            UpdateMusicDucking();
        }

        public void RegisterPanelClose()
        {
            _openPanelsCount = Mathf.Max(0, _openPanelsCount - 1);
            UpdateMusicDucking();
        }

        private void UpdateMusicDucking()
        {
            if (_isMusicMuted || musicSource == null) return;

            float targetVol = (_openPanelsCount > 0) ? duckedMusicVolume : normalMusicVolume;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeMusicVolumeRoutine(targetVol, 0.3f));
        }

        private IEnumerator FadeMusicVolumeRoutine(float targetVolume, float duration)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
                yield return null;
            }

            musicSource.volume = targetVolume;
        }

        // ==========================================
        // --- AYARLAR MENÜSÜ KONTROLLERİ ---
        // ==========================================

        public void SetMusicMute(bool isMuted)
        {
            _isMusicMuted = isMuted;
            PlayerPrefs.SetInt("Settings_Music", _isMusicMuted ? 0 : 1);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        public void SetSfxMute(bool isMuted)
        {
            _isSfxMuted = isMuted;
            PlayerPrefs.SetInt("Settings_Sound", _isSfxMuted ? 0 : 1);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        public bool IsMusicOn() => !_isMusicMuted;
        public bool IsSfxOn() => !_isSfxMuted;

        private void ApplyAudioSettings()
        {
            if (musicSource != null)
            {
                musicSource.mute = _isMusicMuted;
                if (!_isMusicMuted)
                {
                    musicSource.volume = (_openPanelsCount > 0) ? duckedMusicVolume : normalMusicVolume;
                }
            }

            if (sfxSource != null) sfxSource.mute = _isSfxMuted;
            if (loopBrushSource != null) loopBrushSource.mute = _isSfxMuted;
        }

        // ==========================================
        // --- SFX & ÇİZİM TETİKLEYİCİLERİ ---
        // ==========================================

        public void PlayButtonClick()
        {
            if (_isSfxMuted) return;
            if (buttonClickClip != null && sfxSource != null) sfxSource.PlayOneShot(buttonClickClip);
            TriggerVibration();
        }

        public void PlayPanelOpen()
        {
            if (_isSfxMuted) return;
            if (panelOpenClip != null && sfxSource != null) sfxSource.PlayOneShot(panelOpenClip);
        }

        public void PlayPanelClose()
        {
            if (_isSfxMuted) return;
            if (panelCloseClip != null && sfxSource != null) sfxSource.PlayOneShot(panelCloseClip);
        }

        public void PlayError()
        {
            if (_isSfxMuted) return;
            if (errorClip != null && sfxSource != null) sfxSource.PlayOneShot(errorClip);
            TriggerVibration();
        }

        public void StartBrushSound()
        {
            if (_isSfxMuted || loopBrushSource == null || brushLoopClip == null) return;
            if (loopBrushSource.isPlaying && loopBrushSource.clip == brushLoopClip) return;

            loopBrushSource.clip = brushLoopClip;
            loopBrushSource.Play();
        }

        public void StartPencilSound()
        {
            if (_isSfxMuted || loopBrushSource == null || pencilLoopClip == null) return;
            if (loopBrushSource.isPlaying && loopBrushSource.clip == pencilLoopClip) return;

            loopBrushSource.clip = pencilLoopClip;
            loopBrushSource.Play();
        }

        public void StopDrawSound()
        {
            if (loopBrushSource != null && loopBrushSource.isPlaying)
            {
                loopBrushSource.Stop();
            }
        }

        private void TriggerVibration()
        {
            if (isVibrationEnabled)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }
}