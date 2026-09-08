using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using Random=UnityEngine.Random;

public sealed class RecoveredFreeSmallGameRouterTests
{
    [Test]
    public void OriginalWeightColumnDispatchesOnceAndSlotSavesBeforeEntryWithoutDrawingReward()
    {
        var random=Random.state;
        var config=new RrggiomgPoro{RollGlorg=new List<int>{2,3,23},RollKtggl=new List<int>{5,13,29},
            RollRrgogirg=new List<int>{7,17,31},RollLiqki=new List<int>{11,19,37}};
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=config});
        try {
            var seen=new HashSet<int>();int[] totals={25,52,120};int[,] limits={{2,7,14},{3,16,33},{23,52,83}};
            foreach(int type in new[]{0,1,2,-1,99})for(int seed=0;seed<25;seed++) {
                int column=type==0?0:type==1?1:2;
                Random.InitState(seed);int draw=Random.Range(0,totals[column]);
                int expected=draw<=limits[column,0]?0:draw<=limits[column,1]?1:draw<=limits[column,2]?2:3;
                int next=Random.Range(0,1000000);Random.InitState(seed);
                int saves=0,calls=0,received=0;var data=new PlayerData{PlayerTaskDatas=new List<PlayerTaskData>()};
                var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
                Action<float> callback=value=>received++;
                var position=new Vector3(7,8,9);
                Action<int,Vector3,Action<float>> enter=(branch,source,reply)=>{
                    calls++;seen.Add(branch);Assert.AreEqual(expected,branch);Assert.AreEqual(position,source);Assert.AreSame(callback,reply);
                    Assert.AreEqual(branch==0?1:0,saves);
                    Assert.AreEqual(branch==0?1:0,data.PlayerTaskDatas.Count);
                    if(branch==0){Assert.AreEqual(4,data.PlayerTaskDatas[0].id);Assert.AreEqual(1,data.PlayerTaskDatas[0].count);}
                };
                var router=new RecoveredFreeSmallGameRouter(rules,player,(p,c)=>enter(0,p,c),(p,c)=>enter(1,p,c),(p,c)=>enter(2,p,c),(p,c)=>enter(3,p,c));
                router.Open(type,position,callback);
                Assert.AreEqual(1,calls);Assert.AreEqual(0,received,"Dispatch must await the actual game's reward callback.");
                Assert.AreEqual(next,Random.Range(0,1000000),"The router draws only the branch; branch rewards are drawn later.");
            }
            Assert.AreEqual(4,seen.Count);
        } finally {Random.state=random;}
    }
}
