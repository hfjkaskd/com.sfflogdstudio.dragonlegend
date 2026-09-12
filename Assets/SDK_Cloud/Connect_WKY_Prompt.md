执行 WKY SDK 接入，直接修改目标 Unity 工程并完成验证。

必要信息：AI接入时，只允许对文件的迁移，国家数量的设置，以及 CommonConfirmTipsPanel 中语言同步，其余内容只能调用不允许修改

源 SDK：`<源项目路径>`
目标工程：`<目标 Unity 项目路径>`

1. 先扫描目标工程。若已有同一套 WKY SDK、脚本或程序集，立即停止并报告冲突，不覆盖。

2. 无冲突时完整迁移并保留全部 `.meta` 和 GUID：

   * `_Bizza_BuildTemplate/` → 目标根目录
   * `Android/` → `Assets/Plugins/Android/`
   * `StreamingAssets/` → `Assets/StreamingAssets/`
   * 其余内容 → `Assets/SDK_Cloud/`

   `StreamingAssets` 同名文件必须覆盖，运行时直接使用迁移后的 `ChannelConfig.bytes`，禁止修改或编造参数。

   具体参考
   ```
   目标项目/
├─ _Bizza_BuildTemplate/
│  └─ Android/
│     └─ unityLibrary/
├─ Assets/
│  ├─ Plugins/
│  │  └─ Android/
│  ├─ StreamingAssets/
│  │  └─ ChannelConfig.bytes
│  └─ SDK_Cloud/
   ```

   需要对一个预制件 `CommonConfirmTipsPanel.prefab` 的 addressable key 进行补充为 "SDKPanel/CommonConfirmTipsPanel";

3. 创建运行时程序集 `WKY_SDK`，补齐必要引用；

4. Obfuz 缺失时通过 Git UPM 安装：`https://github.com/focus-creative-games/obfuz.git`
   - 记得将 宏`WKY_SDK` 添加到 Obfuz 的设置中，避免打包报错

5. 检查项目中是否有如下 Integration Manager：Bigo Ads、Bytedance、Fyber、InMobi、Mintegral、Moloco、Unity Ads、Vungle、Google。
   如果缺少或超出，不需要补充，只需要在执行完后向用户汇报

6. 原样迁移 `AndroidManifest.xml`，仅将 Android applicationIdentifier 设置为：

   `com.aqwad.coinsort`

7. 默认按测试版接入，开启：

   `BIZZA_REAL_WITHDRAW`
   `BIZZA_ENABLE_MAX`
   `BIZZA_ENABLE_ADJUST`
   `DEBUG_MODE`
   `COMMONGAME`
   `WKY_SDK`

   默认关闭 `BIZZA_HTTP_AD`。

8. 使用现有 `Boots_Flow.Init()` 初始化，只调用一次并等待完成。顺序：

   配置 → 设备信息 → 登录/用户信息 → 广告/归因 → 主流程。
   参考调用方式
   ```
   var flow = new Boots_Flow();
            await flow.Init(); 
   ```

9. 找到统一广告控制器，将激励和插屏底层接入：

   `BizzaSdk.Ad.ShowRewardAd`
   `BizzaSdk.Ad.ShowInterAd`

   保留原有埋点、奖励、提现和业务回调。

10. 把WKY SDK的国家同步到非 WKY；就是说 非 WKY 支持多少个国家 WKY SDK 也支持多少个国家；并且在 CommonConfirmTipsPanel 中同步一下语言内容，同时在接收到服务器的语言后同步到游戏中的语言设置中。AI还需要记得在 DeviceNativeBridge.cs 脚本的 SupportedCountryNames 字段中添加，保证国家的使用

11. 文件迁移、依赖扫描和修改批量完成；全部修改后只启动一次 Unity，统一完成导入、依赖解析和编译验证。

完成后报告：迁移文件、程序集、依赖、MAX 渠道、宏、初始化入口、广告入口、applicationIdentifier、编译结果和阻塞项。不要统计阶段耗时。
