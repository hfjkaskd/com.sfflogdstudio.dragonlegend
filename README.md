# Dragon Legend 复刻工程

Unity 2022.3.62f3。使用 Unity Hub 打开本目录。

当前入口：Assets/Whitebox/Scenes/GameEntry.unity。运行后加载配置；标准 Button 提供默认 US 本地测试配置与对照配置切换。地区和 AB 标签属于本地测试选择，服务器完整分流规则尚未验证。此场景是配置启动入口，并非原版主界面。

SDK 模拟测试入口：Assets/Whitebox/Scenes/MockFlow.unity。现有 SDK mock 未修改。

## 本轮进展

- 完整玩家记录与六类子记录已按原版字段恢复（7 类共 49 字段），采用原版 JsonUtility + PlayerPrefs、存档键 playerData.d。保留新建/读入/损坏恢复状态 0/1/2，以及初始化回调后保存顺序。
- GameEntry 加载完整存档，首次/恢复时初始化；RecoveredPlayerProgress 直接操作同一记录，已有外围系统字段随完整记录保存。
- 新增存档往返、损坏恢复和入口存档保护测试。源码字段核对通过；本轮 C# 编译阶段完成，但 Defender 隔离生成的 Whitebox.Runtime.dll（Trojan:MSIL/Quasar.MG!MTB），Unity 在复制程序集时退出，新增测试尚未运行。未关闭防护、添加排除或恢复隔离文件。此前 47 项通过结果不覆盖本轮新增实现。
- 主按钮、完整旋转表现及全生命周期仍待接入；存档运行验证待安全检测问题解决后继续。

- 恢复 GameData 的 SpinCount、LevelExpCount、MoreWild 属性语义：次数保存后通知原始请求值，经验超阈值仅升一级并清零、事件报告旧阈值，MoreWild 通知在保存前。存档操作由调用方提供，避免局部字段覆盖完整玩家记录。
- 恢复最大次数、等级经验需求和评价触发等级 getter。最新 Unity PlayMode 47 项全部通过。
- 核实实际主旋转在 UIMainView；RunState/StopState 方法原生体只抛出 NotImplementedException，不应作为有效流程移植。按钮与完整存档消费者仍待接入。

- 已恢复 Bonus 区域保底，包括候选索引直接作为行号、区域补足禁选列的原版分支。
- 已串联 InitGameResult：引导/保底 Wild、普通 Wild、Bonus 双次计数与保底、强制免费 Scatter、结算、停轴参数与有序回调。配置入口随配置创建生成器。
- GetCoinSpinAmountWin 保留移除第一项权重后返回索引、不加一的原版实现。
- 最新 Unity PlayMode 41 项全部通过，含真实 US 配置连续 12 次生成。结果生成已接通；扣次数、余额入账、原版状态机、可视转轴与奖励窗口仍未接通。

- 恢复 CheckSingleSymbol（0x2384d2c）的 Bonus/Scatter 单符号放置：调用方列列表、占位更新、五列终止和重试时累积候选行的原版行为。保留由旧候选行造成的覆盖结果。
- 每步只执行一次列抽样，可由后续旋转驱动分帧推进；不改变随机调用顺序。候选列表和随机委托复用；极端连续拒绝仍会增长候选列表，与原版语义一致。
- 最新 Unity PlayMode：37 项通过，0 失败。完整旋转编排、Bonus 区域保底和视觉入口仍未完成。

- 恢复基础棋盘列优先取样、普通/MoreWild 行替换、保底 Wild 列替换，并可直接交给结算与停轴规则。保底权重返回索引映射为 0→4列、1→3列、2→5列；保留普通 Wild 与保底 Wild 的占位记录差异。
- 随机排序保留 System.Random.Next 键排序与稳定相等键顺序，使用固定缓冲替代 LINQ 临时集合。
- 最新 Unity PlayMode：32 项通过，0 失败。Bonus/Scatter 放置、保底计数器及完整单次旋转编排仍未接通，未新增伪造主玩法入口。

