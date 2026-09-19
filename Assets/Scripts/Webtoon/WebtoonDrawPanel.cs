using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

namespace Freeline
{
    public class WebtoonDrawPanel : MonoBehaviour
    {
        [Header("Arayüz Elementleri")]
        [SerializeField] private RawImage artworkImage;
        [SerializeField] private RawImage overlayMask;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private GameObject flashEffect;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Fırça Ayarları (Brush Stamp)")]
        [SerializeField] private Texture2D brushTexture;
        [SerializeField] private float brushScale = 1.2f; // Daha geniş kapatıcılık için hafif büyütüldü
        [SerializeField] private float brushSpacing = 0.08f;
        [SerializeField] private float completionThreshold = 0.80f; // %80'e ulaşıldığında kilitlenmeden tamamlanır

        private WebtoonChapterData _currentChapter;
        private Texture2D _activeSketch;
        private Texture2D _activeColor;

        private Texture2D _maskTexture;
        private Color32[] _maskColors;
        private Color32[] _brushPixels;
        private int _width, _height, _brushWidth, _brushHeight;
        private int _totalPixels, _paintedPixels;

        private bool _isColorPhase = false;
        private bool _isInteractable = false;
        private bool _textureNeedsApply = false;

        private Vector2 _lastDrawPos;
        private bool _isDrawing;

        public void StartDrawing(WebtoonChapterData chapter)
        {
            _currentChapter = chapter;
            _activeColor = chapter.colorTexture;
            _isColorPhase = false;
            _isInteractable = true;
            _isDrawing = false;

            if (flashEffect != null) flashEffect.SetActive(false);

            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                // Oyuncunun eksik yerlere geri dönebilmesi için dikey kaydırma açık kalır
                scrollRect.vertical = true;
                scrollRect.inertia = true;
            }

            if (brushTexture != null)
            {
                _brushPixels = brushTexture.GetPixels32();
                _brushWidth = brushTexture.width;
                _brushHeight = brushTexture.height;
            }

            PrepareSketch();
            SetupPhase(false);
        }

        private void PrepareSketch()
        {
            if (_currentChapter.sketchTexture != null)
            {
                _activeSketch = _currentChapter.sketchTexture;
                return;
            }

            _activeSketch = new Texture2D(_activeColor.width, _activeColor.height, TextureFormat.RGBA32, false);
            Color32[] pixels = _activeColor.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                byte gray = (byte)((c.r * 0.3f) + (c.g * 0.59f) + (c.b * 0.11f));
                pixels[i] = new Color32(gray, gray, gray, c.a);
            }

