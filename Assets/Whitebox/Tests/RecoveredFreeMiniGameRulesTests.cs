using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using Random=UnityEngine.Random;

public sealed class RecoveredFreeMiniGameRulesTests
{
    [Test]
    public void EachColorReadsFourBranchWeightsAndPreservesRandomStream()
    {
        var random=Random.state;
        var c=new RrggiomgPoro{RollGlorg=new List<int>{2,3,23},RollKtggl=new List<int>{5,13,29},
            RollRrgogirg=new List<int>{7,17,31},RollLiqki=new List<int>{11,19,37}};
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=c});
        try {
            Random.InitState(731);int[] totals={25,52,120};int[,] boundaries={{2,7,14},{3,16,33},{23,52,83}};
            int[] expected=new int[3];
            for(int color=0;color<3;color++){int draw=Random.Range(0,totals[color]);expected[color]=draw<=boundaries[color,0]?0:draw<=boundaries[color,1]?1:draw<=boundaries[color,2]?2:3;}
            int next=Random.Range(0,1000000);Random.InitState(731);
            for(int color=0;color<3;color++)Assert.AreEqual(expected[color],(int)rules.GetFreeReward(color));
            Assert.AreEqual(next,Random.Range(0,1000000));
            c.RollGlorg[1]=0;c.RollKtggl[1]=0;c.RollRrgogirg[1]=0;c.RollLiqki[1]=1000000;
            Random.InitState(71);int liveDraw=Random.Range(0,1000000);Random.InitState(71);
            Assert.AreEqual(liveDraw==0?RecoveredFreeSpinReward.Slot:RecoveredFreeSpinReward.Lucky,rules.GetFreeReward(1));
            Random.InitState(8);next=Random.Range(0,1000000);Random.InitState(8);
            Assert.Throws<ArgumentOutOfRangeException>(()=>rules.GetFreeReward(3));
            Assert.AreEqual(next,Random.Range(0,1000000),"Invalid config column fails before random selection.");
        } finally {Random.state=random;}
    }
    [Test]
    public void SlotAndLuckyRewardsUseInclusiveIntegerDrawsThenFloatConversion()
    {
        var random=Random.state;
        var c=new RrggiomgPoro{GlorgKgiitr=new List<int>{4,9},GlorgMin=new List<int>{100,16777217},
            GlorgMoj=new List<int>{110,16777225},LiqkiRgkorp=new List<int>{201,205}};
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=c});
        try {
            Random.InitState(901);int index=Random.Range(0,13)<=4?0:1;
            float slot=Random.Range(c.GlorgMin[index],c.GlorgMoj[index]+1),lucky=Random.Range(201,206);
            int next=Random.Range(0,1000000);Random.InitState(901);
            Assert.AreEqual(slot,rules.GetSlotReward());Assert.AreEqual(lucky,rules.GetLuckyReward());Assert.AreEqual(next,Random.Range(0,1000000));
            c.LiqkiRgkorp[0]=c.LiqkiRgkorp[1]=16777217;
            Assert.AreEqual((float)16777217,rules.GetLuckyReward());
        } finally {Random.state=random;}
    }
    [Test]
    public void WheelUsesOriginalEightWedgesAndOnlyWeightSelectionConsumesRandom()
    {
        var random=Random.state;
        var c=new RrggiomgPoro{KtgglRgkorp=new List<int>{16777217,22,33,44,55,66,77,88},KtgglRgkorpKgiitr=new List<int>{0,0,0}};
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=c});
        try {
            var map=rules.GetWheelInfo();Assert.AreSame(map,rules.GetWheelInfo());
            int[] expected={0,1,2,1,3,1,2,1};Assert.AreEqual(8,map.Count);
            Random.InitState(32);int next=Random.Range(0,1000000);Random.InitState(32);
            for(int i=0;i<8;i++){Assert.AreEqual(expected[i],(int)map[i]);Assert.AreEqual((float)c.KtgglRgkorp[i],rules.GetWheelReward(i));}
            Assert.AreEqual(next,Random.Range(0,1000000));
            Assert.AreEqual(0,rules.RandomWheelWeight(),"Native inclusive threshold selects the first zero-weight wedge.");
            c.KtgglRgkorpKgiitr.Clear();Assert.AreEqual(-1,rules.RandomWheelWeight());
            Assert.Throws<ArgumentOutOfRangeException>(()=>rules.GetWheelReward(8));
        } finally {Random.state=random;}
    }
}
