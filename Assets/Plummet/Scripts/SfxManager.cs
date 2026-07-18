using System.Collections;
using UnityEngine;

namespace Plummet
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        [Header("Version 3 Clips")]
        [SerializeField] private AudioClip buttonTapClip;
        [SerializeField] private AudioClip dropWhooshClip;
        [SerializeField] private AudioClip wallThudClip;
        [SerializeField] private AudioClip rescueBoingClip;

        [Header("Music")]
        [SerializeField] private AudioClip musicLoopClip;
        [SerializeField] private AudioSource musicSource;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float buttonVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float dropVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float wallVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float rescueVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ConfigureSource();
            PlayMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayButtonTap()
        {
            Play(buttonTapClip, buttonVolume);
        }

        public void PlayDropWhoosh()
        {
            Play(dropWhooshClip, dropVolume);
        }

        public void PlayWallThud()
        {
            Play(wallThudClip, wallVolume);
        }

        public void PlayRescueBoing(float delay = 0f)
        {
            if (delay <= 0f)
            {
                Play(rescueBoingClip, rescueVolume);
                return;
            }

            StartCoroutine(PlayDelayed(rescueBoingClip, rescueVolume, delay));
        }

        public void PlayMusic()
        {
            ConfigureSource();
            if (musicLoopClip == null || musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = musicLoopClip;
            musicSource.Play();
        }


        private IEnumerator PlayDelayed(AudioClip clip, float volume, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Play(clip, volume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            ConfigureSource();
            source.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volume));
        }

        private void ConfigureSource()
        {
            if (source == null || source == musicSource)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                foreach (AudioSource candidate in sources)
                {
                    if (candidate != musicSource)
                    {
                        source = candidate;
                        break;
                    }
                }

                if (source == null || source == musicSource)
                {
                    source = gameObject.AddComponent<AudioSource>();
                }
            }

            if (musicSource == null || musicSource == source)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                foreach (AudioSource candidate in sources)
                {
                    if (candidate != source)
                    {
                        musicSource = candidate;
                        break;
                    }
                }

                if (musicSource == null || musicSource == source)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = musicLoopClip;
            musicSource.volume = Mathf.Clamp01(masterVolume * musicVolume);
        }
    }
}
