using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "WebtoonConfig", menuName = "Freeline/Config/Webtoon Config")]
    public class WebtoonConfig : ScriptableObject
    {
        [Tooltip("Game-hours consumed by a normal chapter production session.")]
        [Range(1f, 12f)]
        public float chapterProductionHours = 4f;

        [Tooltip("Energy consumed per chapter (normal quality).")]
        [Range(1f, 100f)]
        public float chapterEnergyCost = 30f;

        [Tooltip("Base follower gain when a chapter is published at normal quality.")]
        public float baseFollowerGainPerChapter = 50f;

        [Tooltip("Fraction of current followers lost per missed day after the grace period.")]
        [Range(0f, 0.5f)]
        public float followerDecayPerMissedDay = 0.02f;

        [Tooltip("Days since last chapter before follower decay starts.")]
        [Range(0f, 14f)]
        public float decayGracePeriodDays = 3f;

        [Tooltip("Coins earned per follower per day.")]
        public float dailyIncomePerFollower = 0.01f;

        [Tooltip("Probability per chapter of triggering a viral boost.")]
        [Range(0f, 1f)]
        public float viralChanceBase = 0.05f;

        [Tooltip("Follower-gain multiplier when a chapter goes viral.")]
        public float viralMultiplier = 5f;

        [Tooltip("Follower-gain multiplier applied when the player spends 2x production hours (high quality).")]
        public float qualityTimeMultiplier = 1.5f;
    }
}
