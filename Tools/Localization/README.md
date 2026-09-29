# 游戏文本盘点与翻译

文本资源：`Assets/Resources/Localization/GameText.json`。

当前接入英语和巴西葡萄牙语。启动时先使用上次保存的语言，SDK 返回账户国家后通过 `WkyRuntime` 同步：`BR` 使用 `pt-BR`，其他国家使用英语。设备语言本身不会覆盖已注册账户的服务端国家。

静态文字由 Prefab 上的 `LocalizedGameText` 绑定，动态金额、次数、倒计时和任务文案由原控制器调用 `GameLocalization`。语言变化通过事件刷新，没有逐帧查表。

每条记录使用稳定语义 `key`，`english` 保留游戏当前原文（包括大小写、空格、拼写、富文本标签和格式占位符），`portuguese` 为巴西葡萄牙语。运行时按原文查表，Prefab 中的静态文字按 key 绑定；不得为了修正英文拼写而单独改动查表原文。

覆盖范围：账户、提现及其任务和审核状态、每日任务、奖励按钮、免费转动、额外 Wild、新手引导、设置、评价、启动与连接失败提示、现有隐私全文，以及从图片改为 Prefab 文字的帮助说明和标题。每日任务覆盖 `Assets/StreamingAssets/RecoveredConfig` 中所有现有配置的六种模板。

术语：SPIN → GIRAR / GIROS，CLAIM → RESGATAR，CASH OUT → SACAR，Wild → curinga。PayPal 等品牌、GRAND / MAJOR / MINOR 奖池名称和 SCATTER 图案名称保持原样。

`inventory_text.py` 只读提取当前 Prefab / Scene 的 Unity YAML 文字组件、序列化文案模板和提现任务数组，可用于修改后的缺漏核查：

```powershell
python Tools/Localization/inventory_text.py
```

有意不翻译纯数字、时钟格式、货币数值样例、品牌名称、GM / mock 调试文案。运行时货币使用既有货币格式化逻辑，不从翻译表推导国家或金额。

最初盘点发现帮助页 `bz_bg02.png`、`bz_bg03.png` 的说明、帮助和设置标题等位于图片像素中。仅修改 JSON 不会替换图片文字；相关 `art.*` 条目供经过预配置的 Prefab 文本使用。复杂游戏美术中的 WILD、SCATTER、GRAND、MAJOR、MINOR 等仍是图案或奖池名称。

新增翻译应验证 key 没有重复、相同英文原文的别名使用相同译文、占位符完整、富文本标签保持一致，并在 Unity 中检查葡语重音字符、换行和按钮宽度。

Unity 菜单 `Dragon Legend/Localization/Prepare and Verify Assets` 会准备字形、更新静态绑定并输出英葡截图和检查报告到 `Artifacts/Localization`。`Build Android Player` 使用项目当前包名、版本和签名生成 `GildedDragon-localized.apk`。
