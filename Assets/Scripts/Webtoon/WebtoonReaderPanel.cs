using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Freeline
{
    public class WebtoonReaderPanel : MonoBehaviour
    {
        [Header("Okuma Arayüzü")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private RawImage artworkImage;
        [SerializeField] private Button closeButton;

        [Header("Zoom Ayarları")]
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 4f;
        [SerializeField] private float zoomSpeedMouse = 0.1f;
        [SerializeField] private float zoomSpeedTouch = 0.005f;

        [Header("Yorumlar Arayüzü")]
        [SerializeField] private GameObject commentsPanel;
        [SerializeField] private Button openCommentsButton;
        [SerializeField] private Button closeCommentsButton;

        private float _currentZoom = 1f;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseReader);
            if (openCommentsButton != null) openCommentsButton.onClick.AddListener(() => commentsPanel.SetActive(true));
            if (closeCommentsButton != null) closeCommentsButton.onClick.AddListener(() => commentsPanel.SetActive(false));
        }

        public void OpenChapter(WebtoonChapterData chapter)
        {
            artworkImage.texture = chapter.colorTexture;

            // Görseli orijinal boyut oranına göre otomatik sığdır
            AspectRatioFitter fitter = artworkImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = artworkImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = (float)chapter.colorTexture.width / chapter.colorTexture.height;

            // Zoom ve Scroll pozisyonunu sıfırla
            _currentZoom = 1f;
            contentRect.localScale = Vector3.one;
            
            // Görselin en üstünden başlat
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0.5f;

            commentsPanel.SetActive(false);
        }

        private void CloseReader()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            HandleZoom();
        }

        private void HandleZoom()
        {
            float zoomDelta = 0f;

            // 1. PC (Fare Tekerleği) ile Zoom
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0) zoomDelta = zoomSpeedMouse;
                else if (scroll < 0) zoomDelta = -zoomSpeedMouse;
            }

            // 2. Mobil (Pinch-to-Zoom - İki Parmak)
            if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
            {
                var touch0 = Touchscreen.current.touches[0];
                var touch1 = Touchscreen.current.touches[1];

                if (touch0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved || 
                    touch1.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 t0Pos = touch0.position.ReadValue();
                    Vector2 t1Pos = touch1.position.ReadValue();
                    
                    Vector2 t0Prev = t0Pos - touch0.delta.ReadValue();
                    Vector2 t1Prev = t1Pos - touch1.delta.ReadValue();

                    float prevDist = Vector2.Distance(t0Prev, t1Prev);
                    float currentDist = Vector2.Distance(t0Pos, t1Pos);

                    zoomDelta = (currentDist - prevDist) * zoomSpeedTouch;
                }
            }

            if (zoomDelta != 0f)
            {
                _currentZoom = Mathf.Clamp(_currentZoom + zoomDelta, minZoom, maxZoom);
                contentRect.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            }
        }
    }
}