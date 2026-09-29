using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Temporary Editor diagnostic. Does not create a scene, run GameEntry, or access player storage.
public static class BigWinThresholdValidation
{
    const int SampleCount=10000,Seed=16092026,Bet=1000;
    const string Output="Artifacts/BigWinEasy";
    [Serializable] public sealed class Bucket
    {
        public string thresholdsDollars;
        public int none,big,mega,super,any;
        public double noneRate,bigRate,megaRate,superRate,anyRate;
        public void Add(RecoveredSlotWinType type)
        {
            switch(type){case RecoveredSlotWinType.None:none++;break;case RecoveredSlotWinType.Big:big++;break;case RecoveredSlotWinType.Mega:mega++;break;case RecoveredSlotWinType.Super:super++;break;default:throw new InvalidOperationException("Unexpected Big Win class.");}
        }
        public void Finish(){any=big+mega+super;noneRate=(double)none/SampleCount;bigRate=(double)big/SampleCount;megaRate=(double)mega/SampleCount;superRate=(double)super/SampleCount;anyRate=(double)any/SampleCount;}
    }
    [Serializable] public sealed class Distribution
    {
        public int count;public double minimum,p10,p25,p50,p75,p90,p95,p99,maximum,mean;
    }
    [Serializable] public sealed class Boundary
    {
        public int betRaw;public float dollars;public string expected,current;
    }
    [Serializable] public sealed class Report
    {
        public string status,error,scope,randomnessCaveat,coinUnits,baselineSha256,currentSha256;
        public bool hybridValidationPassed,onlyBigWinThresholdChanged,allBoundariesPassed,pairedClassificationPassed,legacyFallbackPassed,betScalingPassed;
        public int samples,unitySeed,guideStep,moreWild,betRaw,positiveRewardSpins,coinBearingSpins,coinCount,scatterEntryBoards,guaranteedWildBranches,bonusCollectionResets;
        public double positiveRewardRate,addedPopupRate,averageBaseRewardDollars,averageLineRewardDollars,averageCoinRewardDollars;
        public Bucket oldThresholds=new Bucket{thresholdsDollars="$10/$20/$30"},newThresholds=new Bucket{thresholdsDollars="$2/$5/$10"};
        public Distribution allBaseRewardDollars,positiveBaseRewardDollars;
        public List<Boundary> boundaries=new List<Boundary>();
    }

