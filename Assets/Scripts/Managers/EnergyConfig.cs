using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "EnergyConfig", menuName = "Freeline/Config/Energy Config")]
    public class EnergyConfig : ScriptableObject
    {
        [Tooltip("Maximum energy value.")]
        [Range(1f, 200f)]
        public float maxEnergy = 100f;

        [Tooltip("Energy level at which OnLowEnergy fires.")]
        [Range(1f, 99f)]
        public float energyDepletionWarningThreshold = 30f;

        [Tooltip("In-game hours without food before the hunger penalty applies to energy restoration.")]
        [Range(1f, 12f)]
        public float hungerThresholdHours = 6f;

        [Tooltip("Multiplier applied to RestoreEnergy amounts when the player is hungry (< 1 = penalty).")]
        [Range(0.1f, 1f)]
        public float hungerPenaltyMultiplier = 0.5f;
    }
}
