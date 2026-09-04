using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class SlideBarGame : MonoBehaviour
    {
        [Header("Çubuk Arayüzü")]
        [SerializeField] private RectTransform backgroundBar;
        [SerializeField] private RectTransform targetZone;
        [SerializeField] private RectTransform indicator;
        [SerializeField] private Image mistakeBarFill;
        
        [Header("Görsel Karalama (Shader)")]
        [SerializeField] private Image revealImage; // Artık Maske yok, doğrudan hedef resmi buraya sürüklüyoruz
        
        [Header("Oyun Ayarları")]
        [SerializeField] private float indicatorSpeed = 2f; 
        [SerializeField] private float targetMoveSpeed = 0.5f; 
        [SerializeField] private float progressRate = 0.2f; 
        [SerializeField] private float mistakeRate = 0.3f; 
        
        private bool _isPlaying;
        private bool _isHolding;
        private float _progress;
        private float _maxProgress; 
        private float _mistakeLevel; 
        private int _direction = 1;

        private Material _revealMaterial; // Ekran kartına veri yollayacağımız köprü
        
        public System.Action<bool> OnGameFinished;

        public void Setup()
        {
            _progress = 0f;
            _maxProgress = 0f;
            _mistakeLevel = 0f;
            _direction = 1;
            _isHolding = false;
            _isPlaying = true;
            
            if (mistakeBarFill != null) mistakeBarFill.fillAmount = 0f;
            
            // Materyalin bir kopyasını al ki diğer UI objeleri bozulmasın
            if (revealImage != null)
            {
                _revealMaterial = new Material(revealImage.material);
                revealImage.material = _revealMaterial;
                _revealMaterial.SetFloat("_Progress", 0f);
            }
        }

        private void Update()
        {
            if (!_isPlaying) return;

            MoveIndicator();
            MoveTargetZone();

            if (_isHolding) ProcessHoldingLogic();
        }

        private void MoveIndicator()
        {
            float barWidth = backgroundBar.rect.width;
            float currentX = indicator.anchoredPosition.x;
            
            currentX += _direction * indicatorSpeed * barWidth * Time.deltaTime;
            
            if (currentX >= barWidth)
            {
                currentX = barWidth;
                _direction = -1;
            }
            else if (currentX <= 0f)
            {
                currentX = 0f;
                _direction = 1;
            }

            indicator.anchoredPosition = new Vector2(currentX, indicator.anchoredPosition.y);
        }

        private void MoveTargetZone()
        {
            float maxMoveArea = backgroundBar.rect.width - targetZone.rect.width;
            float sineValue = (Mathf.Sin(Time.time * targetMoveSpeed) + 1f) / 2f; 
            targetZone.anchoredPosition = new Vector2(sineValue * maxMoveArea, targetZone.anchoredPosition.y);
        }

        private void ProcessHoldingLogic()
        {
            float indicatorX = indicator.anchoredPosition.x;
            float targetStartX = targetZone.anchoredPosition.x;
            float targetEndX = targetStartX + targetZone.rect.width;

            if (indicatorX >= targetStartX && indicatorX <= targetEndX)
            {
                _progress += progressRate * Time.deltaTime;
                
                if (_progress > _maxProgress) 
                {
                    _maxProgress = _progress;
                    // Yüzde rekorunu direkt ekran kartına (Shader'a) fırlat
                    if (_revealMaterial != null) _revealMaterial.SetFloat("_Progress", _maxProgress);
                }

                if (_progress >= 1f) EndGame(true);
            }
            else
            {
                _progress = Mathf.Max(0f, _progress - (progressRate * Time.deltaTime)); 
                _mistakeLevel += mistakeRate * Time.deltaTime;
                
                if (mistakeBarFill != null) mistakeBarFill.fillAmount = _mistakeLevel;
                if (_mistakeLevel >= 1f) EndGame(false);
            }
        }

        public void StartHolding() => _isHolding = true;
        public void StopHolding() => _isHolding = false;

        private void EndGame(bool isSuccess)
        {
            _isPlaying = false;
            _isHolding = false;
            OnGameFinished?.Invoke(isSuccess);
        }
    }
}