using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;

public sealed class RecoveredFreeSpinResultTests
{
    private static List<int> WeightAt(int index)
    {
        var weights = new List<int>();
        for (int i=0;i<=index;i++) weights.Add(i==index ? 1000000 : 0);
        return weights;
    }
    private static RecoveredGameplayRules Rules(int coins,int balls) => new RecoveredGameplayRules(
        new GoldenDragonAutoGenConfig {Rrggiomg=new RrggiomgPoro {
            QoinOmoinrKgiitr=WeightAt(coins),RollOmoinrKgiitr=WeightAt(balls),
            RollRipgKgiitr=new List<int>{2,3,5,7}}});

    [TestCase(17,0,0)]
    [TestCase(23,6,7)]
    [TestCase(37,10,5)]
    public void IncrementalResultMatchesNativePlacementAndRandomConsumption(int seed,int coins,int balls)
    {
        var saved = UnityEngine.Random.state;
        try {
            var rules = Rules(coins,balls); var ids = new[]{0,2,4,11};
            UnityEngine.Random.InitState(seed);
            int expectedCoins=rules.GetFreeCoinAmount(),expectedBalls=rules.GetFreeBallAmount();
            var expected=new int[5,3]; var used=new List<int>[5];
            for(int col=0;col<5;col++) {
                used[col]=new List<int>();
                for(int row=0;row<3;row++) expected[col,row]=ids[UnityEngine.Random.Range(0,ids.Length)];
            }
            Place(expected,used,expectedCoins,9); Place(expected,used,expectedBalls,11);
            var types=new List<int>();
            for(int col=0;col<5;col++) for(int row=0;row<3;row++)
                if(expected[col,row]==11) types.Add(rules.GetFreeBallType());
            int expectedNext=UnityEngine.Random.Range(0,int.MaxValue);
            UnityEngine.Random.InitState(seed);
            var actual=new RecoveredFreeSpinResult(rules); int callbacks=0;
            actual.Begin(ids,()=>{Assert.IsFalse(actual.IsGenerating); callbacks++;});
            int attempts=0;
            while(actual.IsGenerating && attempts++<10000) actual.Step();
            Assert.IsFalse(actual.IsGenerating);
            Assert.AreEqual(1,callbacks);
            Assert.AreEqual(expectedCoins,actual.CoinAmount); Assert.AreEqual(expectedBalls,actual.BallAmount);
            for(int col=0;col<5;col++) for(int row=0;row<3;row++) Assert.AreEqual(expected[col,row],actual.GetSymbol(col,row));
            Assert.AreEqual(types.Count,actual.GeneratedBallCount);
            for(int i=0;i<types.Count;i++) Assert.AreEqual(types[i],actual.GetBall(i));
            Assert.AreEqual(expectedNext,UnityEngine.Random.Range(0,int.MaxValue));
            Assert.IsFalse(actual.Step()); Assert.AreEqual(1,callbacks);
        } finally {UnityEngine.Random.state=saved;}
    }

    // Direct list-based reference for native CheckSingleSymbol, including full-column retries.
    private static void Place(int[,] target,List<int>[] used,int count,int symbol)
    {
        for(int i=0;i<count;i++) {
            var available=new List<int>(); int col;
            do {
                col=UnityEngine.Random.Range(0,5);
                for(int row=0;row<3;row++) if(!used[col].Contains(row)) available.Add(row);
            } while(available.Count==0);
            int selected=available[UnityEngine.Random.Range(0,available.Count)];
            target[col,selected]=symbol; used[col].Add(selected);
        }
    }

    [Test]
    public void FullBoardExcessPlacementYieldsWithoutDiscardingRequestedCount()
    {
        var saved=UnityEngine.Random.state;
        try {
            UnityEngine.Random.InitState(23);
            var result=new RecoveredFreeSpinResult(Rules(16,0)); int callbacks=0;
            result.Begin(new[]{0},()=>callbacks++);
            Assert.AreEqual(16,result.CoinAmount);
            for(int i=0;i<1000;i++) Assert.IsTrue(result.Step());
            Assert.IsTrue(result.IsGenerating); Assert.AreEqual(0,callbacks);
            Assert.Throws<InvalidOperationException>(()=>result.Begin(new[]{0}));
        } finally {UnityEngine.Random.state=saved;}
    }
}