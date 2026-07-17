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

        public void ApplySprite(Sprite sprite)
        {
            _image.sprite = sprite;
            _image.enabled = sprite != null;

            // if (sprite != null)
            //     _image.SetNativeSize(); // sprite'ın orijinal piksel boyutunu korur
        }
    }
}