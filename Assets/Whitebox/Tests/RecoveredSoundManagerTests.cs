using System.Collections;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredSoundManagerTests
{
    [UnityTest]
    public IEnumerator OriginalChannelsShareMusicGateAndRememberMutedBgmChanges()
    {
        var root = Object.Instantiate(Resources.Load<RecoveredCoreAudio>("RecoveredAudio/CoreAudio"));
        try {
            var manager = root.Manager;
            var player = new PlayerData { IsMusic = false };
            manager.Bind(player);
            Assert.AreEqual("normalBg", manager.RequestedMusic);
            manager.PlaySound("click"); manager.PlaySound1("ring");
            manager.ChangeBGM("bonusBg");
            Assert.AreEqual(0, manager.LoadedClipCount, "Muted channels must not load audio.");
            Assert.IsNull(manager.MusicSource.clip);
            player.IsMusic = true; manager.SetMusic();
            yield return null;
            Assert.AreEqual("bonusBg", manager.MusicSource.clip.name);
            Assert.IsTrue(manager.MusicSource.isPlaying);
            Assert.IsTrue(manager.MusicSource.loop);
            Assert.IsFalse(manager.SoundSource.loop); Assert.IsFalse(manager.Sound1Source.loop);
            Assert.AreEqual(0, manager.MusicSource.spatialBlend);
            manager.PlaySound("normalBg"); manager.PlaySound1("bonusBg");
            yield return null;
            Assert.IsTrue(manager.SoundSource.isPlaying); Assert.IsTrue(manager.Sound1Source.isPlaying);
            manager.StopSound1(); Assert.IsFalse(manager.Sound1Source.isPlaying);
            Assert.IsTrue(manager.SoundSource.isPlaying, "Second channel stop must leave ordinary one-shots playing.");
            manager.PauseMusic(); Assert.IsFalse(manager.MusicSource.isPlaying);
            manager.CtnMusic(); yield return null; Assert.IsTrue(manager.MusicSource.isPlaying);
            var clip = manager.MusicSource.clip;
            manager.ChangeBGM("unknown"); Assert.AreSame(clip, manager.MusicSource.clip);
            Assert.AreEqual("unknown", manager.RequestedMusic);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => manager.SetMusic());
            player.IsMusic = false; manager.SetMusic(); Assert.IsFalse(manager.MusicSource.isPlaying);
            manager.StopAll(); Assert.IsFalse(manager.SoundSource.isPlaying);
        } finally { Object.DestroyImmediate(root.gameObject); }
    }
}
