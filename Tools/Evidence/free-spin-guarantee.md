# 每次免费转奖励保底

2026-09-16，工程：C:/Projects/com.sfflogdstudio.dragonlegend。

用户要求每次免费转均有保底，避免 Scatter 活动结束显示 $0.00。

当前默认 cp_low_frequency_high_rewards.json 的 Rrggiomg.MinimumCoinsPerSpin 为 1。结果生成保留原金币随机抽样，将数量取原数量与保底数量的较大值：每次免费转至少生成 1 枚实际现金金币，自然抽到更多时保留原数量。单枚沿用 Ronig.QoinRgkorp=[100,1000]，即显示 $1～$10；当前配置每次免费转的保底奖励至少 $1。

金币经过现有停轮、扫描、奖励收集及余额入账链路，计入 TotalFreeSpinWin 和活动结束总额。保底不依赖广告、存档重置或 Editor 分支。只给当前混合配置启用；原版普通/高频快照缺少该字段时仍按原规则运行，用于原版比较。配置生成器同步了该参数，重新生成不会丢失。

## 验证

- Unity 2022.3.62f3 PlayMode：24 项通过，0 失败、0 跳过。结果：free-spin-guarantee-tests.xml、free-spin-guarantee-tests-status.json。
- 512 个配对随机种子验证：零金币变为 1 枚，更多金币原样保留，龙珠抽样不变，实际盘面符号数量与结果一致。
- 4/6/8/10 次连续免费转，每次均存在正金额金币。
- 集成测试固定旧抽样为 0 金币、0 龙珠，固定单枚金额为 $1。实际生产 Prefab 自动完成 6 次免费转；每次金币都被扫描并令余额增加，累计奖励恰为 $6，实际结束窗口显示 $6.00。结束窗口不重复入账。该 $6 是固定金额测试结果，不是线上每局固定奖励。
- 原有入场、结果生成回归测试通过；配置生成器 --check 通过。
- 本次没有改动既有 CoinReward 累计入账行为；上述余额检查确认每转增加，不用于证明历史累计入账算法的金额正确性。

测试使用临时场景与内存玩家状态，未清除用户存档。没有重新生成或安装 Android APK，设备端需重新打包安装才能生效。

验证入口留档于 Tools/Validation/ValidateFreeSpinGuaranteeTests.cs，需要复测时临时复制到 Assets/Whitebox/Editor，运行 Tools/Validation/Free Spin Guarantee Unit Tests，结束后移除临时入口。