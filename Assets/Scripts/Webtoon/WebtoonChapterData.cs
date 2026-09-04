using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "Chapter_", menuName = "Freeline/Webtoon Chapter Data")]
    public class WebtoonChapterData : ScriptableObject
    {
        [Header("Bölüm Kimliği")]
        public int chapterIndex; // 1, 2, 3...
        public string chapterName; // "1. Bölüm"

        [Header("Çizim Görselleri")]
        public Texture2D sketchTexture; // Siyah-Beyaz eskiz görseli
        public Texture2D colorTexture;  // Tamamen boyanmış renkli görsel

        [Header("Kilit Şartları")]
        public int requiredFollowers; // Örn: 5. bölüm için 15000 takipçi
        public int requiredEquipmentLevel; // Örn: 10. bölüm için Seviye 3 Çizim Masası

        [Header("Ekonomi Ödülleri")]
        public int baseFollowerGain; // Bu bölüm çizildiğinde kazanılacak takipçi
    }
}