- 恢复 WinTotalLine 的逐列路径扩展、Wild 替代、已扩展前缀过滤、开头 Wild 赔率覆盖和整数乘法后除线数；保留原版特殊符号列终止边界。输出路径坐标供后续动画消费者使用。
- GameEntry 随配置创建结算实例，切换与停用时清理旧实例。结算仍待接入结果生成、转轴与奖励界面。
- 结算缓冲复用，原版 5x3 棋盘最多 363 个中间路径节点、243 条完整路径；避免每次结算动态创建路径对象。
- 最新 Unity PlayMode 验证：24 项通过、0 失败，含配置切换生命周期检查。

- 新增按 ARM64 指令核对的 SlotGameResult 停轴触发列、连续前缀 Wild 判定与最低正奖励回写规则；保留第零列 sentinel 行为。尚未接入主玩法视图，不代表完整结算已完成。
- 本轮 Unity 2022.3.62f3 PlayMode 共 18 项测试通过，0 失败（含新增 11 项规则边界测试）。
- Dragon Legend 主流程以逆向资料 mumu-current 为来源；上层 il2cpp 目录包含另一款 Nut Sort 的导出，不能混用。

- 创建工程和 main 分支，已推送基础版本 304bca8。
- 新增配置驱动的启动 Prefab、Scene 和 LaunchProfile，事件通过代码绑定。
- 移植首批配置消费者：初始余额/次数、下注档位解锁、转轴权重、支付倍率、Scatter 索引以及提现阶段参数。
- 保留原版权重比较边界与 getter 分支，不擅自换算提现金额。
- 配置切换取消旧加载并释放请求；各平台使用同一加载流程。
- 在 Unity 2022.3.62f3 中编译和生成场景成功；7 项 PlayMode 测试通过，0 项失败。测试代码位于 Assets/Whitebox/Tests。

## 尚未完成

主玩法可视转轴与完整结算、引导、奖励分支、外围系统解锁及原版 UI/动画/场景视觉对齐仍未完成。首批规则尚未接通所有主流程消费者。不能宣称已经完整 1:1 复刻。

ReferenceOriginal 中的原始 Prefab 和场景是参考文件，仍有待转换脚本/组件，因此保留在 Assets 外。已导出美术可在 Assets/Resources/RecoveredArt 中按路径加载。

下一优先级：恢复主循环和结算，再接入原版布局及事件分支；此前不扩展边角系统。SDK 继续保持现有处理方式。

## 开发约束

遵守 AGENTS.md，只使用官方 Unity 包和 Prefab 驱动结构。核心玩法对象使用 SpriteRenderer/Mesh。大资源避免启动时统一强引用。

动画推荐逐项用 Animator/AnimationClip 和官方 2D Animation 验证。全量高分辨率逐帧烘焙会增大包体、贴图内存及加载峰值，目前未采用。

Library、Temp、Logs 和本地 Artifacts 不提交。本轮不追加原始逆向证据、配置真值表或缓存来源元数据到仓库。

## 本轮：付费旋转入口的数据阶段

按 UIMainView.OnClickButton 0x23bd4e8 恢复次数检查、首次引导推进、LimitSpinCount 与已保存里程碑、扣次数/加经验/奖池/银行的调用及存档顺序、先消耗 MoreWild 再生成结果，以及 default 配置满次数首次消耗时写入恢复时间。GameEntry 加载后创建 SpinEntry 并共享完整玩家记录与结果生成器。银行 setter 保留阈值通知先于保存、重复赋值重复通知且不钳制的行为。

当前 SpinEntry 暴露视图事件，尚未绑定主玩法 Prefab；转轴动画、奖励分支和生命周期闭环仍未完成。SDK 未修改。新增 5 项测试覆盖真实配置下的首次旋转调用顺序、忙碌/次数不足保护与银行边界。本轮仅完成原生指令和源码核对，未运行新增测试：此前 Unity 生成程序集被 Defender 隔离，安全状态尚未解决。上一轮存档测试同样仍未运行；不能把历史 47 项通过视为当前版本通过。

## 本轮：奖池/免费旋转分支与任务进度

核对 ClickSpin 的顺序：Bonus 动画、Wild 列动画、奖池、普通中奖动画、Bonus 游戏、免费游戏、基础结束。恢复其中的奖池和免费游戏判断及任务记录变化：所有完整 Wild 列参与奖池计数，3/4/5 列分别对应 minor/major/grand；免费次数直接取 Scatter 索引配置，只有正数才更新任务。GameEntry 提供 RewardBranches 供后续呈现阶段调用，尚未接通奖励弹窗和完整执行链，不代表视觉流程已恢复。

