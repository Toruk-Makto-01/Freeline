using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Animations;

namespace Freeline
{
    public class DetailsPanel : MonoBehaviour
    {
        [Header("UI Referansleri")]
        [SerializeField] private Transform activeBuffsContainer;
        [SerializeField] private Transform permanentStatsContainer;
        [SerializeField] private GameObject statTextPrefab;
        [SerializeField] private Button closeButton;

        [Header("Veri Referanslari")]
        [Tooltip("Dekorasyon isimlerini ve ozelliklerini bulmak icin katalog referansi")]
        [SerializeField] private DecorationCatalog decorationCatalog;

        [Header("Buff Arka Plan Görselleri")]
        [SerializeField] private Sprite activeBuffSprite; // Aktif (Süreli) buff görseli
        [SerializeField] private Sprite passiveBuffSprite; // Pasif (Kalıcı/Dekorasyon) buff görseli

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        // Panel her acildiginda verileri tazelemek icin cagiracagiz
        public void OpenPanel()
        {
            gameObject.SetActive(true);
            RefreshData();
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        private void RefreshData()
        {
            ClearContainer(activeBuffsContainer);
            ClearContainer(permanentStatsContainer);

            PopulateActiveBuffs();
            PopulatePermanentStats();
        }

        // Mevcut yazıları temizler (üst üste binmemesi için)
        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        // 1. KISIM: SÜRELİ GÜÇLENDŞİRMELER (Aktif Buflar)
        private void PopulateActiveBuffs()
        {
            var activeBuffs = GameManager.Instance.EnergyManager.GetActiveBuffs();

            if (activeBuffs == null || activeBuffs.Count == 0)
            {
                CreateTextItem(activeBuffsContainer, "<i>Şu an aktif bir etkiniz bulunmuyor.<i>", true);
                return;
            }

            foreach (var buff in activeBuffs)
            {
                // Bitiş süresini hesapla
                if (DateTime.TryParse(buff.endTimeString, out DateTime endTime))
                {
                    TimeSpan timeLeft = endTime - DateTime.Now;
                    string timeString = $"{timeLeft.Minutes}dk {timeLeft.Seconds}sn";

                    // Buff tipine göre metin oluştur
                    string buffText = "";
                    if (buff.effectType == ConsumableEffectType.EnergyCostReduction)
                    {
                        buffText = $"Kahve Etkisi : <color=#000000>Enerji Tasarrufu</color> ({timeString})";
                    }
                    else if (buff.effectType == ConsumableEffectType.EnergyRegenOverTime)
                    {
                        buffText = $"Dinçlik : <color=#000000>Enerji Yenileme</color> ({timeString})";
                    }
                    else
                    {
                        buffText = $"{buff.effectType} : Etki Değeri {buff.effectValue} ({timeString})";
                    }

                    CreateTextItem(activeBuffsContainer, buffText, true);
                }
            }
        }

        // 2. KISIM: KALICI ÖZELLİKLER (Dekorasyon)
        private void PopulatePermanentStats()
        {
            var equippedItem = GameManager.Instance.SaveManager.CurrentData.equippedDecorations;

            if (equippedItem == null || equippedItem.Count == 0)
            {
                CreateTextItem(permanentStatsContainer, "<i>Henüz bir eşya yerleştirmediniz.</i>", false);
                return;
            }

            bool hasAnyStat = false;

            foreach (var equipped in equippedItem)
            {
                // Kataloga gidip eşyanın detaylarını buluyoruz
                var itemData = decorationCatalog.GetItemById(equipped.itemId);

                if (itemData != null)
                {
                    // NOT: Eğer DecorationItemData içine henüz "StstDescription" (Örn: "Uyku Verimi +%10")
                    // diye vir string değişken eklemediysek, ileride ekleyebiliriz.
                    // Şimdilik test için statik bir yazı basıyoruz.

                    // string statYazisi = string.IsNullOrEmpty(itemData.statDescription) ? "Sadece Dekoratif" : itemData.statDecoration;
                    string statYazisi = "<color=#000000>Ö<ellik Testi +%10</color>"; // Geçici test yazisi

                    CreateTextItem(permanentStatsContainer, $"{itemData.displayName} : {statYazisi}", false);
                    hasAnyStat = true;
                }
            }

            if (!hasAnyStat)
            {
                CreateTextItem(permanentStatsContainer, "<i>Eşyalarınızın ek bir özelliği yok.</i>", false);
            }
        }

        // YENİ KOD: Metoda 'isActiveBuff' (aktif buff mı?) adında bir parametre (true/false) ekledik.
        private void CreateTextItem(Transform parent, string content, bool isActiveBuff)
        {
            // Prefab'ı üret (En dışta Image, içinde Text var)
            GameObject newObj = Instantiate(statTextPrefab, parent);

            // 1. Yazıyı bul ve içeriğini ata
            TextMeshProUGUI textComp = newObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = content;
            }

            // 2. Arka plan görselini (Image) bul ve duruma göre değiştir
            Image bgImage = newObj.GetComponent<Image>();
            if (bgImage != null)
            {
                // Eğer isActiveBuff true ise activeBuffSprite'ı, false ise passiveBuffSprite'ı kullan
                bgImage.sprite = isActiveBuff ? activeBuffSprite : passiveBuffSprite;
            }
        }
    }
}