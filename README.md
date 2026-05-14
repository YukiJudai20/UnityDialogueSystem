# DialogueSystem（Unity 节点式对话系统）

基于 **ScriptableObject + GraphView** 的轻量对话方案：在编辑器里用可视化节点编排流程，运行时由 `DialogueManager` 驱动，UI 通过数据层与事件刷新，适合快速接入任务、商店、剧情等交互。

**Unity 版本：** 2022.3.62f3c1 LTS

------

## 功能概览

- **对话图资产** `DialogueGraphAsset`：入口节点、节点列表、`SerializeReference` 保存多种节点类型。
- **节点类型**
  - **对话**：说话人（NPC / 玩家 / 旁白）、正文、单出口「下一节点」；支持点击继续（由数据层 `CanClickToContinue` 控制）。
  - **条件分支**：多路由，按数据层 `CurrentBranchKey` 匹配 `ConditionKey`，可配置 Else（空键）；由逻辑层在进分支前写入分支键。
  - **选项**：说明文案 + 多条选项，每条绑定目标节点；与对话共用 `DialogueWindow` 展示选项按钮。
  - **结束**：无出口，进入后关闭对话 UI、清空数据层，并可触发 `StartDialogue` 传入的结束回调。
- **编辑器**：菜单 `Window → DialogueSystem → 对话图编辑器`，左侧 GraphView 拖拽布局与连线，右侧 Odin 编辑节点字段；节点颜色区分类型（对话黑、分支橙、选项蓝、结束白）；布局写入节点 `GraphPosition` / `GraphSize`，下次打开自动恢复。
- **运行时 UI**：`DialogueWindow` 订阅 `DialogueRefresh` / `OptionRefresh`，从 `DialogueDataMgr` 刷新文案与动态生成 `DialogueOption` 预制体。

![image](https://github.com/YukiJudai20/UnityDialogueSystem/blob/main/对话SO.png)
![image](https://github.com/YukiJudai20/UnityDialogueSystem/blob/main/对话编辑窗口.png)

------

## 技术栈与依赖

| 模块                                                      | 说明                                                         |
| --------------------------------------------------------- | ------------------------------------------------------------ |
| **ZMUI**（`Assets/ZMPackages/ZMUI`）                      | 窗口基类、`UIModule`、`UIEventControl` 等 UI 框架            |
| **ZMGC**（`Assets/ZMPackages/ZMGC`）                      | `GameWorld`、`WorldManager`、`GameWorld.GetDataLayer` / `GetLogicLayer` |
| **Odin Inspector**（`Assets/ZMPackages/Library/Sirenix`） | 对话图 Inspector 与编辑器内嵌属性面板                        |
| **UGUI**                                                  | 对话窗口与选项条目使用 `UnityEngine.UI.Text` / `Button`      |

------

## 目录结构（核心）

```
Assets/
├── Scripts/
│   ├── Dialogue/              # 对话图资产、节点定义、DialogueManager
│   │   └── Editor/            # GraphView 编辑器、USS/UXML
│   ├── GameWorld/             # GameWorld、DialogueDataMgr、DialogueLogicCtrl
│   ├── Window/Window/         # DialogueWindow 表现层
│   └── Main.cs                # 示例：创建世界、设置分支键、开始对话
├── Resources/
│   ├── DialogueWindow.prefab
│   ├── DialogueOption.prefab
│   └── DialogueGraph.asset    # 示例图
└── ZMPackages/                # ZMUI、ZMGC、Odin 等
```

------

## 快速开始

### 1. 创建 / 编辑对话图

1. 在 Project 中 **右键 → Create → DialogueSystem → 对话 SO**，或使用编辑器里 **「新建资源」**。
2. 打开 **Window → DialogueSystem → 对话图编辑器**，指定该资产。
3. 用工具栏或画布右键添加节点，从输出端口拖到目标节点输入端口完成连线。
4. 在分支节点上配置各出口的 **条件键**；运行前由逻辑层调用 `DialogueDataMgr.SetBranchKey(string)`（示例见 `Main.cs`）。

### 2. 运行时播放

```csharp
// 需已创建 GameWorld 并完成数据层注册（与示例 Main 一致）
DialogueManager.Instance.StartDialogue(dialogueGraphAsset, onDialogueEnded: () =>
{
    // 对话正常走完结束节点后的回调（可选）
});
```

- **继续下一句**：在允许点击继续时，由 `DialogueManager.Continue()` 进入下一节点（示例里绑在鼠标左键，可按项目改为 UI 按钮）。
- **选项**：点击选项按钮会调用 `DialogueManager.Instance.EnterNode(targetNodeId)`（已在 `DialogueWindow` 中注册）。

### 3. 数据层与逻辑层

- **数据层** `DialogueDataMgr`（命名空间 `ZMGC.Game`）：当前节点、对白正文、说话人、选项列表、分支键、`NextNodeId` / `CanClickToContinue` 等。
- **逻辑层** `DialogueLogicCtrl`：对话节点进入时可扩展 `OnDialogueNodeEnter` 等业务（音效、任务、日志等）。
- **表现与逻辑分离**：节点 `OnEnter` 写入 `DialogueDataMgr` 并派发 UI 事件；窗口只负责展示与输入，不承载业务规则。
------
