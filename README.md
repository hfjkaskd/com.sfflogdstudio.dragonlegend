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
