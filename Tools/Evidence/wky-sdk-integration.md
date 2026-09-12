# WKY SDK 接入报告

状态：SDK/游戏侧接入代码、Obfuz 配置、Editor 与 Android 脚本编译、Addressables 构建及 8 个定向测试均通过。**原始弹窗资源仍缺失，视觉与真机端到端验收未完成。**

目标：`C:/Projects/com.sfflogdstudio.dragonlegend`
源 SDK：`C:/Users/pc/Desktop/PlayCloudSdk/PlayCloud_Flow`
执行提示词：`PlayCloud_Flow/Connect_WKY_Prompt.md`

## 已完成

- 接入前未发现同套 WKY 类型、程序集或 GUID 冲突。
- 迁移 217 个文件，保留源 `.meta` 和 GUID，迁移清单见 `Artifacts/WKYIntegration/migration.json`。
- `_Bizza_BuildTemplate` 到根目录；Android 到 `Assets/Plugins/Android`；StreamingAssets 到 `Assets/StreamingAssets`；其余到 `Assets/SDK_Cloud`。
- 安装 Obfuz 3.1.0，官方 Git UPM 固定提交 `0b6b8a92c06cf5bd35c2b34ed595356e1219c938`；packages-lock 已由 Unity 解析生成。Addressables 1.21.21、Newtonsoft.Json 3.2.1 亦已解析；已补齐 unitywebrequesttexture 内置模块。保留现有 MAX、Adjust、UniTask。
- 创建运行时 `WKY_SDK`；保留 Bizza.Common.DeviceInfo、Bizza.Common.UI、Bizza.UserInformationVerification.Contracts 独立边界；增加三个 SDK Editor 程序集隔离 Editor 代码；补齐必要引用。
- Android applicationIdentifier 已设置 `com.aqwad.coinsort`，启用迁移后的 Manifest/Main Gradle/Gradle Properties/Gradle Settings 模板。
- Android 与 Standalone 宏：`BIZZA_REAL_WITHDRAW;BIZZA_ENABLE_MAX;BIZZA_ENABLE_ADJUST;DEBUG_MODE;COMMONGAME;WKY_SDK`。未启用 `BIZZA_HTTP_AD`。
- `GameEntry.Load` 先等待 `WkyRuntime.InitializeAsync`，后进入原游戏配置和主流程；初始化任务共享，`Boots_Flow.Init()` 只调用一次；游戏侧未加入 Editor 绕过逻辑。
- `LocalAdFacade` 保留已有奖励/失败/任务/插屏策略回调，支持 `WkyAdTransport`；正式游戏入口使用 `BizzaSdk.Ad.ShowRewardAd` / `ShowInterAd`。模拟控件在该入口取消绑定且隐藏，不再自动发奖。屏蔽重复、过期和模拟成功回调；退出时取消待处理回调。
- 非 WKY 支持 US、BR；`DeviceNativeBridge.SupportedCountryNames` 同步为 None/US/BR。SDK 枚举保留原有序号以保护二进制配置格式。
- 此 SDK 用户信息提供国家 `Os_Cty`，没有独立 language 字段。通过 `InitContentByCountry` 事件将 US/BR 映射为游戏 EN/BR 和 `SelectedLanguage=en-US/pt-BR`。CommonConfirmTipsPanel 已包含这两种语言，无须改写原有翻译。
- 8 个定向 EditMode 测试全部通过，覆盖发奖回调、重复/过期回调、并发广告、国家映射和原始 ChannelConfig.bytes 的完整字节往返一致性。

## 源文件一致性

- `Assets/StreamingAssets/ChannelConfig.bytes`：与源文件 SHA-256 一致：`ce12e59183560fafcf1390340f2453dc9264cb9687bf8375cf3df15787c8ed03`。
- `Assets/Plugins/Android/AndroidManifest.xml`：与源文件 SHA-256 一致：`31da650ebb2fbae0759d02e6cf2f718ff61788d51a34180bf3d6e26940fb668b`。

迁移文件的授权修改：

