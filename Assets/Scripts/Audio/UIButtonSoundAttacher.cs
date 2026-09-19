using UnityEngine;
using UnityEngine.UI;

namespace Freeline
{
    public class UIButtonSoundAttacher : MonoBehaviour
    {
        private void Awake()
        {
            AttachSounds();
        }

        private void OnEnable()
        {
            // Panel kapalıyken sonradan açıldığında da butonları yakalasın
            AttachSounds();
        }

        public void AttachSounds()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (var btn in buttons)
            {
                if (btn == null) continue;

                // Sessiz buton kontrolü (varsa)
                //if (btn.GetComponent<SilentButton>() != null) continue;

                // Dinleyiciyi temizleyip tek sefer ekle
                btn.onClick.RemoveListener(PlaySound);
                btn.onClick.AddListener(PlaySound);
            }
        }

        private void PlaySound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}