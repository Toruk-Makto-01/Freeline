using UnityEngine;

namespace Freeline
{
    public enum ProductType { Poster, Tablo, Manga }

    [CreateAssetMenu(fileName = "ExhibitionProductData", menuName = "Freeline/Exhibition Product")]
    public class ExhibitionProductData : ScriptableObject
    {
        public string      productName;
        public ProductType productType;
        public float       productionHours;
        public float       energyCost;
        public int         basePrice;
    }
}