    [MenuItem("Tools/Validation/Big Win Thresholds")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var priorRandom=UnityEngine.Random.state;
        var report=new Report {
            status="FAIL",samples=SampleCount,unitySeed=Seed,guideStep=2,moreWild=0,betRaw=Bet,
            scope="10,000 consecutive BASE result generations, GuideStep=2, MoreWild=0, bet=1000 raw ($10). Persistent base Wild/Bonus counters and base-coin collection advance between samples; full bonus collection resets to zero. Same board and coin award are classified against both threshold sets. Includes this base board's line award and coin award only; excludes all later free spins, bonus minigame rewards, jackpots, task payouts, ad multipliers and elapsed-time/UI behavior. This is a base-spin sampling comparison, not a full player-session or popup-per-minute estimate.",
            randomnessCaveat="UnityEngine.Random has fixed seed 16092026. Production RecoveredSlotBoard also creates System.Random() internally for Wild row/column order and guaranteed Bonus selection; its clock-based seed cannot be injected through RecoveredSpinResult. Repeated runs can therefore differ. These are sample frequencies, not exact theoretical probabilities. The old/new comparison is paired on exactly the same sampled amounts and does not redraw boards.",
            coinUnits="Rules.GetCoinReward returns raw cents. Each final-board symbol id=9 is sampled once in column-major order, matching RecoveredBonusCoinSequence; sum is added directly to Settlement.GetWinTotalLine(), matching RecoveredSymbolWinSelection.TotalWin. Dollar reporting divides by 100, with no extra bet multiplier."
        };
        try {
            HybridGameplayValidation.Run();report.hybridValidationPassed=true;
            string file=Path.Combine(Application.streamingAssetsPath,"RecoveredConfig/Remote/cp_low_frequency_high_rewards.json");
            string currentJson=File.ReadAllText(file),beforeJson=File.ReadAllText(Output+"/config-before.json");
            report.currentSha256=Hash(currentJson);report.baselineSha256=Hash(beforeJson);
            var beforeToken=JObject.Parse(beforeJson);var currentToken=JObject.Parse(currentJson);
            Require(JToken.DeepEquals(beforeToken["Qonrii"]["Riikin"],JArray.FromObject(new[]{1,2,3})),"Baseline Riikin must be [1,2,3].");
            Require(beforeToken["Qonrii"]["BigWinBetPercent"]==null,"Baseline must not contain BigWinBetPercent.");
            Require(JToken.DeepEquals(currentToken["Qonrii"]["Riikin"],JArray.FromObject(new[]{1,2,3})),"Legacy Riikin must remain [1,2,3].");
            Require(JToken.DeepEquals(currentToken["Qonrii"]["BigWinBetPercent"],JArray.FromObject(new[]{20,50,100})),"Production percentage must be [20,50,100].");
            beforeToken["Qonrii"]["BigWinBetPercent"]=JArray.FromObject(new[]{20,50,100});
            Require(JToken.DeepEquals(beforeToken,currentToken),"A configuration field besides Qonrii.BigWinBetPercent changed.");report.onlyBigWinThresholdChanged=true;
            var config=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(currentJson);
            var oldConfig=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(beforeJson);
            var rules=new RecoveredGameplayRules(config);var oldRules=new RecoveredGameplayRules(oldConfig);
            Boundaries(rules,report);LegacyFallback(config,report);
            var data=new PlayerData{GuideStep=2,MoreWild=0};
            var player=new RecoveredPlayerProgress(rules,()=>{},data);
            var settlement=new RecoveredSlotSettlement(rules);
            var result=new RecoveredSpinResult(rules,settlement,player);
            var selection=new RecoveredSymbolWinSelection(rules);
            var amounts=new List<double>(SampleCount);var positiveAmounts=new List<double>(SampleCount);
            double lineTotal=0,coinTotal=0;UnityEngine.Random.InitState(Seed);
            for(int sample=0;sample<SampleCount;sample++) {
                bool guaranteed=unchecked(result.WildCounter+1)>=rules.GetWildSpinCD();
                if(guaranteed)report.guaranteedWildBranches++;
                result.Begin(data.GuideStep==1,Bet,player.MoreWild,data.BonusArea);
                int steps=0;while(result.IsGenerating&&steps++<100000)result.Step();
                Require(!result.IsGenerating,"Spin result exceeded bounded generation at sample "+sample);
                if(result.ScatterCount>=3)report.scatterEntryBoards++;
                float coinAmount=0;int coins=0;
                // This pure data scan mirrors BonusCoinSequence.Advance's exact ordering and unit.
                // No RecoveredReelWait/player loop is installed, and no presentation/SDK callback runs.
                for(int column=0;column<5;column++)for(int row=0;row<3;row++)if(result.Board.GetSymbol(column,row)==9){coinAmount+=rules.GetCoinReward();data.BonusArea[column]=unchecked(data.BonusArea[column]+1);coins++;}
                if(coins>0)report.coinBearingSpins++;report.coinCount+=coins;
                selection.Capture(settlement,result.Board.GetSymbol,coinAmount,Bet);
                float raw=selection.TotalWin;Require(raw>=0&&!float.IsInfinity(raw)&&!float.IsNaN(raw),"Invalid reward amount.");
                var oldType=oldRules.GetBigWin(raw,Bet);var newType=selection.BigWin;
                Require(newType==rules.GetBigWin(raw,Bet),"Selection and rules disagree.");
                Require((int)newType>=(int)oldType,"Lower threshold demoted an outcome.");
                report.oldThresholds.Add(oldType);report.newThresholds.Add(newType);
                double dollars=raw/100.0;amounts.Add(dollars);lineTotal+=selection.LineWin/100.0;coinTotal+=coinAmount/100.0;
                if(raw>0){report.positiveRewardSpins++;positiveAmounts.Add(dollars);}
                bool collected=true;for(int column=0;column<5;column++)if(data.BonusArea[column]<2){collected=false;break;}
                if(collected){for(int column=0;column<5;column++)data.BonusArea[column]=0;report.bonusCollectionResets++;}
            }
            report.oldThresholds.Finish();report.newThresholds.Finish();report.pairedClassificationPassed=true;
            report.positiveRewardRate=(double)report.positiveRewardSpins/SampleCount;
            report.addedPopupRate=report.newThresholds.anyRate-report.oldThresholds.anyRate;
            report.averageLineRewardDollars=lineTotal/SampleCount;report.averageCoinRewardDollars=coinTotal/SampleCount;
            report.averageBaseRewardDollars=(lineTotal+coinTotal)/SampleCount;
            report.allBaseRewardDollars=Describe(amounts);report.positiveBaseRewardDollars=Describe(positiveAmounts);
            Require(report.newThresholds.any>=report.oldThresholds.any,"New threshold reduced eligible popup count.");
            report.status="PASS";
            Debug.Log("BIG_WIN_EASY_PASS: same "+SampleCount+" base boards; old "+report.oldThresholds.any+" / new "+report.newThresholds.any+" eligible. Sampling caveats recorded in report.json.");
        } catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
        finally {UnityEngine.Random.state=priorRandom;File.WriteAllText(Output+"/report.json",JsonUtility.ToJson(report,true));}
        if(Application.isBatchMode)EditorApplication.Exit(report.status=="PASS"?0:1);
    }
    static void Boundaries(RecoveredGameplayRules rules,Report report)
    {
        int[] cents={0,199,200,499,500,999,1000,1000000};
        var expected=new[]{RecoveredSlotWinType.None,RecoveredSlotWinType.None,RecoveredSlotWinType.Big,RecoveredSlotWinType.Big,RecoveredSlotWinType.Mega,RecoveredSlotWinType.Mega,RecoveredSlotWinType.Super,RecoveredSlotWinType.Super};
        for(int scale=1;scale<=2;scale++)for(int i=0;i<cents.Length;i++) {
            int amount=cents[i]*scale,bet=Bet*scale;var found=rules.GetBigWin(amount,bet);
            report.boundaries.Add(new Boundary{betRaw=bet,dollars=amount/100f,expected=Name(expected[i]),current=Name(found)});
            Require(found==expected[i],"Boundary "+amount+" cents at bet "+bet+" failed.");
        }
        // Exact cent immediately below each doubled threshold (scaling 199 to 398 alone skips 399).
        Require(rules.GetBigWin(399,2000)==RecoveredSlotWinType.None,"Doubled $4 lower boundary");
        Require(rules.GetBigWin(999,2000)==RecoveredSlotWinType.Big,"Doubled $10 lower boundary");
        Require(rules.GetBigWin(1999,2000)==RecoveredSlotWinType.Mega,"Doubled $20 lower boundary");
        report.allBoundariesPassed=report.betScalingPassed=true;
    }
    static void LegacyFallback(GoldenDragonAutoGenConfig current,Report report)
    {
        foreach(bool empty in new[]{false,true}) {
            var legacy=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(JsonUtility.ToJson(current));
            legacy.Qonrii.BigWinBetPercent=empty?new List<int>():null;
            legacy.Qonrii.Riikin=new List<int>{1,2,3};var rules=new RecoveredGameplayRules(legacy);
            int[] cents={0,999,1000,1999,2000,2999,3000,1000000};
            var expected=new[]{RecoveredSlotWinType.None,RecoveredSlotWinType.None,RecoveredSlotWinType.Big,RecoveredSlotWinType.Big,RecoveredSlotWinType.Mega,RecoveredSlotWinType.Mega,RecoveredSlotWinType.Super,RecoveredSlotWinType.Super};
            for(int i=0;i<cents.Length;i++)Require(rules.GetBigWin(cents[i],Bet)==expected[i],"Legacy fallback boundary; empty="+empty+", amount="+cents[i]);
            // Preserve the native signed-int multiplication BEFORE float conversion on fallback.
            // At bet=int.MaxValue the [2,4,6] products are [-2,-4,-6], scanned in reverse.
            legacy.Qonrii.Riikin=new List<int>{2,4,6};
            Require(rules.GetBigWin(-7,int.MaxValue)==RecoveredSlotWinType.None,"Legacy overflow -7 below -6");
            Require(rules.GetBigWin(-6,int.MaxValue)==RecoveredSlotWinType.Super,"Legacy overflow -6 equal threshold");
            Require(rules.GetBigWin(0,int.MaxValue)==RecoveredSlotWinType.Super,"Legacy overflow 0 above threshold");
            Require(rules.GetBigWin(-1,int.MinValue)==RecoveredSlotWinType.None,"Legacy min-int products zero, below threshold");
            Require(rules.GetBigWin(0,int.MinValue)==RecoveredSlotWinType.Super,"Legacy min-int products zero, at threshold");
            // Real old [2,4,6] configuration continues to mean $20/$40/$60 at bet 1000.
            Require(rules.GetBigWin(1999,Bet)==RecoveredSlotWinType.None,"Original fallback below $20");
            Require(rules.GetBigWin(2000,Bet)==RecoveredSlotWinType.Big,"Original fallback $20");
            Require(rules.GetBigWin(4000,Bet)==RecoveredSlotWinType.Mega,"Original fallback $40");
            Require(rules.GetBigWin(6000,Bet)==RecoveredSlotWinType.Super,"Original fallback $60");
        }
        report.legacyFallbackPassed=true;
    }
    static Distribution Describe(List<double> amounts)
    {
        amounts.Sort();var d=new Distribution{count=amounts.Count};if(amounts.Count==0)return d;
        double sum=0;foreach(double value in amounts)sum+=value;
        d.minimum=amounts[0];d.maximum=amounts[amounts.Count-1];d.mean=sum/amounts.Count;
        d.p10=Quantile(amounts,.10);d.p25=Quantile(amounts,.25);d.p50=Quantile(amounts,.5);d.p75=Quantile(amounts,.75);d.p90=Quantile(amounts,.9);d.p95=Quantile(amounts,.95);d.p99=Quantile(amounts,.99);return d;
    }
    // Linear interpolation on zero-indexed sorted rank (n-1)*p.
    static double Quantile(List<double> values,double p){double rank=(values.Count-1)*p;int low=(int)rank;int high=Math.Min(low+1,values.Count-1);return values[low]+(values[high]-values[low])*(rank-low);}
    static string Name(RecoveredSlotWinType type){switch(type){case RecoveredSlotWinType.None:return "NONE";case RecoveredSlotWinType.Big:return "BIG";case RecoveredSlotWinType.Mega:return "MEGA";case RecoveredSlotWinType.Super:return "SUPER";default:return "UNKNOWN";}}
    static string Hash(string text){using(var sha=SHA256.Create()){byte[] bytes=sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();}}
    static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException("BIG_WIN_VALIDATION_FAILED: "+message);}
}
