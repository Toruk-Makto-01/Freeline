using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Freeline
{
    public class SnapScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("Scroll Settings")]
        public ScrollRect scrollRect;
        public int pageCount = 4; // Number of pages in the scroll view
        public float snapSpeed = 10f;

        [Header("Page Dots Settings")]
        public List<Image> pageDots; // 0 beyaz/yeşil kutucuklar
        public Color activeColoer = new Color(0.2f, 0.8f, 0.4f, 1f); // Yeşil
        public Color inactiveColor = Color.wheat; // Beyaz

        private float[] _pagePositions;
        private int _currentPage = 0;
        private bool _isDragging;
        private float _targetNormalizedPos;

        void Start()
        {
            // Calculate the normalized positions for each page (Örneğin 4 sayfa ise: 0.0, 0.33, 0.66, 1.0)
            _pagePositions = new float[pageCount];
            for (int i = 0; i < pageCount; i++)
            {
                if (pageCount == 1) _pagePositions[i] = 0f; // Avoid division by zero if there's only one page
                else _pagePositions[i] = (float)i / (pageCount - 1);
            }

            _targetNormalizedPos = _pagePositions[0];
            UpdateDots(0);
        }

        void Update()
        {
            // PArmağımızı çektiğimizde hedef sayfaya yumuşakça (Lerp) kay
            if (!_isDragging)
            {
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                    scrollRect.horizontalNormalizedPosition, _targetNormalizedPos, Time.deltaTime * snapSpeed);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            // Bıraktığımızda en yakın sayfayı bul
            float currentPos = scrollRect.horizontalNormalizedPosition;
            float minDistance = float.MaxValue;
            int closestPage = 0;

            for (int i = 0; i < pageCount; i++)
            {
                float distance = Mathf.Abs(_pagePositions[i] - currentPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPage = i;
                }
            }

            _targetNormalizedPos = _pagePositions[closestPage];
            _currentPage = closestPage;
            UpdateDots(_currentPage);
        }

        // Sayfa noktalarının (beyaz/yeşil) renklerini güncelleyen fonksiyon
        private void UpdateDots(int activeIndex)
        {
            if (pageDots == null || pageDots.Count == 0) return;

            for (int i =0; i < pageDots.Count; i++)
            {
                if (pageDots[i] != null)
                {
                    pageDots[i].color = (i == activeIndex) ? activeColoer : inactiveColor;
                }
            }
        }
    }
}
