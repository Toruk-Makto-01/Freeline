using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "JobConfig", menuName = "Freeline/Config/Job Config")]
    public class JobConfig : ScriptableObject
    {
        [Tooltip("How many jobs are shown on the board at once.")]
        [Range(1, 6)]
        public int boardSize = 3;

        [Tooltip("How many times the player can re-roll the board per day.")]
        [Range(0, 10)]
        public int maxDailyRefreshes = 3;

        [Tooltip("Tip payout as a fraction of basePayout when earlyFinishBonusActive is true.")]
        [Range(0f, 1f)]
        public float tipBonusMultiplier = 0.2f;
    }
}
