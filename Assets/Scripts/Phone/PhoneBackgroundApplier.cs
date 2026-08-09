using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    // Bu script atandığı objede kesinlikle bir Image componenti olmasını zorunlu kılar
    [RequireComponent(typeof(Image))]
    public class PhoneBackgroundApplier : MonoBehaviour
    {
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            // Obje aktifleştiğinde duyurulara abone ol
            BackgroundPanel.OnBackgroundChanged += ApplyBackground;
        }

        private void OnDisable()
        {
            // Obje kapandığında abonelikten çık (Hafıza sızıntısını önler)
            BackgroundPanel.OnBackgroundChanged -= ApplyBackground;
        }

        private void ApplyBackground(Sprite newBg)
        {
            if (_image != null && newBg != null)
            {
                _image.sprite = newBg;
            }
        }
    }
}