SetTaskData 保留首次固定 count=1、后续增量及上下限、已领取只保存、空列表只保存、未知配置错误等原始行为；任务配置缓存按原顺序构造。新增 8 项边界测试，因此前 Defender 隔离 Unity 程序集，本轮未编译/运行，只完成源码及 ARM64 对照和 git diff 检查。SDK 未改动。

## 本轮：恢复渲染管线和场景显示基础

原版 GraphicsSettings/QualitySettings 使用 URP，而此前复刻工程仍使用内置管线。已从完整恢复的 MonoBehaviour 序列化数据重建六套 PipelineAsset 和六套 ForwardRenderer（普通导出对应文件仅有空壳），保留各质量档的渲染参数及原始档位映射，Android/iPhone 默认 Medium。Shader 按原名绑定官方包，重名蓝噪声纹理再按原尺寸核对；PostProcessData/XRSystemData 引用官方包资产。本机 Unity 2022.3.62f3 自带 URP 14.0.12，依赖已由 Package Manager 解析并保存到 packages-lock.json。[Unity 官方兼容说明](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/requirements.html)

入口场景恢复原版 UI 基础相机与世界叠加相机、相机栈、UI 根坐标；入口 Prefab 改用相机 Canvas、1080×1920、宽度匹配、原平面距离和 Shader channels，并统一 UI Layer。环境光/烘焙设置及空 LightingData 取自当前逆向场景，LightingData 重绑复刻场景。旧 BuildGameEntry 入口改为打开已保存场景，不再重新生成覆盖布局。

验证：场景 117 处本地引用检查通过；17 个场景/Prefab/渲染配置文件外部 GUID 均可解析。正常 Unity 测试重试仍被 Defender 阻止读取 Whitebox.Runtime.dll；加入官方 URP 后编译后的 IL 处理同样在读取该程序集时被阻止，未发现新的 C# error CS 诊断。当前未完成 Unity 场景导入、运行或最新画面对照验收，不能据静态检查宣称视觉 1:1。主界面 Spine 转换、转轴、弹窗动画和完整玩法接线仍未完成，SDK 未改动。

## 本轮：主界面字体与富文本依赖

接入官方 TextMeshPro 3.0.9（Package Manager 已解析并锁定）。恢复主界面 18 个 TMP 文本所依赖的 Quorum SDF 字体、材质预设、内嵌图标字体；从完整序列化数据恢复普通导出中的空壳 TMP Settings、默认样式表和 cash/free/spin 渐变。资源目录遵循原版运行时查找路径。字体文件的源 GUID 字符串与实际导入 GUID 对齐；官方 TMP Shader 及 includes 从包内 Essential Resources 原样引入，并保留许可文件。[官方资源说明](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)

修正 Quorum 图集的导入设置为原版 1024×1024、可读、无 mipmap、线性 Alpha8、无压缩。只修改导入参数，没有重画图集。静态核对：85 个字形记录保留；PNG Alpha 通道与原始 Alpha8 数据的 1,048,576 字节在坐标行方向转换后逐字节一致；16 个文本配置/材质文件的外部 GUID 无缺失。

本轮未接入完整主界面 Prefab，也未完成字体在 Unity 中的画面验收。包解析时 Unity 仍因 Defender 阻止读取 Whitebox.Runtime.dll 退出；这不属于测试通过。此前完整玩法、动画和视觉对齐的未完成范围继续有效，SDK 保持不变。

## 本轮：Bonus 游戏入口的数据阶段

按 UIMainView.CheckBonusGame 0x23c6d54 和谓词 0x23bf8d8 补齐遗漏分支。区域完成条件为每项大于 1，或 GameData 已有 IsBonusGame 标记；保留空列表触发、null 列表报错的原始边界。标记为运行时状态，不加入 PlayerData 存档字段。进入时先更新任务 5 并保存旧区域，再清标记、替换五项零值区域并再次保存；原列表不被就地清空。

RewardBranches 提供 CheckBonusGame，供普通中奖动画之后、免费游戏判断之前调用。NPC、音效、Bonus 转场与小游戏视图仍未接线，完整主循环仍未完成，SDK 未变。新增 6 个测试用例覆盖阈值、已有标记、两次保存的可观察顺序和空/null 区域；本轮只完成原生指令及源码对照、diff 检查，Defender 阻塞未解除，未运行 Unity 测试，不能声明当前版本测试通过。
## 本轮：恢复运行验证与 Bonus 区域累积

