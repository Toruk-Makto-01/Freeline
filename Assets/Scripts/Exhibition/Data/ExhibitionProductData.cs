using UnityEngine;

namespace Freeline
{
    [CreateAssetMenu(fileName = "ExhibitionProduct", menuName = "Freeline/Exhibition Product")]
    public class ExhibitionProductData : ScriptableObject
    {
        public string productName;
        public Sprite icon;
        public int basePrice;
        public float energyCost;
        public float productionHours;

        [Header("Mini Oyun Zorluk Ayarlari")]
        [Tooltip("Kolay, Orta, Zor veya Uzman")]
        public string difficultyLabel = "Kolay";
        [Tooltip("Slide bar ibresinin hiz çarpani (örn: 1.0, 1.5, 2.0)")]
        public float slideSpeedMultiplier = 1f;
    }
}