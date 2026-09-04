using UnityEngine;
using System.Collections.Generic;

namespace Freeline
{
    public class WebtoonManager : MonoBehaviour
    {
        public static WebtoonManager Instance { get; private set; }

        public int TotalFollowers { get; private set; }
        public int PublishedChapterCount { get; private set; } // Bugüne kadar çizilen bölüm sayısı
        
        // Sadece okunmamış yorumu olan BÖLÜM İNDEKSLERİNİ tutar
        private HashSet<int> _chaptersWithUnreadComments = new HashSet<int>(); 

        public int UnreadCommentCount => _chaptersWithUnreadComments.Count;

        public float DailyIncome => TotalFollowers * 0.05f; 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void PublishChapter(WebtoonChapterData chapter)
        {
            TotalFollowers += chapter.baseFollowerGain;
            PublishedChapterCount++;
            
            // Yeni bölüm yayınlandığında o bölüme hemen sahte yorumlar/kırmızı nokta ekle
            _chaptersWithUnreadComments.Add(chapter.chapterIndex);
        }

        public bool HasUnreadComments(int chapterIndex)
        {
            return _chaptersWithUnreadComments.Contains(chapterIndex);
        }

        public void MarkChapterCommentsAsRead(int chapterIndex)
        {
            if (_chaptersWithUnreadComments.Contains(chapterIndex))
            {
                _chaptersWithUnreadComments.Remove(chapterIndex);
            }
        }

        public bool CanUnlockChapter(WebtoonChapterData chapter)
        {
            return TotalFollowers >= chapter.requiredFollowers;
        }
    }
}