当前 Defender 实时防护状态已变为关闭，正常 Unity 2022.3.62f3 编译和 PlayMode 测试恢复。先对提交 7b6e561 完成 69 项测试，全部通过，覆盖此前未运行的存档、付费旋转入口、奖励分支与 Bonus 入口用例。

补齐 GameData.SetBonusArea 0x236f868：指定列当前值大于 1 时直接返回且不保存，否则递增后保存；保留负值及非法索引的原始边界，不提前设置 Bonus 游戏标记。新增 8 个用例，包含最后一列累积完成后进入 Bonus 分支并重置的串联验证。最新完整 PlayMode 结果 77 项通过、0 失败（本地 Artifacts/bonus-area-tests.xml），不是完整视觉或生命周期验收。

保留官方 URP 首次成功导入生成的全局配置、GraphicsSettings 引用、调试输入轴、ShaderGraph 设置及 MonoImporter 元数据。未修改 SDK；主玩法 Prefab 接线、转轴动画、Bonus 转场和小游戏等仍未完成。
## 本轮：免费游戏入口运行状态

按 CheckFreeGame 0x23c90bc 补齐任务更新之后的数据顺序：任务 3 保存，发送原事件 7 对应的提现任务刷新参数 (5,1)，随后 GameSlotType=Free、FreeSpinCount=配置次数、TotalFreeSpinWin=0。这些是 GameData 运行时字段，不写入 PlayerData；原顺序不追加保存。未触发时保留已有免费状态。事件消费者名由 UIMainView.OnInit 的监听绑定交叉核对。

新增顺序和未触发保护用例，Unity 2022.3.62f3 PlayMode 全部 79 项通过、0 失败（本地 Artifacts/free-entry-tests.xml）。这仅证明现有规则及入口数据阶段；免费开场弹窗、Scatter 动画、自动旋转及返回基础游戏的生命周期仍未接通。SDK 保持不变，完整 1:1 尚未完成。
## 本轮：免费旋转结果生成器

恢复 FreeSlotGameResult.InitFreeGameResult 0x238265c、RandomSymbolInfo 0x2382ad0、CheckSingleSymbol 0x2382b24，以及免费金币数量、龙珠数量与类型的三个配置 getter。顺序为先取数量、列优先均匀生成底盘、清理共享占位、金币 9 放置、龙珠 11 放置，最后列优先生成所有龙珠类型。包含底盘本身出现的龙珠；类型权重索引非 0/1 时保留原版映射到 2。

采用复用棋盘和固定候选行缓冲，每步最多一次列重试，避免原版随机重试在超额配置下卡住主线程；不静默截断配置数量。GameEntry 在配置生命周期创建/清理该实例。自动免费旋转入口、表现调用与结束分支仍未接通，本轮不宣称完整循环已完成。

新增 4 项用例，对照直接移植的列表式放置逻辑检查棋盘、类型序列和后续随机数（含满盘），并验证超额配置保持待完成且每步可返回。Unity 2022.3.62f3 全量 PlayMode 83 项通过、0 失败（本地 Artifacts/free-result-tests.xml）。主界面、动画和完整生命周期仍待恢复；SDK 不变。
## 本轮：自动免费旋转入口接结果生成

按 UIMainView.FreeAutoSpin 0x23bf158 及完成回调 0x23bf7d0 恢复：免费次数不足 1 时返回，否则先扣运行时次数，再生成免费结果，完成后请求 AutoFreeSpin 表现。不会扣普通次数、增加经验/银行/奖池，也不保存 PlayerData；生成异常不退款。为保持原版同步生成的串行语义，逐步生成期间拒绝重复开始。GameEntry 创建并清理入口与结果实例。

新增 4 项用例，验证空次数早退、扣次与表现回调顺序、重复调用、普通玩家记录不变及生成失败边界。Unity 2022.3.62f3 PlayMode 全量 87 项通过、0 失败（本地 Artifacts/free-spin-entry-tests.xml）。表现请求仍待绑定转轴 Prefab，奖励、结束弹窗和回到基础游戏的完整循环未完成。已确认原版结束判断是次数等于 0，待后续接入；SDK 未修改。
## 本轮：免费旋转结束与返回基础模式的控制顺序

