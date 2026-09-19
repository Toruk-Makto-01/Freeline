using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Freeline
{
    public class ExhibitionManager : MonoBehaviour
    {
        public event Action OnExhibitionDay;
        public bool IsExhibitionDay { get; private set; }

        private void Start()
        {
            GameManager.Instance.TimeManager.OnNewDayStarted += OnNewDayStarted;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TimeManager.OnNewDayStarted -= OnNewDayStarted;
        }

        private void OnNewDayStarted(int day)
        {
            // Eğer bugün 7'nin katı değilse sergi gününü kesinlikle iptal et (Oyuncu atlamış demektir)
            if (day % 7 != 0)
            {
                IsExhibitionDay = false;
                return;
            }

            IsExhibitionDay = true;
            OnExhibitionDay?.Invoke();
        }

        public void StartExhibition()
        {
            IsExhibitionDay = false;
        }

        public void SkipExhibition()
        {
            IsExhibitionDay = false;
        }

        public void AddProductToStock(ExhibitionProductData product)
        {
            var stock = GameManager.Instance.SaveManager.CurrentData.exhibitionStock;

            ExhibitionStockItem item = stock.Find(x => x.product == product);

            if (item != null)
            {
                item.quantity++;
            }
            else
            {
                stock.Add(new ExhibitionStockItem
                {
                    product = product,
                    quantity = 1
                });
            }
        }

        public int GetDaysUntilNextExhibition()
        {
            // Zaman yöneticisinden mevcut günü al
            int currentDay = GameManager.Instance.TimeManager.CurrentDay;
            int remaining = 7 - (currentDay % 7);
            return remaining == 7 ? 0 : remaining; // 0 ise bugün sergi günü demektir
        }
    }
}