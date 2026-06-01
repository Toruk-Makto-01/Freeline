using UnityEngine;

namespace Freeline
{
    public enum JobDifficulty
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public enum JobType
    {
        SlideBar,
        LineTrace,
        Coloring
    }

    [CreateAssetMenu(fileName = "NewJob", menuName = "Freeline/Jobs/Job Data")]
    public class JobData : ScriptableObject
    {
        public string        jobTitle;
        public string        clientName;
        public float         durationHours;
        public float         basePayout;
        public float         energyCost;
        public JobDifficulty difficulty;
        public JobType       jobType;
        public int           requiredLevel;
    }
}