恢复 CheckFreeSpinEnd 0x23ca10c：次数恰好为 0 时先将模式改为 Base、暂停音乐，再请求 UIFreeSpinEndView（传入初始免费次数）；等待结束视图完成后请求 transform 音效和转场。按 0x23bf600 的转场事件顺序请求恢复基础界面和初始化转轴，按 0x23bf618 的转场完成顺序请求清除免费结束标记及 normalBg 音乐。非零次数交给 FreeSpinEntry，负值不会打开结束弹窗。GameEntry 管理此控制实例的生命周期。

控制层明确等待实际视图/动画回调，当前未绑定完整 Prefab，不能把请求事件视为视觉实现完成。新增 3 项用例覆盖等待、调用顺序、初始次数传递、正/负次数分支；Unity 2022.3.62f3 全量 PlayMode 90 项通过、0 失败（本地 Artifacts/free-exit-tests.xml）。免费奖励结算、主玩法表现及完整游戏生命周期仍待完成；SDK 保持原处理方式。
## 本轮：余额入账 setter 与里程碑存档顺序

恢复 GameData.set_GreenCount 0x236d788：先通知旧余额/请求值，通知时存储仍是旧值；随后写入余额、检查 GreenCountLog，再执行两次保存（CheckGreenCount 及 setter 各一次）。不钳制负值，每次赋值最多推进一个里程碑，余额不变也仍通知和保存。阈值 20000/40000/60000/80000 从 Dragon Legend 原始 ELF 的 0xdbc73c/0xdbc634/0xdbc5ec/0xdbc740 读取，并核对方法签名字节。保留原生 b.lt 的 NaN 比较边界。SDK Track 不接入，里程碑存档字段保留。

新增 10 项用例，全量 Unity 2022.3.62f3 PlayMode 100 项通过、0 失败（本地 Artifacts/green-balance-tests.xml）。余额写入接口尚待绑定各奖励的原始完成阶段；已核对 PlayFlyCoin 0x23c306c 为先外部回调、再重置顶部、最后入账，后续必须保留。主流程完整奖励表现和 1:1 视觉仍未完成。
## 本轮：飞币完成后的奖励入账

恢复 PlayFlyCoin 完成回调 0x23c306c：先执行可空外部回调，再调用顶部重置，最后读取当时的余额加上奖励并走 SetGreenCount。不得提前缓存余额；任一回调抛错时不继续入账。保留重复回调重复入账、负奖励不钳制的原始行为，动画消费者负责按实际完成事件调用。

新增 5 项边界与顺序用例，Unity 2022.3.62f3 PlayMode 全量 105 项通过、0 失败（本地 Artifacts/fly-credit-tests.xml）。此入口通过 GameEntry.RewardBranches 可供视图调用，尚未绑定真实飞币 Prefab；飞行路径、数量、时长及完整主流程表现继续待恢复。SDK 不变，完整 1:1 尚未达成。
## 本轮：主界面余额与等级原始显示 Prefab

从原版 UIMainView 的 Top 子树恢复 BalancePanel，保留 Topbg、Cash、Level 的原布局、颜色、TMP 字体材质及 Image 参数；移除依赖旧程序集的 TopTitle 脚本，Image/TMP 绑定官方组件。尚不包含提现、帮助、设置和 Spine 动画，也未绑定运行时余额/经验刷新；显示文本仍为原 Prefab 初值。

修复已导入 PNG 但缺失独立 Sprite 资产导致的白块：原样恢复六个 Sprite 及 GUID，保留原裁切、网格、UV、pivot 和九宫格边界，纹理引用逐一核对现有原始 PNG。测试使用 Unity 对象空值判断检查 Sprite、纹理及字体，避免 NUnit 普通非空断言遗漏缺失对象。预览采用官方 URP 渲染请求、UI Layer 5、1080×1920 相机 Canvas。

Unity 2022.3.62f3 完整 PlayMode 106 项通过、0 失败（本地 Artifacts/balance-sprites-tests.xml）。已检查本次生成的 Artifacts/current-balance-panel.png：背景、图标、文本及进度条可见，原白块已消失。这是顶部显示片段验收，不代表完整主界面或游戏生命周期 1:1；后续仍须接入原生刷新及动画。SDK 未改动。

