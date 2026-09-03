using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Freeline
{
    public class DrawRevealGame : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private RawImage maskImage;
        [SerializeField] private Texture2D targetLineTexture;
        [SerializeField] private int brushRadius = 35;
        [SerializeField] private float completionThreshold = 0.85f; // Çizgilerin %85'i bitince tamamlanır

        public float CompletionThreshold => completionThreshold;

        private Texture2D _maskTexture;
        private Color32[] _colors;
        private int _width, _height;
        private bool _isCompleted;

        // Birebir Eşleme Dizileri
        private bool[] _isLineMap;        // Çizginin bulunduğu pikseller
        private bool[] _paintedLineMap;   // Oyuncunun boyadığı çizgi pikselleri
        private int _totalLinePixels;
        private int _paintedLinePixels;

        public System.Action OnDrawingCompleted;
        public System.Action<float> OnProgressChanged;

        public void Setup()
        {
            RectTransform rt = maskImage.rectTransform;
            _width = Mathf.FloorToInt(rt.rect.width);
            _height = Mathf.FloorToInt(rt.rect.height);
            _isCompleted = false;

            int totalMaskPixels = _width * _height;

            // 1. Maske Dokusunu Sıfırla
            _maskTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _colors = new Color32[totalMaskPixels];
            Color32 clearColor = new Color32(0, 0, 0, 0);
            for (int i = 0; i < totalMaskPixels; i++) _colors[i] = clearColor;
            _maskTexture.SetPixels32(_colors);
            _maskTexture.Apply();
            maskImage.texture = _maskTexture;

            // 2. Çizgileri Ekran Boyutuna Birebir Haritalandır
            _isLineMap = new bool[totalMaskPixels];
            _paintedLineMap = new bool[totalMaskPixels];
            _totalLinePixels = 0;
            _paintedLinePixels = 0;

            if (targetLineTexture != null)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        // UV koordinatları üzerinden hedef görselden renk oku
                        float u = (float)x / _width;
                        float v = (float)y / _height;
                        Color pixel = targetLineTexture.GetPixelBilinear(u, v);

                        // Alfa > 0.2 ve siyah/koyu çizgi tespiti
                        if (pixel.a > 0.2f && (pixel.r < 0.8f || pixel.g < 0.8f || pixel.b < 0.8f))
                        {
                            int index = y * _width + x;
                            _isLineMap[index] = true;
                            _totalLinePixels++;
                        }
                    }
                }
                Debug.Log($"[DrawReveal] Haritalanan ekran çizgi pikseli: {_totalLinePixels}");
            }

            OnProgressChanged?.Invoke(0f);
        }

        public void OnPointerDown(PointerEventData eventData) => DrawAt(eventData);
        public void OnDrag(PointerEventData eventData) => DrawAt(eventData);

        private void DrawAt(PointerEventData eventData)
        {
            if (_isCompleted) return;

            RectTransform rt = maskImage.rectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                int px = Mathf.FloorToInt(localPoint.x + (rt.rect.width / 2f));
                int py = Mathf.FloorToInt(localPoint.y + (rt.rect.height / 2f));
                PaintCircle(px, py, brushRadius);
            }
        }

        private void PaintCircle(int x, int y, int radius)
        {
            int startX = Mathf.Max(0, x - radius);
            int endX = Mathf.Min(_width, x + radius);
            int startY = Mathf.Max(0, y - radius);
            int endY = Mathf.Min(_height, y + radius);

            bool modified = false;
            Color32 whiteColor = new Color32(255, 255, 255, 255);

            for (int i = startX; i < endX; i++)
            {
                for (int j = startY; j < endY; j++)
                {
                    if ((i - x) * (i - x) + (j - y) * (j - y) <= radius * radius)
                    {
                        int index = j * _width + i;

                        // Maskeyi aç
                        if (_colors[index].a == 0)
                        {
                            _colors[index] = whiteColor;
                            modified = true;
                        }

                        // Eğer burası hedef çizgiyse ve daha önce sayılmadıysa skoru artır
                        if (_isLineMap[index] && !_paintedLineMap[index])
                        {
                            _paintedLineMap[index] = true;
                            _paintedLinePixels++;
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                _maskTexture.SetPixels32(_colors);
                _maskTexture.Apply();

                float rawProgress = _totalLinePixels > 0 ? (float)_paintedLinePixels / _totalLinePixels : 0f;
                OnProgressChanged?.Invoke(rawProgress);
                CheckCompletion(rawProgress);
            }
        }

        private void CheckCompletion(float progress)
        {
            if (_totalLinePixels == 0) return;

            if (progress >= completionThreshold)
            {
                _isCompleted = true;
                Debug.Log("<color=green>[DrawReveal] Çizim Tamamlandı!</color>");
                OnDrawingCompleted?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_maskTexture != null)
            {
                Destroy(_maskTexture);
                _maskTexture = null;
            }
        }
    }
}