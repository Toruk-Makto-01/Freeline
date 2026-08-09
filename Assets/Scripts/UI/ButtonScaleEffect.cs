using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Freeline
{
    // Butonun basılma ve bırakılma olaylarını dinlemek için arayüzleri (Interface) ekliyoruz
    public class ButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Ayarlar")]
        [SerializeField] private float shrinkScale = 0.9f; // Butonun ne kadar küçüleceği (%90 boyut)
        [SerializeField] private float animationSpeed = 20f; // Küçülme/Büyüme hızı

        private Vector3 _originalScale;
        private Coroutine _scaleCoroutine;

        void Start()
        {
            // Oyun başladığında butonun orijinal boyutunu hafızaya al
            _originalScale = transform.localScale;
        }

        // Ekrana/Butona dokunulduğu an çalışır
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleTo(_originalScale * shrinkScale));
        }

        // Parmak ekrandan/butondan çekildiği an çalışır
        public void OnPointerUp(PointerEventData eventData)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleTo(_originalScale));
        }

        // Yumuşak (Smooth) bir geçiş için Coroutine kullanıyoruz
        private IEnumerator ScaleTo(Vector3 targetScale)
        {
            while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
            {
                // Mevcut boyuttan hedef boyuta yumuşakça geçiş yap
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
                yield return null;
            }
            
            // Tam olarak hedef boyuta eşitle
            transform.localScale = targetScale;
        }
    }
}