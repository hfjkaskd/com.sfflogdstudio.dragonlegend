using System;
using System.Collections.Generic;
namespace DragonLegend.Whitebox.Recovered
{
    [Serializable]
    public sealed class PlayerData
    {
        public int Level;
        public List<int> BonusArea;
        public float GreenCount;
        public float LevelExpCount;
        public int SpinCount;
        public int LastSpinTime;
        public int JpAddCount;
        public int BankCount;
        public bool IsMusic;
        public bool IsVibrate;
        public int MoreWild;
        public int LoginTime;
        public List<PlayerTaskData> PlayerTaskDatas;
        public List<PlayerCollectData> PlayerCollectDatas;
        public List<PlayerCashOutData> PlayerCashOutDatas;
        public List<Account> PlayerAccount;
        public List<PlayerCashOutOrder> PlayerCashOutOrders;
        public GiftAccount GiftAccount;
        public int RandomIndex;
        public bool IsGetPushReward;
        public int GuideStep;
        public List<int> CashOutTipIndexs;
        public bool isFirstHall;
        public bool isStartSpin;
        public int LimitSpinCount;
        public int GreenCountLog;
        public int SpinCountLog;
        public PlayerData()
        {
            RandomIndex = -1; GuideStep = 1; Level = 1;
            IsMusic = true; IsVibrate = true;
            BonusArea = new List<int>{0,0,0,0,0};
            PlayerTaskDatas = new List<PlayerTaskData>();
            PlayerCollectDatas = new List<PlayerCollectData>();
            PlayerCashOutDatas = new List<PlayerCashOutData>();
            PlayerAccount = new List<Account>();
            PlayerCashOutOrders = new List<PlayerCashOutOrder>();
            CashOutTipIndexs = new List<int>();
            LoginTime = unchecked((int)DateTimeOffset.Now.ToUnixTimeSeconds());
        }
        public void Init(DragonLegend.Whitebox.RecoveredGameplayRules rules)
        {
            GreenCount = rules.GetInitGreenCount();
            SpinCount = rules.GetInitSpinCount();
            if (RandomIndex == -1) RandomIndex = UnityEngine.Random.Range(0, rules.GetCollectInfoCount());
        }
    }
    [Serializable]
    public sealed class PlayerTaskData
    {
        public int id;
        public int count;
        public bool isRecieve;

    }
    [Serializable]
    public sealed class PlayerCollectData
    {
        public int id;
        public int count;
        public bool isRecieve;

    }
    [Serializable]
    public sealed class PlayerCashOutData
    {
        public int id;
        public int type;
        public int step;
        public int count;
        public int time;
        public bool isCashout;

    }
    [Serializable]
    public sealed class Account
    {
        public string accountName;
        public string emailName;
        public int type;

    }
    [Serializable]
    public sealed class PlayerCashOutOrder
    {
        public string orderId;
        public int index;
        public string status;

    }
    [Serializable]
    public sealed class GiftAccount
    {
        public string name;
        public string address;
        public string code;
        public string phone;

    }
}
