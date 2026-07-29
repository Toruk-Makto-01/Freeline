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
            if (day % 7 != 0)
                return;

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
    }
}