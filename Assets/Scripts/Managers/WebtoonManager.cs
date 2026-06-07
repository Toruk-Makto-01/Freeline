using System;
using UnityEngine;

namespace Freeline
{
    /// <summary>
    /// Webtoon bölümü üretimini, takipçi kazanımını, günlük çürümeyi
    /// ve pasif coin gelirini yönetir.
    /// Takipçi sayısı SaveData üzerindeki <see cref="WebtoonData"/> nesnesinde saklanır.
    /// </summary>
    /// <remarks>
    /// DefaultExecutionOrder(-5): SaveManager'dan (order 0) önce çalışır.
    /// OnNewDayStarted tetiklendiğinde çürüme ve pasif gelir hesabı burada yapılır;
    /// ardından SaveManager bu değerleri diske yazar.
    /// </remarks>
    [DefaultExecutionOrder(-5)]
    public class WebtoonManager : MonoBehaviour
    {
        [SerializeField] private WebtoonConfig config;

        /// <summary>
        /// Çizim masası ekipmanından gelen takipçi kazanım bonusu (katkı çarpanı).
        /// Örnek: 0.2 → %20 ek kazanım. Ekipman sistemi tarafından dışarıdan atanır.
        /// </summary>
        public float equipmentQualityBonus;

        /// <summary>
        /// Bir bölüm yayınlandığında tetiklenir.
        /// Parametreler: (bölüm numarası, takipçi artışı, viral mi).
        /// </summary>
        public event Action<int, float, bool> OnChapterPublished;

        /// <summary>
        /// Her yeni günün başında kazanılan pasif gelir miktarıyla tetiklenir.
        /// HUD coin animasyonu için kullanılabilir.
        /// </summary>
        public event Action<float> OnPassiveIncomeEarned;

        /// <summary>
        /// Takipçi sayısı değiştiğinde tetiklenir.
        /// Parametreler: (güncel takipçi sayısı, değişim miktarı — kazanımda pozitif, çürümede negatif).
        /// </summary>
        public event Action<float, float> OnFollowersChanged;

        // SaveData'ya her erişimde property üzerinden ulaşılır; yerel referans saklanmaz.
        // SaveData nesnesi yeniden oluşturulursa eski referans geçersiz kalır.
        private WebtoonData Webtoon => GameManager.Instance.SaveManager.CurrentData.webtoonData;

        void Start()
        {
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDay;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDay;
        }

        /// <summary>
        /// Oyuncunun bir bölüm üretim oturumu başlatmasını işler.
        /// Zaman ilerletir, enerji tüketir ve <see cref="PublishChapter"/>'ı çağırır.
        /// Yüksek kalite modunda iki kat süre harcar ama daha fazla takipçi kazandırır.
        /// </summary>
        /// <param name="highQuality">
        /// <c>true</c> ise 2× saat ve <see cref="WebtoonConfig.qualityTimeMultiplier"/> uygulanır.
        /// </param>
        /// <returns>Enerji yeterliyse <c>true</c>, yetersizse <c>false</c>.</returns>
        public bool ProduceChapter(bool highQuality = false)
        {
            EnergyManager em = GameManager.Instance.EnergyManager;
            if (em.CurrentEnergy < config.chapterEnergyCost) return false;

            float hours = config.chapterProductionHours * (highQuality ? 2f : 1f);

            // AdvanceTime gün sonunu senkron tetikleyebilir; bu durumda çürüme ve pasif gelir
            // HandleNewDay içinde uygulanır. PublishChapter ardından daysSinceLastChapter'ı
            // sıfırlar — doğru sıralama budur.
            GameManager.Instance.TimeManager.AdvanceTime(hours);
            em.ConsumeEnergy(config.chapterEnergyCost);

            PublishChapter(highQuality);
            return true;
        }

        /// <summary>
        /// Takipçi değişimini hesaplar, <see cref="WebtoonData"/>'yı günceller ve event'leri tetikler.
        /// Betiklenmiş olaylar veya hata ayıklama için public olarak erişilebilir.
        /// </summary>
        /// <param name="highQuality">Kalite çarpanının uygulanıp uygulanmayacağı.</param>
        public void PublishChapter(bool highQuality = false)
        {
            bool wasViral = UnityEngine.Random.value < config.viralChanceBase;

            // Tüm çarpanlar sırayla uygulanır: ekipman bonusu → kalite → viral şans.
            float gain = config.baseFollowerGainPerChapter
                * (1f + equipmentQualityBonus)
                * (highQuality ? config.qualityTimeMultiplier : 1f)
                * (wasViral    ? config.viralMultiplier        : 1f);

            WebtoonData wt = Webtoon;
            wt.followers              += gain;
            wt.totalChaptersPublished += 1;
            wt.daysSinceLastChapter    = 0f;

            OnFollowersChanged?.Invoke(wt.followers, gain);
            OnChapterPublished?.Invoke(wt.totalChaptersPublished, gain, wasViral);
        }

        /// <summary>
        /// Güncel takipçi sayısına göre beklenen günlük pasif geliri döndürür.
        /// UI'ın "bugün kazanacaksın" göstergesinde kullanılabilir.
        /// </summary>
        public float GetDailyPassiveIncome() => Webtoon.followers * config.dailyIncomePerFollower;

        // WebtoonPanel'in BÖLÜM ÇİZ düğmesini enerji kontrolüne göre etkinleştirmesi için gerekli.
        public float ChapterEnergyCost => config.chapterEnergyCost;

        /// <summary>
        /// Her yeni gün başında çürümeyi uygular ve pasif geliri hesaplayarak SaveData'ya yazar.
        /// </summary>
        private void HandleNewDay(int day)
        {
            WebtoonData wt = Webtoon;
            wt.daysSinceLastChapter += 1f;

            ApplyFollowerDecay(wt);

            float income = wt.followers * config.dailyIncomePerFollower;
            if (income > 0f)
            {
                SaveData save = GameManager.Instance.SaveManager.CurrentData;
                save.currentCoins   += income;
                wt.lifetimeEarnings += income;
                OnPassiveIncomeEarned?.Invoke(income);
            }
        }

        /// <summary>
        /// Bekleme süresi grace period'u aştıysa takipçi çürümesini uygular.
        /// Çürüme negatif bir delta olarak <see cref="OnFollowersChanged"/>'a bildirilir.
        /// </summary>
        private void ApplyFollowerDecay(WebtoonData wt)
        {
            // Grace period içindeyse ya da hiç takipçi yoksa çürüme uygulanmaz.
            if (wt.daysSinceLastChapter <= config.decayGracePeriodDays) return;
            if (wt.followers <= 0f) return;

            float decay         = wt.followers * config.followerDecayPerMissedDay;
            float previousCount = wt.followers;
            wt.followers        = Mathf.Max(0f, wt.followers - decay);

            float delta = wt.followers - previousCount; // her zaman negatif
            OnFollowersChanged?.Invoke(wt.followers, delta);
        }
    }
}
