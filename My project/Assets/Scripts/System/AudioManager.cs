using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource typingSource;

        [Header("Clips")]
        [SerializeField] private List<AudioClip> typingClips = new List<AudioClip>();

        private readonly Dictionary<string, AudioClip> nameToClip = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            nameToClip.Clear();
            foreach (AudioClip clip in typingClips)
            {
                if (clip == null) continue;
                if (!nameToClip.ContainsKey(clip.name))
                {
                    nameToClip.Add(clip.name, clip);
                }
            }

            EnsureSources();
        }

        private void EnsureSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            if (typingSource == null)
            {
                typingSource = gameObject.AddComponent<AudioSource>();
                typingSource.playOnAwake = false;
            }
        }

        public void PrepareTypingClip(string clipName)
        {
            if (nameToClip.TryGetValue(clipName, out AudioClip clip))
            {
                typingSource.clip = clip;
            }
        }

        public void TickType()
        {
            if (typingSource != null && typingSource.clip != null)
            {
                typingSource.pitch = Random.Range(0.95f, 1.05f);
                typingSource.PlayOneShot(typingSource.clip, 0.6f);
            }
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && musicSource.clip != null)
            {
                if (musicSource.time > 0f)
                {
                    // Music was paused, resume it
                    musicSource.UnPause();
                }
                else
                {
                    // Music was stopped, restart it
                    musicSource.Play();
                }
            }
        }
    }
}