            _activeSketch.SetPixels32(pixels);
            _activeSketch.Apply();
        }

        private void SetupPhase(bool isColorPhase)
        {
            _isColorPhase = isColorPhase;
            _paintedPixels = 0;

            if (phaseText != null) phaseText.text = isColorPhase ? "AŞAMA 2: RENKLENDİRME" : "AŞAMA 1: ESKİZ";
            if (progressText != null) progressText.text = "%0";

            _width = _activeColor.width;
            _height = _activeColor.height;
            _totalPixels = _width * _height;

            artworkImage.texture = isColorPhase ? _activeColor : _activeSketch;

            _maskTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _maskColors = new Color32[_totalPixels];

            if (!isColorPhase)
            {
                Color32 whitePaper = new Color32(255, 255, 255, 255);
                for (int i = 0; i < _totalPixels; i++) _maskColors[i] = whitePaper;
            }
            else
            {
                Color32[] sketchPixels = _activeSketch.GetPixels32();
                for (int i = 0; i < _totalPixels; i++) _maskColors[i] = sketchPixels[i];
            }

            _maskTexture.SetPixels32(_maskColors);
            _maskTexture.Apply();
            overlayMask.texture = _maskTexture;

            AspectRatioFitter fitter = artworkImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = artworkImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = (float)_width / _height;

            RectTransform maskRt = overlayMask.rectTransform;
            maskRt.SetParent(artworkImage.transform);
            maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero; maskRt.offsetMax = Vector2.zero;

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // En baştan başla
        }

        private void Update()
        {
            if (!_isInteractable || brushTexture == null || Pointer.current == null) return;

            bool isPressedDown = Pointer.current.press.wasPressedThisFrame;
            bool isPressing = Pointer.current.press.isPressed;
            bool isReleased = Pointer.current.press.wasReleasedThisFrame;
            Vector2 pointerPos = Pointer.current.position.ReadValue();

            if (isPressedDown)
            {
                if (TryGetPixelPosition(pointerPos, out Vector2 pos))
                {
                    _isDrawing = true;
                    if (scrollRect != null) scrollRect.vertical = false; // Çizerken kaydırmayı kilitle
                    StampBrush((int)pos.x, (int)pos.y);
                    _lastDrawPos = pos;

                    // --- SES BAŞLAT ---
                    if (AudioManager.Instance != null) AudioManager.Instance.StartBrushSound();
                }
            }
            else if (isPressing && _isDrawing)
            {
                if (TryGetPixelPosition(pointerPos, out Vector2 currentPos))
                {
                    InterpolateAndStamp(_lastDrawPos, currentPos);
                    _lastDrawPos = currentPos;

                    // Ekranın altına yaklaşırsa hafifçe otomatik aşağı kaydır
                    CheckAutoScroll(pointerPos);
                }
            }
            else if (isReleased)
            {
                _isDrawing = false;
                if (scrollRect != null) scrollRect.vertical = true; // Parmağı kaldırdığında serbestçe kaydırıp eksik yerlere bakabilsin

                // --- SES DURDUR ---
                if (AudioManager.Instance != null) AudioManager.Instance.StopDrawSound();
            }

            if (_textureNeedsApply)
            {
                _maskTexture.SetPixels32(_maskColors);
                _maskTexture.Apply();
                UpdateProgress((float)_paintedPixels / _totalPixels);
                _textureNeedsApply = false;
            }
        }

        private void OnDisable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopDrawSound();
            }
        }

        private void CheckAutoScroll(Vector2 screenPos)
        {
            if (scrollRect == null) return;

            // Eğer parmak ekranın en alt %15'lik kısmındaysa sayfayı yavaşça aşağı kaydır
            if (screenPos.y < Screen.height * 0.15f)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - (0.3f * Time.deltaTime));
            }
            // Ekranın en üst %15'lik kısmındaysa yukarı kaydır
            else if (screenPos.y > Screen.height * 0.85f)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + (0.3f * Time.deltaTime));
            }
        }

        private bool TryGetPixelPosition(Vector2 screenPos, out Vector2 pixelPos)
        {
            pixelPos = Vector2.zero;
            RectTransform rt = overlayMask.rectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localPoint))
            {
                float normalizedX = (localPoint.x + rt.rect.width * 0.5f) / rt.rect.width;
                float normalizedY = (localPoint.y + rt.rect.height * 0.5f) / rt.rect.height;

                if (normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f)
                {
                    pixelPos.x = normalizedX * _width;
                    pixelPos.y = normalizedY * _height;
                    return true;
                }
            }
            return false;
        }

        private void InterpolateAndStamp(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            float scaledBrushSize = _brushWidth * brushScale;
            float stepDistance = Mathf.Max(1f, scaledBrushSize * brushSpacing);

            if (distance > stepDistance)
            {
                int steps = Mathf.CeilToInt(distance / stepDistance);
                for (int i = 1; i <= steps; i++)
                {
                    Vector2 point = Vector2.Lerp(start, end, (float)i / steps);
                    StampBrush((int)point.x, (int)point.y);
                }
            }
        }

        private void StampBrush(int centerX, int centerY)
        {
            Color32 clearColor = new Color32(0, 0, 0, 0);

            int scaledWidth = Mathf.RoundToInt(_brushWidth * brushScale);
            int scaledHeight = Mathf.RoundToInt(_brushHeight * brushScale);

            int startX = centerX - scaledWidth / 2;
            int startY = centerY - scaledHeight / 2;

            for (int y = 0; y < scaledHeight; y++)
            {
                int py = startY + y;
                if (py < 0 || py >= _height) continue;

                for (int x = 0; x < scaledWidth; x++)
                {
                    int px = startX + x;
                    if (px < 0 || px >= _width) continue;

                    int bx = Mathf.RoundToInt((x / (float)scaledWidth) * _brushWidth);
                    int by = Mathf.RoundToInt((y / (float)scaledHeight) * _brushHeight);
                    bx = Mathf.Clamp(bx, 0, _brushWidth - 1);
                    by = Mathf.Clamp(by, 0, _brushHeight - 1);

                    int brushIndex = by * _brushWidth + bx;

                    if (_brushPixels[brushIndex].a > 25) // Eşik hafif düşürüldü, daha çok pikseli temizler
                    {
                        int maskIndex = py * _width + px;
                        if (_maskColors[maskIndex].a != 0)
                        {
                            _maskColors[maskIndex] = clearColor;
                            _paintedPixels++;
                            _textureNeedsApply = true;
                        }
                    }
                }
            }
        }

        private void UpdateProgress(float rawProgress)
        {
            // %80 eşiğine ulaşıldığında oyuncuya %100 göster ve tamamla
            float normalizedProgress = Mathf.Clamp01(rawProgress / completionThreshold);

            if (progressText != null)
                progressText.text = $"%{Mathf.FloorToInt(normalizedProgress * 100)}";

            if (normalizedProgress >= 1f && _isInteractable)
            {
                _isInteractable = false;
                StartCoroutine(HandlePhaseTransition());
            }
        }

        private IEnumerator HandlePhaseTransition()
        {
            if (flashEffect != null) flashEffect.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            if (flashEffect != null) flashEffect.SetActive(false);

            if (!_isColorPhase)
            {
                SetupPhase(true);
                _isInteractable = true;
            }
            else FinishGame();
        }

        private void FinishGame()
        {
            WebtoonManager.Instance.PublishChapter(_currentChapter);
            gameObject.SetActive(false);

            var panel = Object.FindAnyObjectByType<WebtoonPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                panel.RefreshUI();
            }
        }
    }
}