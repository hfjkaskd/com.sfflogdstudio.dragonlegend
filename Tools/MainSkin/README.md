# 主界面暖红金换皮

以用户最后确认的暖红金参考图为准。新资源位于 `Assets/Resources/MainSkin`，入口仍为原有 GameEntry 场景。只替换主界面的背景、顶部边饰、棋盘、奖池装饰、龙的红金材质、福袋、宝藏坐垫、旋转按钮、鱼与 Scatter 图案和金额呈色。

保留原 RectTransform、Transform、对象顺序、Canvas 排序、Button、事件、骨骼、UV、网格和动画。动态金额、次数和转轴结果仍由现有逻辑控制，不使用整屏效果图覆盖交互。

原始共享资源和弹窗未覆盖。GameEntry 仅将四项主界面 Prefab 引用切换到独立副本。`RecoveredSpinPlayfield.baseSymbols` 是可选的普通转轴美术配置，原 `symbols` 保留供免费玩法使用；其他行为不变。

## 美术来源

使用内置 imagegen，未使用 API/CLI 生成。参考图存于 `Artifacts/MainSkin/approved-reference.png`。统一提示词方向：朱砂红漆、香槟金雕纹、暖杏色云海、象牙金文字；保留原资源的形状、尺寸、分片位置和 UI 布局。各资源的源图、生成结果路径和目标路径记录在 `textures.json`。

生成的 RGB 通过离线导入适配到原贴图尺寸。透明通道沿用原图；分片动画只在指定部位转移颜色或替换底板，其余光效帧保留。旋转模糊图配套更新。全部处理发生在资源制作阶段，运行时没有图像转换、截图叠加或额外层级创建。

## 检查与维护

- `python -X utf8 Tools/MainSkin/install_and_audit.py`：核对原尺寸、逐像素透明通道、Prefab 对象结构、坐标、层级、资源路径及非主界面资源哈希。
- Unity 菜单 `Dragon Legend/Main Skin/Validate and Capture`：检查导入、脚本和标准 Button；运行时保存真实 Game View 截图与检查报告到 `Artifacts/MainSkin`。
- `prepare.py` 和 `NormalizeTextures.ps1` 是可选的离线重建工具。最终贴图和 Prefab 已在 Assets 中，正常打开/运行无需调用它们。
- 未打包或安装到 Android/iOS；当前验证范围为 Unity Editor 运行、资源和序列化结构检查。
