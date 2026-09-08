# Unity 开发规范

本项目的 Unity 开发必须遵守以下规则。

## 1. Obfuz 混淆兼容

禁止用反射调用方法或字段。确实必须使用反射时，被调用处和调用处按需添加 `[Obfuz.ObfuzIgnore]`。避免依赖 `enum.ToString()` 做逻辑判断、存档 key、配置 key 或反射匹配。

禁止通过拖拽的方式添加 Unity 事件，需要使用代码的方式。

## 2. Prefab-First

优先用 Prefab / Scene / Inspector / ScriptableObject 配置结构和参数。代码只负责运行时逻辑，例如状态控制、事件绑定、数据刷新、显示隐藏、实例化已配置好的 Prefab、播放动画/音效。

禁止用代码大量动态创建静态 UI、GameObject 层级、布局、样式、颜色、字体等静态结构。禁止写死本应由策划或美术调整的参数。

## 3. 资源引用

组件、Prefab、轻量配置可以用 `[SerializeField]`。较大的图片、音效、特效、模型等资源尽量不要用 `[SerializeField]` 强引用，避免游戏启动时内存峰值过高。大资源可使用 Resources 或 Addressables 按路径加载。

## 4. Editor 与真机一致

禁止写 Editor 专用兜底逻辑。不得用 `Application.isEditor`、`#if UNITY_EDITOR` 改变核心逻辑、初始化流程、资源加载流程、数据路径或异常处理。Editor 必须复现 Android/iOS 真机逻辑；问题应修根因，不允许只在 Editor 下绕过。

允许仅用于 Debug Log、Gizmos、Profiler 等不影响运行结果的调试代码。

## 5. UI 交互

所有可点击 UI 必须使用 Unity 标准 Button 或 UI Toolkit Button。禁止用 Image + Collider、OnMouseDown、OnPointerDown、手写 Raycast、空物体点击区域等方式替代 Button。

视觉层和交互层必须在 Prefab 中清晰表达，禁止“视觉和点击区域分离”的伪按钮结构。

## 6. 禁止 UI 当游戏元素

核心玩法对象不能直接用 UI 元素实现。游戏元素应使用正确的 GameObject、Sprite、Mesh、Prefab 等结构，UI 只负责界面展示和交互。

## 7. 性能规则

轻量、低频逻辑优先保持简单可读，不需要过度优化。高频路径和重任务必须考虑 CPU、GC、内存和加载峰值。

在 `Update / LateUpdate / FixedUpdate / 高频回调 / 大循环` 中避免 LINQ、频繁 new、Find、FindObjectOfType、频繁 GetComponent、字符串拼接、频繁 Instantiate/Destroy、重复 UI Layout rebuild。高频对象应缓存、复用或使用对象池。

## 8. 高成本方案提示

遇到高性能任务或高成本实现时，必须主动说明性能风险，并给出更低成本的替代方案。需要明确建议：

- 推荐使用哪个方案
- 是否可以接受高消耗方案
- 如果继续使用高消耗方案，可能带来的 CPU / GC / 内存 / 加载影响

不要直接默认采用高消耗实现。
