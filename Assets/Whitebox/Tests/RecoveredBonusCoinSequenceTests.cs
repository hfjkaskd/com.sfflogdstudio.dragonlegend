using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

public sealed class RecoveredBonusCoinSequenceTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Ronig = new RonigPoro { QoinRgkorp = new List<int> { 4, 7 } }
    });
    [UnityTest]
    public IEnumerator CoinsScanLiveColumnMajorWithInclusiveRewardsAndFinalDelay()
    {
        var random = Random.state; float scale = Time.timeScale, delta = Time.captureDeltaTime;
        var data = new PlayerData { BonusArea = new List<int> { 2, 0, 0, 0, -1 }, GreenCount = 12 };
        int saves = 0, done = 0; var rules = Rules();
        var progress = new RecoveredPlayerProgress(rules, () => saves++, data);
        var sequence = new RecoveredBonusCoinSequence(rules, progress, .5f);
        var board = new int[5,3]; board[0,0] = board[0,2] = 9;
        var coins = new List<RecoveredBonusCoin>(); var times = new List<float>();
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            Random.InitState(954); var expected = new[] {Random.Range(4,8), Random.Range(4,8), Random.Range(4,8)};
            int expectedNext = Random.Range(0,100000); Random.InitState(954);
            sequence.Begin((c,r) => board[c,r], coin => { coins.Add(coin); times.Add(Time.time); }, () => done++);
            Assert.AreEqual(1,coins.Count); Assert.AreEqual(3,data.BonusArea[0]);
            board[4,2] = 9; // Read future cells when their turn arrives, not from a precomputed list.
            Time.timeScale = 0;
            for(int i=0;i<15;i++) yield return null;
            Assert.AreEqual(1,coins.Count); Assert.AreEqual(0,done);
            Time.timeScale = 1;
            for(int i=0;i<70&&sequence.IsRunning;i++) yield return null;
            Assert.IsNull(sequence.Error); Assert.AreEqual(1,done); Assert.AreEqual(3,coins.Count);
            Assert.AreEqual(0,coins[0].Column); Assert.AreEqual(0,coins[0].Row);
            Assert.AreEqual(0,coins[1].Column); Assert.AreEqual(2,coins[1].Row);
            Assert.AreEqual(4,coins[2].Column); Assert.AreEqual(2,coins[2].Row);
            for(int i=0;i<3;i++) Assert.AreEqual(expected[i],coins[i].Reward);
            Assert.AreEqual(expectedNext,Random.Range(0,100000));
            Assert.GreaterOrEqual(times[1]-times[0],.499f);
            Assert.GreaterOrEqual(times[2]-times[1],.499f);
            Assert.GreaterOrEqual(Time.time-times[2],.499f,"The final coin also incurs the delay.");
            Assert.AreEqual(4,data.BonusArea[0]); Assert.AreEqual(0,data.BonusArea[4]);
            Assert.AreEqual(expected[0]+expected[1]+expected[2],sequence.TotalReward);
            Assert.AreEqual(0,saves); Assert.AreEqual(12,data.GreenCount);
        } finally { sequence.CancelForProfileChange(); Random.state=random; Time.timeScale=scale; Time.captureDeltaTime=delta; }
    }
    [UnityTest]
    public IEnumerator CancellationAndPresentationFailureDoNotContinueOrUndoNativeWrites()
    {
        var random=Random.state; float delta=Time.captureDeltaTime,scale=Time.timeScale;
        var rules=Rules(); int saves=0,done=0,shown=0;
        var data=new PlayerData {BonusArea=new List<int>{0,0,0,0,0}};
        var progress=new RecoveredPlayerProgress(rules,()=>saves++,data);
        var sequence=new RecoveredBonusCoinSequence(rules,progress,.5f);
        try {
            Time.captureDeltaTime=.05f; Time.timeScale=1;
            sequence.Begin((c,r)=>9,coin=>shown++,()=>done++);
            sequence.CancelForProfileChange();
            for(int i=0;i<15;i++)yield return null;
            Assert.AreEqual(1,shown); Assert.AreEqual(0,done); Assert.AreEqual(1,data.BonusArea[0]);
            sequence.Begin((c,r)=>9,coin=>throw new System.InvalidOperationException("presentation"),()=>done++);
            Assert.IsInstanceOf<System.InvalidOperationException>(sequence.Error);
            Assert.IsFalse(sequence.IsRunning); Assert.AreEqual(2,data.BonusArea[0]);
            Assert.AreEqual(0,saves); Assert.AreEqual(0,done);
        } finally {sequence.CancelForProfileChange();Random.state=random;Time.captureDeltaTime=delta;Time.timeScale=scale;}
    }
    [Test]
    public void EmptyBoardCompletesSynchronouslyWithoutRandomDraws()
    {
        var random=Random.state;
        try {
            var rules=Rules();int saved=0,done=0,reads=0;
            var data=new PlayerData{BonusArea=new List<int>{0,0,0,0,0}};
            var sequence=new RecoveredBonusCoinSequence(rules,new RecoveredPlayerProgress(rules,()=>saved++,data),.5f);
            Random.InitState(8);int expected=Random.Range(0,100000);Random.InitState(8);
            sequence.Begin((c,r)=>{reads++;return 7;},coin=>Assert.Fail("No coin"),()=>done++);
            Assert.AreEqual(15,reads);Assert.AreEqual(1,done);Assert.IsFalse(sequence.IsRunning);
            Assert.AreEqual(0,sequence.TotalReward);Assert.AreEqual(0,saved);
            Assert.AreEqual(expected,Random.Range(0,100000));
        } finally {Random.state=random;}
    }
}
