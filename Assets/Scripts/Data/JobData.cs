using UnityEngine;

namespace Freeline
{
    public enum JobDifficulty
    {
        Beginner,
        Intermediate,
        Advanced
    }

    [CreateAssetMenu(fileName = "NewJob", menuName = "Freeline/Jobs/Job Data")]
    public class JobData : ScriptableObject
    {
        public string        jobTitle;
        public float         durationHours;
        public float         basePayout;
        public float         energyCost;
        public JobDifficulty difficulty;
        public Sprite        jobIcon;
    }
}