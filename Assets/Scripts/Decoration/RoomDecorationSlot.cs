using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    [RequireComponent(typeof(Image))]
    public class RoomDecorationSlot : MonoBehaviour
    {
        [SerializeField] private DecorationCategory category;
        private Image _image;

        public DecorationCategory Category => category;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            // Unity'nin önerdiği en güncel ve en hızlı tarama komutu: FindAnyObjectByType
            var manager = Object.FindAnyObjectByType<RoomDecorationManager>();
            
            if (manager != null)
            {
                manager.RegisterSlot(this);
            }
            else
            {
                Debug.LogWarning($"{category} slotu kaydedilemedi! RoomDecorationManager bulunamadi.");
            }
        }

        public void ApplySprite(Sprite sprite)
        {
            _image.sprite = sprite;
            _image.enabled = sprite != null;
        }
    }
}