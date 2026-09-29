# MORE WILD 增强概率与次数修复

日期：2026-09-16。工程：`C:/Projects/com.sfflogdstudio.dragonlegend`。

## 修复内容

此前混合配置中的普通 WILD 与 MORE WILD 五列权重相同，领取后没有概率提升。入口还先扣库存，再将扣后的库存传给盘面生成，导致第 5 次由 1 扣至 0 后使用普通权重。

- 当前启动混合配置仅将 `Gimrol.MorgKilp1..5` 替换为原 `cp_test.json` 的增强表；普通转轴、普通 WILD、Scatter、Bonus、免费转、奖励金额和提现参数均未改。
- `RecoveredSpinEntry.TryBegin` 捕获扣减前的 `moreWildForSpin`，当前盘面用它选择权重；库存照常减 1、刷新显示并保存。
- 赠送保持 5 次。领取显示 5/5；五次接受的转动依次使用 5、4、3、2、1 的增强状态，第六次用 0，回到普通。第 5 次开始时剩余计数变 0、计数条隐藏，但已开始的这一转仍增强。
- 弹窗提示与主界面分母均读取 `GetMoreWild()`，分子读取实际剩余库存，没有硬编码的 5。测试同时验证配置为 4 次时，也恰好生效 4 次。
- 老存档若还剩 1 次，现在这一转仍能获得增强；无需清除存档。

增强是提高 WILD 出现概率，并非每转必出整列龙头。原有新手保底、周期保底和盘面后续放置流程继续沿用。

## 验证

Unity 2022.3.62f3 PlayMode 定向测试 25 项全部通过，包括新增 MoreWildBoostTests 和既有 SpinEntry、MoreWildClaim、SpinResult、SlotBoard 测试。

新增用实际领取流程、旋转入口和生成后的 Board 验证全部 5 次（以及 4 次配置）、最后一次生效及下一次恢复普通；忙碌、无转数和正在生成结果的重复请求均不扣库存。测试只使用本地配置和内存玩家记录，不加载 GameEntry，不调用广告服务，不清除玩家存档。

配置生成器 `--check` 通过；与修改前 JSON 的差异仅上述五个字段。HybridGameplayValidation 另验证普通事件权重、24 个固定盘面奖励、五档提现及超过 60 次继续转动兼容性。

结果：`Tools/Evidence/more-wild-boost-validation.json`、`Tools/Evidence/hybrid-gameplay-validation.json`。
测试原始结果：`Artifacts/morewild-boost-tests.xml`。

本次未重新生成或安装 Android APK；旧 APK 不包含修复，需要重新导出和打包。
