using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public enum BarOrientation
    {
        Horizontal, // Yatay çubuk (X ekseni)
        Vertical    // Dikey çubuk (Y ekseni)
    }

    public class SlideBarGame : MonoBehaviour
    {
        [Header("Eksen Ayarı")]
        [SerializeField] private BarOrientation orientation = BarOrientation.Horizontal;

        [Header("Çubuk Arayüzü")]
        [SerializeField] private RectTransform backgroundBar;
        [Tooltip("Oyuncunun kontrol ettiği yakalama alanı (Yeşil Bar)")]
        [SerializeField] private RectTransform targetZone;
        [Tooltip("Otonom hareket eden hedef (Çizim İbresi)")]
        [SerializeField] private RectTransform indicator;

        [Header("Görsel Karalama (Shader)")]
        [SerializeField] private Image revealImage;

        [Header("Oyuncu Barı Fizik Ayarları")]
        [SerializeField] private float thrustForce = 1500f;     // Basılı tutunca uygulanan kaldırma gücü
        [SerializeField] private float gravityForce = 1200f;    // Bırakıldığında çeken yerçekimi
        [SerializeField] private float maxVelocity = 900f;      // Maksimum hız sınırı
        [SerializeField] private float bounceDamping = 0.25f;   // Uçlara çarpınca sekme oranı

        [Header("Hedef (Balık) AI Ayarları")]
        [SerializeField] private float baseFishSmoothTime = 0.6f;  // Daha yumuşak varış
        [SerializeField] private float fishMaxSpeed = 350f;         // Balığın aşamayacağı maksimum sakin hız
        [SerializeField] private float maxStepDistance = 200f;      // Tek seferde en fazla ne kadar uzağa gidebilir
        [SerializeField] private float minWaitTime = 0.6f;
        [SerializeField] private float maxWaitTime = 1.6f;

        [Header("Oyun İlerleme Hızı")]
        [SerializeField] private float progressRate = 0.35f; // Yeşil alan içindeyken resmin açılma hızı

        private float _currentSpeedMultiplier = 1f;
        private bool _isPlaying;
        private bool _isHolding;

        // Oyuncu Barı Fizik Değişkenleri
        private float _playerBarPosition;
        private float _playerVelocity;

        // Hedef AI Değişkenleri
        private float _fishPosition;
        private float _fishTargetPosition;
        private float _fishVelocity;
        private float _fishTimer;

        // İlerleme Değişkenleri
        private float _progress;
        private float _maxProgress;
        private Material _revealMaterial;

        public System.Action<bool> OnGameFinished;

        public void Setup(float speedMultiplier = 1f, Sprite targetSprite = null)
        {
            _currentSpeedMultiplier = Mathf.Max(0.5f, speedMultiplier);

            _progress = 0f;
            _maxProgress = 0f;
            _playerVelocity = 0f;
            _fishVelocity = 0f;
            _fishTimer = 0f;
            _isHolding = false;
            _isPlaying = true;

            // Bar ve hedefi başlangıç konumuna al
            _playerBarPosition = 0f;
            _fishPosition = 0f;
            _fishTargetPosition = GetTotalTravelArea() * 0.5f;

            UpdatePositionsUI();

            if (revealImage != null)
            {
                if (targetSprite != null) revealImage.sprite = targetSprite;
                _revealMaterial = new Material(revealImage.material);
                revealImage.material = _revealMaterial;
                _revealMaterial.SetFloat("_Progress", 0f);
            }
        }

        private void Update()
        {
            if (!_isPlaying) return;

            UpdatePlayerPhysics();
            UpdateFishAI();
            UpdatePositionsUI();
            EvaluateOverlapProgress();
        }

        private void UpdatePlayerPhysics()
        {
            float dt = Time.deltaTime;

            if (_isHolding)
            {
                _playerVelocity += thrustForce * dt;
            }
            else
            {
                _playerVelocity -= gravityForce * dt;
            }

            _playerVelocity = Mathf.Clamp(_playerVelocity, -maxVelocity, maxVelocity);
            _playerBarPosition += _playerVelocity * dt;

            float maxTravel = GetPlayerMaxTravel();

            if (_playerBarPosition <= 0f)
            {
                _playerBarPosition = 0f;
                _playerVelocity = -_playerVelocity * bounceDamping;
            }
            else if (_playerBarPosition >= maxTravel)
            {
                _playerBarPosition = maxTravel;
                _playerVelocity = -_playerVelocity * bounceDamping;
            }
        }

        private void UpdateFishAI()
        {
            float dt = Time.deltaTime;
            _fishTimer -= dt;

            float maxFishTravel = GetFishMaxTravel();

            if (_fishTimer <= 0f)
            {
                // Ekranın bir ucundan öbür ucuna uçmak yerine, mevcut konumunun sağına/soluna adım atar
                float step = Random.Range(-maxStepDistance, maxStepDistance);
                _fishTargetPosition = Mathf.Clamp(_fishPosition + step, 0f, maxFishTravel);

                // Zorluğa göre bekleme süresi
                float waitMin = minWaitTime / _currentSpeedMultiplier;
                float waitMax = maxWaitTime / _currentSpeedMultiplier;
                _fishTimer = Random.Range(waitMin, waitMax);
            }

            // smoothTime ne kadar yüksekse hareket o kadar ipeksi ve yumuşak olur
            float smoothTime = Mathf.Max(0.2f, baseFishSmoothTime / _currentSpeedMultiplier);
            float currentMaxSpeed = fishMaxSpeed * _currentSpeedMultiplier;

            _fishPosition = Mathf.SmoothDamp(_fishPosition, _fishTargetPosition, ref _fishVelocity, smoothTime, currentMaxSpeed, dt);
            _fishPosition = Mathf.Clamp(_fishPosition, 0f, maxFishTravel);
        }

        private void UpdatePositionsUI()
        {
            if (orientation == BarOrientation.Horizontal)
            {
                if (targetZone != null)
                    targetZone.anchoredPosition = new Vector2(_playerBarPosition, targetZone.anchoredPosition.y);

                if (indicator != null)
                    indicator.anchoredPosition = new Vector2(_fishPosition, indicator.anchoredPosition.y);
            }
            else
            {
                if (targetZone != null)
                    targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, _playerBarPosition);

                if (indicator != null)
                    indicator.anchoredPosition = new Vector2(indicator.anchoredPosition.x, _fishPosition);
            }
        }

        private void EvaluateOverlapProgress()
        {
            float dt = Time.deltaTime;

            float catchZoneStart = _playerBarPosition;
            float catchZoneSize = (orientation == BarOrientation.Horizontal) ? targetZone.rect.width : targetZone.rect.height;
            float catchZoneEnd = catchZoneStart + catchZoneSize;

            bool isInside = (_fishPosition >= catchZoneStart && _fishPosition <= catchZoneEnd);

            // Sadece hedef yeşil alanın içindeyse VE oyuncu butona basılı tutuyorsa ilerle
            if (isInside && _isHolding)
            {
                _progress += progressRate * dt;

                if (_progress > _maxProgress)
                {
                    _maxProgress = _progress;
                    if (_revealMaterial != null) _revealMaterial.SetFloat("_Progress", _maxProgress);
                }

                if (_progress >= 1f)
                {
                    EndGame();
                }
            }
        }
        // Dışındaysa hiçbir kaybetme/ceza yok; ilerleme sadece durur

        private float GetTotalTravelArea()
        {
            if (backgroundBar == null) return 500f;
            return (orientation == BarOrientation.Horizontal) ? backgroundBar.rect.width : backgroundBar.rect.height;
        }

        private float GetPlayerMaxTravel()
        {
            float total = GetTotalTravelArea();
            float zoneSize = (orientation == BarOrientation.Horizontal) ? targetZone.rect.width : targetZone.rect.height;
            return Mathf.Max(0f, total - zoneSize);
        }

        private float GetFishMaxTravel()
        {
            float total = GetTotalTravelArea();
            float fishSize = (orientation == BarOrientation.Horizontal) ? indicator.rect.width : indicator.rect.height;
            return Mathf.Max(0f, total - fishSize);
        }

        public void StartHolding()
        {
            _isHolding = true;
            if (AudioManager.Instance != null) AudioManager.Instance.StartPencilSound();
        }

        public void StopHolding()
        {
            _isHolding = false;
            if (AudioManager.Instance != null) AudioManager.Instance.StopDrawSound();
        }

        private void EndGame()
        {
            _isPlaying = false;
            _isHolding = false;
            OnGameFinished?.Invoke(true); // Her zaman başarılı biter
        }
    }
}