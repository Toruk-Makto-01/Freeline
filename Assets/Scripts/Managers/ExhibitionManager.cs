using System;
using System.Collections.Generic;
using UnityEngine;

namespace Freeline
{
    // Stok kaydı — SaveData.exhibitionStock listesinde tutulur
    [Serializable]
    public class ExhibitionStock
    {
        public ProductType productType;
        public string      productName;
        public int         basePrice;
        public int         quantity;
    }

    public enum NpcPersonality { Generous, Bargainer, Indecisive }

    // Çalışma zamanında üretilen sergi ziyaretçisi — kaydedilmez
    public class ExhibitionNpc
    {
        public NpcPersonality personality;
        public float          offerMultiplier; // Generous: 1.1–1.3, Bargainer: 0.6–0.8, Indecisive: 0.9–1.0
        public bool           hasPurchased;
        public string         greetingLine;

    }

    // Haftalık sergi sistemini yöneten manager
    // Stok, NPC üretimi, satış ve sergi günü tespitini yönetir
    public class ExhibitionManager : MonoBehaviour
    {
        // 7'nin katı günlerde (7, 14, 21...) sergi hazır olduğunda tetiklenir
        public event Action          OnExhibitionDayReady;
        public event Action          OnExhibitionStarted;

        // itemsSold, totalEarned
        public event Action<int, int> OnExhibitionEnded;

        // Bugün sergi günü mü; StartExhibition veya SkipExhibition ile sıfırlanır
        public bool IsExhibitionDay { get; private set; }

        private int _itemsSold;
        private int _totalEarned;

        private static readonly string[] GreetingsGenerous = {
            "Harika bir iş çıkarmışsınız!",
            "Bu fiyata değer, alıyorum!",
            "Eserleriniz çok etkileyici."
        };
        private static readonly string[] GreetingsBargainer = {
            "Biraz indirim yapabilir misiniz?",
            "Bu fiyat biraz yüksek...",
            "En iyisini almak istiyorum ama bütçem kısıtlı."
        };
        private static readonly string[] GreetingsIndecisive = {
            "Hmm... emin değilim.",
            "Belki... bir düşüneyim.",
            "Güzel ama karar veremiyorum."
        };

        void Start()
        {
            GameManager.Instance.TimeManager.OnNewDayStarted += HandleNewDayStarted;
        }

        void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.TimeManager.OnNewDayStarted -= HandleNewDayStarted;
        }

        // Her 7. günde sergi günü bayrağını set eder
        private void HandleNewDayStarted(int day)
        {
            if (day % 7 == 0)
            {
                IsExhibitionDay = true;
                OnExhibitionDayReady?.Invoke();
            }
        }

        // Üretilen ürünü stoğa ekler; aynı ürün zaten varsa miktarı artırır
        public void AddToStock(ExhibitionProductData product)
        {
            var stock    = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;
            var existing = stock.Find(s =>
                s.productName == product.productName &&
                s.productType == product.productType);

            if (existing != null)
            {
                existing.quantity++;
            }
            else
            {
                stock.Add(new ExhibitionStock
                {
                    productType = product.productType,
                    productName = product.productName,
                    basePrice   = product.basePrice,
                    quantity    = 1
                });
            }
        }

        public IReadOnlyList<ExhibitionStock> GetStock()
        {
            return GameManager.Instance.SaveManager.CurrentData.exhibitionStock;
        }

        // Oyuncu sergiyi onayladığında çağrılır; satış sayaçlarını sıfırlar
        public void StartExhibition()
        {
            IsExhibitionDay = false;
            _itemsSold      = 0;
            _totalEarned    = 0;
            OnExhibitionStarted?.Invoke();
        }

        // Oyuncu sergiyi atlarsa bayrak sıfırlanır, sergi işlenmez
        public void SkipExhibition()
        {
            IsExhibitionDay = false;
        }

        // Tüm ziyaretçiler işlendikten sonra ExhibitionPanel tarafından çağrılır
        public void EndExhibition()
        {
            OnExhibitionEnded?.Invoke(_itemsSold, _totalEarned);
        }

        // Ziyaretçi sayısı: clamp(3 + log10(followers+1) + Random(0,2), 3, 12)
        public List<ExhibitionNpc> GenerateVisitors()
        {
            int followers = (int)GameManager.Instance.SaveManager.CurrentData.webtoonData.followers;
            int rawCount  = Mathf.RoundToInt(
                3f + Mathf.Log10(followers + 1) + UnityEngine.Random.Range(0, 3));
            int count     = Mathf.Clamp(rawCount, 3, 12);

            var visitors = new List<ExhibitionNpc>(count);
            for (int i = 0; i < count; i++)
                visitors.Add(CreateNpc());
            return visitors;
        }

        // Satışı işler: pazarlık kabulü, stok güncelleme, kazanç yazma
        // playerBargained → NPC kişiliğine göre kabul şansı; reddedilirse satış iptal
        // playerAccepted  → NPC teklifini olduğu gibi kabul
        public void ProcessSale(ExhibitionStock item, ExhibitionNpc npc,
            bool playerAccepted, bool playerBargained)
        {
            if (npc.hasPurchased) return;

            bool saleHappens;
            if (playerBargained)
            {
                float acceptChance = npc.personality switch
                {
                    NpcPersonality.Generous   => 0.8f,
                    NpcPersonality.Indecisive => 0.5f,
                    NpcPersonality.Bargainer  => 0.2f,
                    _                         => 0.5f
                };
                saleHappens = UnityEngine.Random.value <= acceptChance;
            }
            else
            {
                saleHappens = playerAccepted;
            }

            if (!saleHappens) return;

            int finalPrice = Mathf.RoundToInt(item.basePrice * npc.offerMultiplier);

            item.quantity--;
            if (item.quantity <= 0)
                GameManager.Instance.SaveManager.CurrentData.exhibitionStock.Remove(item);

            GameManager.Instance.SaveManager.CurrentData.currentCoins += finalPrice;
            _itemsSold++;
            _totalEarned += finalPrice;
            npc.hasPurchased = true;
        }

        // Rastgele kişilik, çarpan ve selamlama satırı içeren NPC oluşturur
        private ExhibitionNpc CreateNpc()
        {
            var personality = (NpcPersonality)UnityEngine.Random.Range(0, 3);

            float multiplier = personality switch
            {
                NpcPersonality.Generous   => UnityEngine.Random.Range(1.1f, 1.3f),
                NpcPersonality.Bargainer  => UnityEngine.Random.Range(0.6f, 0.8f),
                NpcPersonality.Indecisive => UnityEngine.Random.Range(0.9f, 1.0f),
                _                         => 1f
            };

            string[] pool = personality switch
            {
                NpcPersonality.Generous  => GreetingsGenerous,
                NpcPersonality.Bargainer => GreetingsBargainer,
                _                        => GreetingsIndecisive
            };

            return new ExhibitionNpc
            {
                personality     = personality,
                offerMultiplier = multiplier,
                hasPurchased    = false,
                greetingLine    = pool[UnityEngine.Random.Range(0, pool.Length)]
            };
        }
    }
}
