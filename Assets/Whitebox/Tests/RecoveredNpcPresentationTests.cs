using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredNpcPresentationTests
{
    [UnityTest]
    public IEnumerator BonusNpcKeepsIndependentSoundShakeAndAnimationCompletion()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/Npc"));
        var npc=root.GetComponent<RecoveredNpcPresentation>();var players=root.GetComponentsInChildren<RecoveredRegionAnimator>(true);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;yield return null;
            npc.Shake.Initialize();var origin=npc.Shake.OriginalPosition;
            int sounds=0,completions=0;double started=Time.timeAsDouble,soundAt=0;
            npc.SoundRequested+=name=>{Assert.AreEqual("dragon",name);sounds++;soundAt=Time.timeAsDouble-started;};
            npc.Show(2,()=>{Assert.AreEqual(0,players[0].Selected);Assert.IsFalse(players[1].gameObject.activeSelf);completions++;});
            Assert.AreEqual(1,players[0].Selected);Assert.AreEqual(2,players[1].Selected);
            Time.timeScale=0;for(int i=0;i<6;i++)yield return null;Assert.AreEqual(0,sounds);Assert.IsFalse(npc.Shake.IsShaking);
            Time.timeScale=1;for(int i=0;i<6;i++)yield return null;
            Assert.AreEqual(1,sounds);Assert.That(soundAt,Is.InRange(.199999,.31));Assert.IsFalse(npc.Shake.IsShaking);
            for(int i=0;i<14&&!npc.Shake.IsShaking;i++)yield return null;
            Assert.IsTrue(npc.Shake.IsShaking);Assert.That(Time.timeAsDouble-started,Is.InRange(.799999,.96));
            Assert.AreNotEqual(origin,npc.Shake.Target.anchoredPosition);
            for(int i=0;i<55;i++)yield return null;
            Assert.AreEqual(1,completions);Assert.IsFalse(npc.Shake.IsShaking);Assert.AreEqual(origin,npc.Shake.Target.anchoredPosition);

            // Idle interrupts completion, but the already-started delayed sound/shake survives.
            npc.Show(2,()=>completions++);npc.Show(0);npc.Show(99,()=>completions++);
            Assert.AreEqual(0,players[0].Selected);Assert.IsFalse(players[1].gameObject.activeSelf);
            for(int i=0;i<20&&!npc.Shake.IsShaking;i++)yield return null;
            Assert.AreEqual(2,sounds);Assert.IsTrue(npc.Shake.IsShaking);
            for(int i=0;i<60;i++)yield return null;Assert.AreEqual(1,completions);

            // Independent delayed jobs, newest animation completion only.
            npc.Show(1,()=>completions+=100);npc.Show(2,()=>completions++);
            for(int i=0;i<70;i++)yield return null;
            Assert.AreEqual(4,sounds);Assert.AreEqual(2,completions);
            npc.Show(1,()=>completions++);root.SetActive(false);
            for(int i=0;i<70;i++)yield return null;
            Assert.AreEqual(4,sounds);Assert.AreEqual(2,completions);
            root.SetActive(true);yield return null;npc.Show(1,()=>completions++);npc.Cancel();
            for(int i=0;i<70;i++)yield return null;
            Assert.AreEqual(4,sounds);Assert.AreEqual(2,completions);Assert.AreEqual(0,npc.State);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root);}
    }
}
