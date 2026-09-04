using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Freeline
{
    public class WebtoonChapterSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject redDotNotification;
        [SerializeField] private Button button;

        private WebtoonChapterData _chapterData;
        private System.Action<WebtoonChapterData> _onClickCallback;

        public void Setup(WebtoonChapterData data, bool isPublished, System.Action<WebtoonChapterData> onClick)
        {
            _chapterData = data;
            _onClickCallback = onClick;
            
            titleText.text = data.chapterName; // Örn: "1. BÖLÜM"

            if (isPublished)
            {
                lockIcon.SetActive(false);
                button.interactable = true;
                // WebtoonManager'a sor: Bu bölümde okunmamış yorum var mı?
                redDotNotification.SetActive(WebtoonManager.Instance.HasUnreadComments(data.chapterIndex));
            }
            else
            {
                lockIcon.SetActive(true);
                button.interactable = false;
                redDotNotification.SetActive(false);
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClickCallback?.Invoke(_chapterData));
        }
    }
}