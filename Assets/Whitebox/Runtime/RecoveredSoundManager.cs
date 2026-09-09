using System.Collections.Generic;
using DragonLegend.Whitebox.Recovered;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // SoundManager 237a590..237aa24. Assets load by path to avoid startup strong references.
    public sealed class RecoveredSoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource bgm, sound, sound1;
        [SerializeField] private string[] clipNames;
        [SerializeField] private string resourceRoot;
        [SerializeField] private string initialMusic;
        private readonly Dictionary<string, AudioClip> loaded = new Dictionary<string, AudioClip>();
        private HashSet<string> names;
        private PlayerData player;
        private string bgmMusic;
        public AudioSource MusicSource => bgm;
        public AudioSource SoundSource => sound;
        public AudioSource Sound1Source => sound1;
        public string RequestedMusic => bgmMusic;
        public int LoadedClipCount => loaded.Count;
        public void Bind(PlayerData data) { player = data; ChangeBGM(initialMusic); }
        private AudioClip Clip(string name)
        {
            if (loaded.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(resourceRoot + name);
            if (clip == null) throw new System.InvalidOperationException("Missing original audio: " + name);
            loaded.Add(name, clip); return clip;
        }
        private bool Contains(string name)
        {
            if (names == null) names = new HashSet<string>(clipNames);
            return names.Contains(name);
        }
        // Both native getters read PlayerData +0x38 (IsMusic).
        public void PlaySound(string name) { if (Contains(name) && player.IsMusic) sound.PlayOneShot(Clip(name)); }
        public void PlaySound1(string name) { if (Contains(name) && player.IsMusic) sound1.PlayOneShot(Clip(name)); }
        public void StopSound() => sound.Stop();
        public void StopSound1() => sound1.Stop();
        public void ChangeBGM(string name)
        {
            bgmMusic = name;
            if (Contains(name) && player.IsMusic) { bgm.clip = Clip(name); bgm.Play(); }
        }
        public void SetMusic()
        {
            if (!player.IsMusic) bgm.Stop();
            else { if (!Contains(bgmMusic)) throw new KeyNotFoundException(bgmMusic); bgm.clip = Clip(bgmMusic); bgm.Play(); }
        }
        public void PauseMusic() { if (player.IsMusic) bgm.Pause(); }
        public void CtnMusic() { if (player.IsMusic) bgm.Play(); }
        public void StopAll() { bgm.Stop(); sound.Stop(); sound1.Stop(); }
    }
}