## 本轮：顶部余额/经验绑定真实玩家数据

BalancePanel 接入 GameEntry 配置及存档加载完成阶段，实例化已配置的原显示 Prefab，并订阅当前 PlayerProgress；GM 切换及入口停用时解除订阅、销毁旧面板。静态层级、字体和布局均保留在 Prefab，未用代码构建。LaunchProfile 增加原 LanguageType 数值配置（EN=0、BR=1），现有默认 US 仍取 0；未推断未知国家分流。

恢复 TopTitle.Init 0x23b9018：余额格式化、等级文本、经验/需求及 fillAmount。CurrencyUtils 0x238c4e4 先除以 100，按 EN/BR 取币符和小数分隔符；非零语言走 BR，保留 decimals=0 的原 invariant 分组行为。运行时原格式因此是 $1789.00 和 8/10，与 Prefab 初始示例 $ 1,789.00 和 80% 不同。

恢复余额变化 0x23b9480 的 0.5 秒滚动，打断时从事件旧余额重新开始；原 DOTween .cctor 0x241a014 的 0x241a0e4/0x241a0fc 将 defaultEaseType 写为 6（OutQuad），由 Prefab 中 Unity AnimationCurve 等价表达。经验变化 0x23b92ac 使用 0.5 秒线性填充，不取消前序填充；完成回调 0x23b9694 仅在原始比值恰为 1 时更新等级、清空进度并读取当时等级的新需求。使用原生 Update 和复用动画记录集合，不引入第三方 Tween 程序集。

新增 8 项格式/动画/重绑定用例，完整 PlayMode 114 项通过（Artifacts/balance-binding-tests.xml）。本轮最新 current-balance-panel.png 使用实际绑定数据生成并已查看，确认金额、8/10 文本和 80% 填充显示。真实入口与版本切换的额外绑定断言专项 1 项通过（Artifacts/balance-entry-tests.xml），确认当前数据格式、旧面板释放及单一新面板。完整主界面、顶部飞币重挂父节点、转轴、奖励转场和完整生命周期仍未完成，SDK 未改动。

## 本轮：核心转轴符号资源与原生显示组件

从原 Main.unity 的 GManager.SymbolInfos 恢复 11 个符号的原顺序、ID、清晰/模糊 Sprite 路径和特效来源路径。按 InitSymbols 0x2370fa4，基础池移除索引 8，得到 0..7、9、10；免费池保留前七项 0..6。ScriptableObject 保存路径，Sprite 按需加载并缓存；特效路径仅保留来源，未将尚未转换的 Spine 资产伪装为已实现特效。

恢复 22 个清晰/模糊 Sprite 及一个遮罩 Sprite，保留原 GUID、网格、UV、裁切与 PPU，所有纹理 GUID 均对应工程现有 PNG。单独放在 RecoveredSymbols/Sprites，避免按 Resources 路径加载时与同名 PNG 混淆。

SymbolItem 为普通 Transform + SpriteRenderer Prefab，使用官方 URP Sprite-Unlit-Default 材质，遵循项目禁止 UI 承载核心玩法对象的规范。原版 SymbolItem 的底部 pivot 和子图居中对应 100 像素/单位下的子节点 y=0.86。恢复 SetImg 0x2374a60 的基础清晰/模糊切换（模糊缩放 2 倍）、基础 hide 遮罩及免费模式仅 blur 控制遮罩、主图始终清晰的规则。原实现使用 Image，此处按工程规范改由 SpriteRenderer 承载，并非照搬原组件类型。

Unity 2022.3.62f3 全量 PlayMode 117 项通过、0 失败（Artifacts/symbol-view-tests.xml）；新增用例检查符号顺序、资源、PPU、显示切换、无 UI 组件以及每个渲染区域可见。已查看当前工程生成的 Artifacts/current-symbols.png，22 种清晰/模糊呈现均正常。

原 RollReel 构造方法 0x23775c4 确认每列七个槽位、间距 172；Init 0x23747c4 为逐槽均匀随机填充，尚待接入七槽复用、裁切、滚动、停轴回弹和结算结果落位。本轮符号 Prefab 未接到实际转轴控制器，完整主流程及视觉 1:1 仍未完成。SDK 未改动。

