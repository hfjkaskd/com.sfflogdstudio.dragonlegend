using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Obfuz.ObfuzIgnore]
public partial class AccountModuleCfg
{
    public const string add_ecpm = "/Frenzy/Bus/AddEcpm"; // 添加收益  可以点击查看 ---------------------------------
    public const string add_order = "/Frenzy/Bus/AddOrder"; // 去提现 - 全部提现 可以点击查看   ---------------------------------
    public const string add_order_task = "/Frenzy/Bus/AddOrderTask"; // 去提现 - 现金或奖卷 可以点击查看  ---------------------------------
    public const string base_list = "/Frenzy/Bus/BaseData"; // 其它配置信息 - 可以点击查看  ---------------------------------
    public const string from = "/Frenzy/Bus/From"; // 用户归因 - 后台查看   --------------------------------- 这个 AI写的不太对 VN
    public const string get_ecpm_id = "/Frenzy/Bus/EcpmId"; // 获取收益ID - 可以点击查看 --------------------------------- 
    public const string login = "/Frenzy/Bus/Login"; // 用户登陆 - 可以点击查看 --------------------------------- 
    public const string msg = "/Frenzy/Bus/Talk"; // 添加反馈 - 需要服务器查看 ---------------------------------
    public const string msg_list = "/Frenzy/Bus/TalkList"; // 反馈列表 - 需要服务器查看  ---------------------------------
    public const string new_user = "/Frenzy/Bus/New"; // 新用户奖励-领取 - 可以点击查看 ---------------------------------
    public const string number = "/Frenzy/Bus/Reach"; // 关卡，完成次数上报 - 可以点击查看 ---------------------------------
    public const string order_list = "/Frenzy/Bus/Order"; // 订单列表 - 可以点击查看 ---------------------------------
    public const string order_name = "/Frenzy/Bus/OrderNick"; // 提现用户列表 - 可以点击查看 
    public const string plat_from = "/Frenzy/Bus/PayPage"; // 提现平台 - 可以点击查看 ---------------------------------
    public const string task_list = "/Frenzy/Bus/TaskList"; // 每日任务-现金 - 可以点击查看
    public const string user_info = "/Frenzy/Bus/Info"; // 用户信息 - 可以点击查看 ---------------------------------
    public const string user_name = "/Frenzy/Bus/NickName"; // 昵称添加
    public const string ad_info = "/Frenzy/BusLog/AdInfo"; // 广告上报 - 可以点击查看
    public const string app_info = "/Frenzy/BusLog/AppInfo"; // 日志上报 - 可以点击查看


    #region 每日任务
    public const string one_Count = "os_one";
    public const string one_Ratio = "os_one_rate";
    public const string two_Count = "os_two";
    public const string two_Ratio = "os_two_rate";
    public const string three_Count = "os_three";
    public const string three_Ratio = "os_three_rate";
    #endregion
}

/*
public const string add_ecpm = ""; // 添加收益  可以点击查看 ---------------------------------
    public const string add_order = ""; // 去提现 - 全部提现 可以点击查看   ---------------------------------
    public const string add_order_task = ""; // 去提现 - 现金或奖卷 可以点击查看  ---------------------------------
    public const string base_list = ""; // 其它配置信息 - 可以点击查看  ---------------------------------
    public const string from = ""; // 用户归因 - 后台查看   --------------------------------- 这个 AI写的不太对 VN
    public const string get_ecpm_id = ""; // 获取收益ID - 可以点击查看 --------------------------------- 
    public const string login = ""; // 用户登陆 - 可以点击查看 --------------------------------- 
    public const string msg = ""; // 添加反馈 - 需要服务器查看 ---------------------------------
    public const string msg_list = ""; // 反馈列表 - 需要服务器查看  ---------------------------------
    public const string new_user = ""; // 新用户奖励-领取 - 可以点击查看 ---------------------------------
    public const string number = ""; // 关卡，完成次数上报 - 可以点击查看 ---------------------------------
    public const string order_list = ""; // 订单列表 - 可以点击查看 ---------------------------------
    public const string order_name = ""; // 提现用户列表 - 可以点击查看 
    public const string plat_from = ""; // 提现平台 - 可以点击查看 ---------------------------------
    public const string task_list = ""; // 每日任务-现金 - 可以点击查看
    public const string user_info = ""; // 用户信息 - 可以点击查看 ---------------------------------
    public const string user_name = ""; // 昵称添加
    public const string ad_info = ""; // 广告上报 - 可以点击查看
    public const string app_info = ""; // 日志上报 - 可以点击查看
*/