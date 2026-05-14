# ZMUI 框架使用说明

ZMUI 为 **Mono 分离式 UI 管理框架**：窗口逻辑类（继承 `WindowBase`）**不挂在预制体上**，由 **`UIModule`** 在运行时 `new` 出来，并与克隆出的 **UGUI 预制体**绑定。适合与 **ZMAsset** 等加载方式对接。

---

## 1. 场景与资源前提

### 1.1 场景节点

初始化时会执行：

```csharp
GameObject.Find("UICamera").GetComponent<Camera>();
GameObject.Find("UIRoot").transform;
```

因此首场景（或常驻场景）中必须存在：

| 名称 | 要求 |
|------|------|
| **UICamera** | 带 `Camera` 组件，作为 UI 相机；窗口 `Canvas.worldCamera` 会指向它。 |
| **UIRoot** | 作为所有窗口实例的父节点；`InstantiateObject` 的 `parent` 即此 Transform。 |

### 1.2 ScriptableObject 配置（Resources）

| 资源 | 路径 | 作用 |
|------|------|------|
| **WindowConfig** | `Resources/WindowConfig` | 记录每个窗口名与 **加载路径**（`WindowData.name` / `path`）。 |
| **UISetting** | `Resources/UISetting` | 预制体扫描目录、代码生成路径、遮罩模式、解析方式等。 |

加载失败时先检查上述资源是否在 **Resources** 下且命名正确。

---

## 2. 初始化流程

```csharp
// 若窗口走 ZMAsset，需先完成对应模块 InitAssetsModule，再初始化 UI
ZMAsset.InitFrameWork();
// await ZMAsset.InitAssetsModule("Window"); // 按需
UIModule.Instance.Initialize();
```

**`UIModule.Initialize()`** 主要做：

1. 绑定 **UICamera**、**UIRoot**。  
2. **`Resources.Load<WindowConfig>("WindowConfig")`**。  
3. **`AdaptationBangs.InitializeAdaptation()`**（刘海等适配）。  
4. **仅编辑器**：调用 **`WindowConfig.GeneratorWindowConfig()`**，根据 **`UISetting.WindowPrefabFolderPathArr`** 扫描磁盘上的 `*.prefab`，重写 **`windowDataList`**。

**注意**：`WindowPrefabFolderPathArr` 应配置为 **`Assets/...` 下真实放窗口预制体的目录**（与 ZMAsset bundle 表中的工程路径一致），不要指向 AB 输出目录（该处无 `.prefab` 文件）。

---

## 3. 命名约定（重要）

- **窗口逻辑类名** = **预制体文件名（无扩展名）** = **`WindowConfig` 中的 `name`**。  
  例如：类 **`DialogueWindow`** 对应预制体 **`DialogueWindow.prefab`**，`GetWindowData("DialogueWindow")` 才能命中。  
- **`PopUpWindow<DialogueWindow>()`** 内部用 **`typeof(T).Name`** 作为窗口名加载与缓存。

---

## 4. 窗口配置（WindowConfig）

每条 **`WindowData`**：

| 字段 | 含义 |
|------|------|
| **name** | 窗口名，与类名、预制体名一致。 |
| **path** | 传给加载接口的路径；使用 ZMAsset 时须与 **`{模块}assetbundleconfig.json`** 里该预制体的 **`path`** 一致（一般为 `Assets/.../窗口名`，是否带 `.prefab` 与加载端约定一致即可，ZMAsset 会在实例化时补 `.prefab`）。 |

**热更 / 多程序集**：若热更侧另有 `WindowConfig`，可在运行时 **`UIModule.Instance.AddAOTWindowMetadata(config)`** 合并进主配置。

---

## 5. UISetting（编辑器菜单）

菜单：**`ZM / ZMUI Setting`**（需 Odin 等条件编译时可用），编辑 **`UISetting`**：

| 配置项 | 说明 |
|--------|------|
| **SINGMASK_SYSTEM** | 单遮罩：多窗口叠加时仅最上层显示 Mask；否则每窗独立 Mask。 |
| **ParseType** | 生成器解析节点方式：名称 / Tag。 |
| **GeneratorType** | **Find**：生成查找组件脚本；**Bind**：生成绑定数据组件脚本。 |
| **WindowPrefabFolderPathArr** | 一个或多个 **Assets 下** 的窗口预制体根目录，供 **`GeneratorWindowConfig`** 扫描。 |
| **WindowGeneratorPath** 等 | 自动生成表现层脚本、Bind 脚本、Item 脚本等输出目录。 |
| **UsingNameSpaceArr** | 生成代码时自动 `using` 的命名空间。 |

---

## 6. 生命周期（WindowBehaviour / WindowBase）

逻辑类继承 **`WindowBase`**（再继承 **`WindowBehaviour`**）。

| 回调 | 调用时机 |
|------|----------|
| **OnAwake** | 窗口 **首次** 创建并绑定 `gameObject` 后调用一次；可在此设置 **`FullScreenWindow`**、将 **`Update = true`** 以开启 **`OnUpdate`**。 |
| **OnShow** | 每次 **显示**（含从隐藏再次显示）。 |
| **OnHide** | 每次 **隐藏**。 |
| **OnDestroy** | **Destroy** 窗口时。 |
| **OnUpdate** | 仅当 **`Update == true`** 且外部调用了 **`UIModule.Instance.OnUpdate()`** 时，对 **可见** 窗口每帧调用。 |

**注意**：`GetComponent` 取 **`XXXDataComponent`** 一般在 **`OnAwake`** 中完成；预制体上必须挂好数据组件，且 AB 需与当前工程一致。

---

## 7. UIModule 常用 API

