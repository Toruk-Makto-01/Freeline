using UnityEngine;

namespace Freeline
{
    public enum ExhibitionProductType
    {
        Poster,
        Painting,
        MangaPage,
        OilPainting
    }

    [CreateAssetMenu(fileName = "Exhibition Product", menuName = "Freeline/Exhibition/Product")]
    public class ExhibitionProductData : ScriptableObject
    {
        [Header("General")]
        public string productName;
        public ExhibitionProductType productType;
        public Sprite icon;

        [Header("Production")]
        public int energyCost;
        public int productionHours;

        [Header("Exhibition")]
        public int basePrice;

        [Header("Unlock")]
        public int unlockDay;
    }
}