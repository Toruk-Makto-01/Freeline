using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Freeline
{
    public class WebtoonPanel : MonoBehaviour
    {
        [Header("Üst Bilgiler")]
        [SerializeField] private TextMeshProUGUI followerText;
        [SerializeField] private Button drawChapterButton;
        [SerializeField] private TextMeshProUGUI drawButtonText;
        [SerializeField] private Button closeButton; // Kapatma butonu eklendi

        [Header("Bölüm Listesi (Scroll)")]
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private WebtoonChapterSlot slotPrefab;

        [Header("Tüm Bölümler (Sırasıyla Sürükle)")]
        [SerializeField] private List<WebtoonChapterData> allChapters;

        [Header("Çizim Mini Oyunu")]
        [SerializeField] private WebtoonDrawPanel drawPanel;

        [Header("Okuma Paneli")]
        [SerializeField] private WebtoonReaderPanel readerPanel;

        private void Awake()
        {
            // Kapatma butonu bağlantısı
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePanel);
            }

            // Bölüm Çiz butonu bağlantısı
            if (drawChapterButton != null)
            {
                drawChapterButton.onClick.RemoveAllListeners();
                drawChapterButton.onClick.AddListener(OnDrawButtonClicked);
            }
        }

        private void OnEnable() => RefreshUI();

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        public void RefreshUI()
        {
            followerText.text = WebtoonManager.Instance.TotalFollowers.ToString("N0");

            int publishedCount = WebtoonManager.Instance.PublishedChapterCount;
            WebtoonChapterData nextChapter = null;

            if (publishedCount < allChapters.Count)
                nextChapter = allChapters[publishedCount];

            if (nextChapter != null)
            {
                bool canUnlock = WebtoonManager.Instance.CanUnlockChapter(nextChapter);
                drawChapterButton.interactable = canUnlock;
                drawButtonText.text = canUnlock ? "BÖLÜM ÇİZ" : $"KİLİTLİ\n({nextChapter.requiredFollowers} Takipçi)";
            }
            else
            {
                drawChapterButton.interactable = false;
                drawButtonText.text = "TÜM BÖLÜMLER BİTTİ";
            }

            foreach (Transform child in contentParent) Destroy(child.gameObject);

            for (int i = 0; i < allChapters.Count; i++)
            {
                var chapter = allChapters[i];
                bool isPublished = i < publishedCount;

                var slot = Instantiate(slotPrefab, contentParent);
                slot.Setup(chapter, isPublished, OnChapterClicked);
            }
        }

        private void OnChapterClicked(WebtoonChapterData chapter)
        {
            Debug.Log($"[Webtoon] {chapter.chapterName} okuma/yorum paneline giriliyor...");
            WebtoonManager.Instance.MarkChapterCommentsAsRead(chapter.chapterIndex);
            RefreshUI();

            // Okuma panelini aç
            if (readerPanel != null)
            {
                readerPanel.gameObject.SetActive(true);
                readerPanel.OpenChapter(chapter);
            }
        }

        public void OnDrawButtonClicked()
        {
            int publishedCount = WebtoonManager.Instance.PublishedChapterCount;
            if (publishedCount < allChapters.Count)
            {
                WebtoonChapterData nextChapter = allChapters[publishedCount];

                if (drawPanel != null)
                {
                    drawPanel.gameObject.SetActive(true);
                    drawPanel.StartDrawing(nextChapter);
                    gameObject.SetActive(false); // Webtoon listesini gizle
                }
            }
        }
    }
}