- DeviceNativeBridge 的国家列表，Bizza.Common.UI.asmdef 的必要程序集引用。
- 用户后续明确允许移除两个缺失类型：删除 GameCoinRevenueToController 未使用事件；移除 GameAB_CustomData 相关运行时结构、配置编辑 UI 和日志输出。
- 旧二进制配置中的 AB 字节块继续保留为不解释的兼容数据；版本 1/2/3 的后续字段仍按原布局读取，版本 3 配置往返序列化字节一致。没有改写原始 ChannelConfig.bytes、Manifest 或 Prefab。

## MAX 渠道

已有：Bigo Ads、ByteDance/Pangle、Fyber/DT Exchange、InMobi、Mintegral、Moloco、Unity Ads、Vungle/Liftoff。
Google AdMob：用户明确要求跳过，不安装，不再列为接入阻塞项。无额外渠道；AppLovin 为核心 SDK。

## 编译与验证结果

- Unity Editor：编译通过，0 个 C# 编译错误。
- Android Player 脚本：通过，使用真正 Android 平台宏编译；结果清单 `Artifacts/WKYIntegration/android-compile.txt`。
- Obfuz：官方 Git UPM 已安装；`ProjectSettings/Obfuz.asset` 中包含 `WKY_SDK`，以及 Whitebox.Runtime/Assembly-CSharp 引用关系。已生成加密虚拟机、密钥和运行时初始化代码。未执行完整 APK 混淆构建。
- Addressables：本地 WKY SDK 分组已登记 `SDKPanel/CommonConfirmTipsPanel`，Android 内容构建成功。
- EditMode 测试：8/8 通过，0 失败，结果 `Artifacts/WKYIntegration/tests.xml`。
- 配置保护：ChannelConfig.bytes 与 AndroidManifest.xml 均与源文件完全相同；移除废弃类型后配置往返序列化仍字节一致。
- 验证过程中已修复缺失的 UnityWebRequestTexture 内置模块，以及接入配置工具的设置初始化和 Test Framework API 兼容问题。最终日志为 `Artifacts/WKYIntegration/unity-final-validation.log`；较早日志仅作故障排查留档。

## 剩余资源问题

源 CommonConfirmTipsPanel.prefab 还缺少 9 个 GUID 对应的图片、字体、材质和动画资源：

- `26370f0111f5ad64ab5d97c057ca3690`
- `365597e67a9bcf042bc71fc2b2dbaa16`
- `53e09fef452485e41b756c741a77977c`
- `68dde9efe07d2c44bbe2792cd56951ad`
- `7494cc5178f09174b8d1fb4f29abfacf`
- `7c262cc52fb985a4aac0bc3282234e94`
- `97ac0f6ca50a3134f95c0040ff4ae774`
- `a34c7acb122ed4f47801a5adb7febedb`
- `eb0ae6d80479c4346a57d74032d4b133`

完整源资源缺失，未使用其他图片或伪造 GUID 代替。

## 交付与验证边界

统一配置工具：`Assets/Whitebox/Editor/WkyIntegrationSetup.cs`。
阶段文件：`Artifacts/WKYIntegration/setup-phase.txt`（passed），不会在后续打开项目时反复执行配置。
详细审计：`Artifacts/WKYIntegration/audit.json`。
迁移清单：`Artifacts/WKYIntegration/migration.json`。
接入前相关文件副本：`Artifacts/WKYIntegration/before/`。

尚未生成和安装 APK，未验证真实设备登录、广告、归因或提现。源 SDK 弹窗仍需补齐原始资源才能完成视觉验收；此前缺失的两个类型已按用户授权移除，无须再提供。Google AdMob 无须安装。


## 默认国家修复

- 服务器国家缺失（用户信息不存在、null、空串或空白）时读取 ChannelConfig.real_CustomConfig.DefaultCountry。
- 国家与 UI 翻译分开处理：BR 使用葡语，其他暂无对应翻译的国家使用现有英文；不会再因国家码为空或没有对应翻译抛出异常。
- 实测当前 ChannelConfig 默认国家枚举值为 0（None）。没有修改配置文件或写死替代国家；此值使用现有英文界面。
- 新增默认国家、服务器优先和无对应翻译回归用例；Unity 编译通过，15/15 EditMode 测试通过，包括原始配置文件往返字节一致性。
- Play 检查不再出现 unsupported country 异常。SDK 的测试设备状态和缺少 UID 日志仍属于各自原有逻辑，本次未改写；本次未重新打包或验证真机。
