using System;
using UnityEngine;

namespace Freeline
{
    // Runs before SaveManager (order 0) so decay and passive income are written to
    // SaveData before it is flushed to disk on OnNewDayStarted.
    [DefaultExecutionOrder(-5)]
    public class WebtoonManager : MonoBehaviour
    {
        [SerializeField] private WebtoonConfig config;

        // Additive multiplier bonus from equipment/decorations (e.g. 0.2 = +20% follower gain).
        // Set by the equipment system when better drawing tools are equipped.
        public float equipmentQualityBonus;

        // (chapterNumber, followerDelta, wasViral)
        public event Action<int, float, bool> OnChapterPublished;

        // Amount of passive income earned at the start of each new day.
        public event Action<float> OnPassiveIncomeEarned;

        // (currentFollowers, delta) — positive on gain, negative on decay.
        public event Action<float, float> OnFollowersChanged;

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

        // Player initiates a chapter production session.
        // highQuality = true → spends 2× hours, applies qualityTimeMultiplier to follower gain.
        // Returns false if the player does not have enough energy.
        public bool ProduceChapter(bool highQuality = false)
        {
            EnergyManager em = GameManager.Instance.EnergyManager;
            if (em.CurrentEnergy < config.chapterEnergyCost) return false;

            float hours = config.chapterProductionHours * (highQuality ? 2f : 1f);

            // AdvanceTime may trigger a new day synchronously (decay + income applied).
            // PublishChapter then resets daysSinceLastChapter afterwards — correct order.
            GameManager.Instance.TimeManager.AdvanceTime(hours);
            em.ConsumeEnergy(config.chapterEnergyCost);

            PublishChapter(highQuality);
            return true;
        }

        // Calculates follower change, updates WebtoonData, and fires events.
        // Exposed publicly for scripted events or debug use.
        public void PublishChapter(bool highQuality = false)
        {
            bool wasViral = UnityEngine.Random.value < config.viralChanceBase;

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

        // Current expected daily passive income based on today's follower count.
        public float GetDailyPassiveIncome() => Webtoon.followers * config.dailyIncomePerFollower;

        private void HandleNewDay(int day)
        {
            WebtoonData wt = Webtoon;
            wt.daysSinceLastChapter += 1f;

            ApplyFollowerDecay(wt);

            float income = wt.followers * config.dailyIncomePerFollower;
            if (income > 0f)
            {
                SaveData save = GameManager.Instance.SaveManager.CurrentData;
                save.currentCoins  += income;
                wt.lifetimeEarnings += income;
                OnPassiveIncomeEarned?.Invoke(income);
            }
        }

        private void ApplyFollowerDecay(WebtoonData wt)
        {
            if (wt.daysSinceLastChapter <= config.decayGracePeriodDays) return;
            if (wt.followers <= 0f) return;

            float decay          = wt.followers * config.followerDecayPerMissedDay;
            float previousCount  = wt.followers;
            wt.followers         = Mathf.Max(0f, wt.followers - decay);

            float delta = wt.followers - previousCount; // negative
            OnFollowersChanged?.Invoke(wt.followers, delta);
        }
    }
}
