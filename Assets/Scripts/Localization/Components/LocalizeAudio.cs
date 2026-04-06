using UnityEngine;

namespace Runtime.Localization
{
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Localization/Localize Audio")]
    public class LocalizeAudio : LocalizeBase
    {
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public override void OnLocalize()
        {
            if (_audioSource == null) return;
            
            AudioClip clip = LocalizationManager.GetAsset<AudioClip>(Key);
            if (clip != null)
            {
                bool wasPlaying = _audioSource.isPlaying;
                _audioSource.clip = clip;
                
                if (wasPlaying)
                {
                    _audioSource.Play();
                }
            }
        }
    }
}
