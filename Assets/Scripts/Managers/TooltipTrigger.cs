using UnityEngine;
using UnityEngine.EventSystems;

namespace Freeline
{
    public class TooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private string buffTitle;
        private string buffDescription;
        private bool isPositiveBuff;

        // --- YENİ EKLENEN KURULUM FONKSİYONU ---
        public void Setup(string title, string desc, bool isPositive)
        {
            buffTitle = title;
            buffDescription = desc;
            isPositiveBuff = isPositive;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TooltipManager.Instance.ShowTooltip(buffTitle, buffDescription, isPositiveBuff, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}