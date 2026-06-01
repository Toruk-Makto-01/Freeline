using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "TimeConfig", menuName = "Freeline/Config/Time Config")]
    public class TimeConfig : ScriptableObject
    {
        [Tooltip("Hour the day begins after sleeping (24-hour clock).")]
        [Range(0f, 12f)]
        public float startHour = 9f;

        [Tooltip("Earliest hour the player can manually sleep.")]
        [Range(18f, 23f)]
        public float sleepWindowStart = 21f;

        [Tooltip("Hour at which auto-sleep triggers if the player has not slept.")]
        [Range(23f, 24f)]
        public float dayEndHour = 24f;
    }
}
