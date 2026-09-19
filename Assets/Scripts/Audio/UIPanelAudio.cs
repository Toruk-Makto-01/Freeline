using UnityEngine;

namespace Freeline
{
    public class UIPanelAudio : MonoBehaviour
    {
        [SerializeField] private bool playOpenSound = true;
        [SerializeField] private bool playCloseSound = true;
        [SerializeField] private bool affectMusicVolume = true; // Bu panel açıkken müzik kısılsın mı?

        private bool _isInitialized = false;

        private void Start()
        {
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;

            if (AudioManager.Instance != null)
            {
                if (playOpenSound) AudioManager.Instance.PlayPanelOpen();
                if (affectMusicVolume) AudioManager.Instance.RegisterPanelOpen();
            }
        }

        private void OnDisable()
        {
            if (!_isInitialized) return;

            if (AudioManager.Instance != null)
            {
                if (playCloseSound) AudioManager.Instance.PlayPanelClose();
                if (affectMusicVolume) AudioManager.Instance.RegisterPanelClose();
            }
        }
    }
}