## 本轮：七槽转轴循环与原生裁切

新增 RecoveredReelView 和 Reel Prefab，按 Init 0x23747c4 初始化七个复用符号，172 像素间距；原 RotNode 高 516、符号底部锚点换算为 -258 像素起点。参数存于 Prefab，首次实例化 SymbolItem 后，换圈及重新初始化都复用这七个对象。

按 ARM64 RefreshSymbol 0x2375630 和 SetSymbolPos 0x2374f4c：先减去 speed×deltaTime，仅在严格低于 -688 时回移 688，一次调用最多回移一次；保留大帧位移后仍越界的原行为。换圈先请求效果、金币、龙珠清理，再把旧槽 4/5/6 搬到逻辑槽 0/1/2，随后按原顺序随机四项。免费模式初始每两圈触发一次 CheckFakeCoin 请求，此后间隔按 Random.Range(4,6) 重取。清理和 FakeCoin 请求尚待绑定实际特效池，不代表这些特效已完成。

原 188×518 矩形窗口使用 SpriteMask，遮罩 Sprite 来自原始 White1px。手写不完整 SpriteMask YAML 曾导致导入崩溃；已改由 BuildReelMask 的 UnityEditor 官方 API 保存完整渲染器及内置材质引用，正常导入成功。该脚本仅用于资产制作，运行时和真机均使用保存好的原生 Prefab，不存在 Editor 核心逻辑兜底。

修正后 Unity 2022.3.62f3 全量 PlayMode 120 项通过、0 失败（Artifacts/reel-cycle-verified-tests.xml）。新增测试核对初始随机、接续槽位、后续随机数、严格边界、大位移单次回移、对象复用及免费循环间隔。已检查本次生成的 Artifacts/current-reels.png，五列按原间距裁切显示，测试同时验证窗口外不可见、每列窗口内有实际符号像素。首次崩溃运行不计为通过。

本轮转轴可接受逐帧 Refresh 驱动，但还未绑定主 Spin 链路；加速、停轴减速/回弹、真实结果落位和奖励表现仍待恢复。完整 1:1 目标继续有效，SDK 未修改。

## 本轮：基础转轴加速、结果落位与回弹

Reel Prefab 接入 RecoveredBaseReelMotion，按 StarSlotSpin 0x2375c54 的 Already Start 重入保护、OutSine 加速、0x2377878 的清晰模式 Refresh 及 0x2377898 的立即 StartCoroutine 行为恢复启动。加速完成当帧执行一次匀速 Refresh，不把这次位移丢到下一帧；匀速换圈使用模糊模式。最大速度可按原 SetMaxSpeed 独立更新。

按 ConstantSpeedRoll 0x2377bf4 的基础分支，遇到停轴标记先 yield 一帧，随后才取得当前列结果。ApplyBaseColumn 将结果行依序写到前三槽，保留其余槽的原 ID，并让七槽全部恢复清晰图，不额外消耗随机数。结果提供者失败时终止该阶段，不在后续帧反复重试，也不伪造完成标记。

StopSlotRoll 0x2375fbc 以 abs(offset)×2/maxSpeed 计算返回时长，使用 OutBack；完成回调 0x2377810 同时清除 isStartSpin 和 isStop。overshoot 参数由原 ELF 0xdbbce8 读取为 1.7015800476074219，保存于 Prefab。数学缓动与 Update 均由 Unity 原生运行，无第三方 Tween 程序集。

新增三个顺序/边界测试，Unity 2022.3.62f3 全量 PlayMode 123 项通过、0 失败（Artifacts/reel-motion-tests.xml）。覆盖半程速度、同帧匀速位移、延迟读取新结果、尾槽保留、无额外随机、回弹越过零点、提前停轴、零距离返回和异常终止。

免费模式停轴、SetStop 外层延迟及完成通知、主 Spin 按钮与结果生成器的完整绑定尚未恢复，本轮不代表完整主流程或视觉生命周期 1:1。SDK 保持原处理方式。

五列 Prefab 运动后渲染专项 1 项通过（Artifacts/reel-landing-render-tests.xml），已查看本次 current-reels.png：五列经过加速、匀速、停轴等待、真实测试列结果写入和回弹后，三行顺序与目标列一致，窗口裁切正确。该画面是当前组件测试预览，尚不是已接通的游戏主场景。