| API | 说明 |
|-----|------|
| **Initialize()** | 初始化 UI 根与 `WindowConfig`（编辑器会刷新配置表）。 |
| **AddAOTWindowMetadata(config)** | 合并热更/分包带来的窗口路径元数据。 |
| **PopUpWindow&lt;T&gt;()** | 弹出窗口：`T : WindowBase, new()`；已存在则 **ShowWindow**。 |
| **GetWindow&lt;T&gt;()** | 从 **当前可见列表** 中按类型名查找；找不到会打 Error。 |
| **HideWindow&lt;T&gt;()** / **HideWindow(string)** | 隐藏：从可见列表移除、`SetVisible(false)`、**OnHide**。 |
| **DestroyWinodw&lt;T&gt;()**（拼写如此） | 销毁实例并从字典移除，**OnDestroy** + `Destroy(gameObject)`。 |
| **DestroyAllWindow(filterlist)** | 批量销毁，可传入 **不销毁** 的窗口名列表。 |
| **PreLoadWindow&lt;T&gt;()** | 只加载预制体并 **OnAwake**，默认 **SetVisible(false)**，不 **OnShow**。 |
| **OnUpdate()** | 需由你方在 **`MonoBehaviour.Update`** 中转发，驱动 **`Update==true`** 的窗口 **OnUpdate**。 |

### 7.1 堆栈弹窗

| API | 说明 |
|-----|------|
| **PushWindowToStack&lt;T&gt;(popCallBack, single, pushToStackTop)** | 将“待弹窗”信息压入栈。 |
| **StartPopFirstStackWindow()** | 开始弹出栈顶序列。 |
| **PushAndPopStackWindow&lt;T&gt;(...)** | 压栈并立即开始弹出流程。 |
| **PopStackWindow()** | 弹出栈中下一个窗口。 |
| **ClearStackWindows()** | 清空栈数据。 |

隐藏或销毁参与堆栈流程的窗口时，会触发 **`PopNextStackWindow`** 继续栈逻辑。

---

## 8. WindowBase 常用能力

- **Canvas / CanvasGroup**：基类在 **OnAwake** 后初始化 **CanvasGroup**、查找子节点 **UIMask**、**UIContent**。预制体结构需符合框架约定（如存在 **UIMask**、**UIContent**）。  
- **ShowAnimation / HideAnimation**：与 **sortingOrder**、DOTween 相关；**HideWindow()** 可走动画后隐藏。  
- **FullScreenWindow**：为 **true** 时参与 **智能显隐**（被全屏窗遮挡的窗口伪隐藏以降低渲染开销）。  
- **AddButtonClickListener** / **AddToggleClickListener** / **AddInputFieldListener**：便于在 **OnDestroy** 时统一移除监听。  
- **PopUpWindow&lt;T&gt;()**：从当前窗口再弹子窗口的快捷方式。

---

## 9. 对接自定义资源框架（如 ZMAsset）

修改 **`UIModule`** 中 **`LoadWindow` / `ResourcesLoadObj` / `GameObjectDestoryWindow`** 三处即可：

- 当前工程示例：**`ResourcesLoadObj`** 内使用 **`ZMAsset.InstantiateObject(WindowConfig.path, mUIRoot)`**。  
- 销毁时若需释放 AB 引用，可在 **`GameObjectDestoryWindow`** 中改为 **`ZMAsset.Release`** 等。

**务必**在 **`Initialize`** 之前或业务入口处完成 **`InitAssetsModule`**，保证 **`path`** 已在 CRC 表中注册。

---

## 10. 代码生成（Editor）

工具脚本位于 **`Assets/ZMPackages/ZMUI/Editor/`**，常见入口包括：

- **`GeneratorWindowTool`**：根据窗口预制体生成 **`XXXWindow`** 表现层脚本。  
- **`GeneratorBindComponentTool`** / **`GeneratorFindComponentTool`**：生成 **`XXXDataComponent`** 或查找型组件脚本。  
- **`GeneratorBindItemsComponentTool`**：列表 Item 等。

生成路径由 **`UISetting`** 中各项 **`*GeneratorPath`** 指定；生成规则与 **ParseType / GeneratorType** 相关。具体按钮与菜单位置以当前 Unity 菜单为准（部分依赖 Odin）。

---

## 11. 默认组件与示例

- **`Default/`**：如 **SelectWindow**、**Toast** 等可参考。  
- **`Example/`**：示例窗口与自动生成脚本，可对照学习 **DataComponent** 与 **Window** 的配合。

---

## 12. 常见问题

1. **`GetWindow` 为 null**  
   该方法只查 **可见** 窗口；若窗口已 **Hide** 则取不到。需要时改为查 **`mAllWindowDic`** 或自行缓存引用。

2. **配置里没有该窗口名**  
   检查 **`WindowConfig`** 是否含 **`name`**；编辑器下执行一次 **`Initialize`** 触发 **`GeneratorWindowConfig`**，或手动维护 **`windowDataList`**。

3. **ZMAsset 加载失败**  
   **`path`** 与 bundle 表不一致，或未 **`InitAssetsModule`**。

4. **刘海适配**  
   使用 **`AdaptationBangs`**（初始化在 **`UIModule.Initialize`** 内）。

---

## 13. 相关源码路径

| 内容 | 路径 |
|------|------|
| UI 单例与窗口调度 | `Runtime/Core/UIModule.cs` |
| 窗口逻辑基类 | `Runtime/Base/WindowBase.cs`、`WindowBehaviour.cs` |
| 窗口配置 | `Runtime/Base/WindowConfig.cs` |
| 全局 UI 设置 | `AOT/UISetting.cs` |
| 编辑器总入口（Odin） | `Editor/ZMUIWindow.cs` |

---

*文档依据当前工程内 ZMUI 源码整理；若框架升级请以源码为准。*
