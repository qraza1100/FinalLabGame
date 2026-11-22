using System;
using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Holds the scene's audio sources and clips. Call SFX.Play(s => s.pearl).</summary>
    public class SFX : MonoBehaviour
    {
        public static SFX I { get; private set; }

        public AudioSource sfxSource;
        public AudioSource musicSource;
        public AudioSource ambientSource;
        [Range(0, 1)] public float musicBaseVolume = 0.5f;
        [Range(0, 1)] public float ambientBaseVolume = 0.45f;

        [Header("Clips")]
        public AudioClip click, hover, pearl, sigil, heart, denied, win, lose, tick;
        public AudioClip gateOpen, gateClose, vent, hit, collapse, sonar, boost;

        void Awake()
        {
            I = this;
            AudioListener.volume = SaveSystem.MasterVolume;
            ApplyMusicVolume();
        }

        public void ApplyMusicVolume()
        {
            if (musicSource) musicSource.volume = SaveSystem.MusicVolume * musicBaseVolume;
            if (ambientSource) ambientSource.volume = SaveSystem.MusicVolume * ambientBaseVolume;
        }

        public static void Play(Func<SFX, AudioClip> pick, float volume = 1f)
        {
            if (I == null || I.sfxSource == null || pick == null) return;
            var clip = pick(I);
            if (clip != null) I.sfxSource.PlayOneShot(clip, volume);
        }
    }
}
