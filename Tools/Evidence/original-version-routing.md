# 原版配置版本与入口审计

范围：本地提取的 com.sfflogdstudio.dragonlegend 原版 ARM64 客户端、包内资源和 2026-09-07 设备远程响应缓存。不是服务器后台完整规则清单。

## 已确认的启动流程

1. GameData.Init (0x236d4b8) 将 IsA 初始化为 false。
2. GameStart.InitRemoteConfig MoveNext (0x2388c9c) 调用 RemoteConfigGetString，固定 key 为 cp_default_1。
3. 回调 (0x2387da0) 成功且非空时将返回值传给 LoadConfig；失败或空值传空字符串。两条路径传入的 isFail 都是 false。
4. ConfigManager.LoadConfig (0x2369c20)：非 A 且输入非空直接解码输入；IsA=true 强制清空远程输入。
5. 输入为空时：非 A 基名 GoldenDragon，A 基名 GoldenDragonA。isFail=false 用 PublishSDK.IsBackup；true 选 _default，false 选 _organic。
6. 显式 isFail=true 分支改读 GameDataManager.GetIsBackUp (0x236a01c)：PlayerPrefs config_type 默认为0，等于0返回true，否则false。因此0选_default，非0选_organic。不能将远程请求失败直接等同于这个分支。
7. SROptions 和 GameDataManager.SetIsBackUp (0x237049c) 存在切换 config_type 的调试路径。

## 版本分类

- cp_default_1：已确认原版启动请求的远程键；已有设备缓存，普通触发频率。
- GoldenDragon_default：远程输入不可用且 IsBackup=true 的非A包内分支；高频参数。
- GoldenDragon_organic：远程输入不可用且 IsBackup=false 的非A包内分支；普通频率。
- GoldenDragonA_default / GoldenDragonA_organic：代码可拼出这两个资源名；目前未取得对应资源或证实进入A的生产条件。不能算作已验证可运行版本。
- cp_default / cp_test：设备缓存包含这些键，但此次确认的生产启动方法不请求它们。不能据此称为国家版本或自动分流版本。
- iaa_v2_config_2：缓存记录的匹配规则名，不是已确认的独立玩法配置入口。

## 边界

PublishSDK.IsBackup 在本地托管层继续转发平台实现；未确认其服务器判定条件。缓存 matchedRuleNames 是匹配结果，不包含完整受众规则，无法从 B/default/organic 命名推断国家、安装来源、活跃天数或概率分桶。当前项目的六个 GM profile 是重建时的本地比较入口，不是原版六个版本。

证据目录：C:/Projects/Nut Sort Relax/reconstruction/mumu-current/delivery/Code/Gameplay；SDK转发：native-functions/dependency/3f76ef0.c；缓存来源记录：Assets/StreamingAssets/RecoveredConfig/Remote/provenance.json。
