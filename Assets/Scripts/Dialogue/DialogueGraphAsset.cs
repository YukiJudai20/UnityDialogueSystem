using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using ZMGC.Game;
using ZM.UI;

    /// <summary>
    /// 说话人类型：区分 NPC、玩家与旁白，用于 UI 侧决定名字标签与头像显隐。
    /// </summary>
    public enum SpeakerType
    {
        [LabelText("NPC")]
        NPC = 0,

        [LabelText("玩家")]
        Player = 1,

        [LabelText("旁白")]
        Narrator = 2,
    }

    /// <summary>
    /// 节点类型：对话为单出口台词；选项节点含多条玩家选项与连线；条件分支为程序判定走线；结束为流程终点（无出口）。
    /// </summary>
    public enum DialogueNodeKind
    {
        [LabelText("对话")]
        Dialogue = 0,

        [LabelText("条件分支")]
        Branch = 1,

        /// <summary>
        /// 仅 <see cref="OptionNode"/> 使用 <see cref="OptionNode.Choices"/>；推进到此时由 UI 展示说明与选项（通常为弹窗）。
        /// </summary>
        [LabelText("选项")]
        Option = 2,

        /// <summary>
        /// 流程终点：无输出端口；进入时 <see cref="EndNode.OnEnter"/> 关闭窗口、清空数据并触发 <c>OnDialogueEnded</c>。
        /// </summary>
        [LabelText("结束")]
        End = 3,
    }

    /// <summary>
    /// 单条分支选项：文案由策划编辑，目标节点由图编辑器连线写入。
    /// </summary>
    [Serializable]
    public sealed class DialogueChoice
    {
        [LabelText("选项文案")]
        [TextArea(1, 3)]
        public string ChoiceText = "继续";

        [LabelText("目标节点 ID")]
        [ReadOnly]
        [Tooltip("在节点编辑器中拖拽连线到目标节点即可自动填写。")]
        public string TargetNodeId = string.Empty;
    }

    /// <summary>
    /// 条件分支的一条出口：由运行时根据 <see cref="ConditionKey"/> 等判断走向，目标由图连线写入。
    /// </summary>
    [Serializable]
    public sealed class BranchRoute
    {
        [HorizontalGroup("路由", Width = 0.42f)]
        [LabelText("条件键")]
        [Tooltip("运行时自行解释，例如 quest_done、gold_ge_100；留空可作为 Else 兜底（建议放在列表最后一项）。")]
        public string ConditionKey = string.Empty;

        [HorizontalGroup("路由", Width = 0.58f)]
        [LabelText("显示名")]
        public string Label = "分支";

        [Title("策划说明")]
        [HideLabel]
        [TextArea(2, 5)]
        [Tooltip("给策划看的条件说明，不参与运行时逻辑。")]
        public string Description = string.Empty;

        [LabelText("目标节点 ID")]
        [ReadOnly]
        [Tooltip("在节点编辑器中从该出口拖到目标节点即可自动填写。")]
        public string TargetNodeId = string.Empty;
    }

    /// <summary>
    /// 图中的一个对话节点基类。
    /// </summary>
    [Serializable]
    public abstract class DialogueNodeBase
    {
        [HideInInspector]
        public string Id = string.Empty;

        [HideInInspector]
        public Vector2 GraphPosition;

        /// <summary>
        /// 编辑器里节点的宽高（GraphView 布局）；为零表示尚未记录，打开时使用默认尺寸。
        /// </summary>
        [HideInInspector]
        public Vector2 GraphSize;

        public const float DefaultEditorNodeWidth = 220f;
        public const float DefaultEditorNodeHeight = 128f;

        /// <summary>
        /// 供 GraphView 恢复节点矩形（位置 + 非零尺寸）。
        /// </summary>
        public Rect GetEditorLayoutRectOrDefault()
        {
            var w = GraphSize.x >= 24f ? GraphSize.x : DefaultEditorNodeWidth;
            var h = GraphSize.y >= 24f ? GraphSize.y : DefaultEditorNodeHeight;
            return new Rect(GraphPosition, new Vector2(w, h));
        }

        /// <summary>
        /// 将 GraphView 中的布局写回资产（位置与大小）。
        /// </summary>
        public void SetEditorLayoutFromRect(Rect rect)
        {
            GraphPosition = rect.position;
            GraphSize = new Vector2(
                Mathf.Max(rect.width, 24f),
                Mathf.Max(rect.height, 24f));
        }

        [HorizontalGroup("标题", Width = 0.65f)]
        [LabelText("标题")]
        public string Title = "对话";

        public abstract DialogueNodeKind Kind { get; }

        /// <summary>
        /// 节点被激活时调用：负责派发对应的 UI 事件。
        /// </summary>
        public abstract void OnEnter();

        /// <summary>该节点在编辑器中的输出端口数量。</summary>
        public abstract int GetOutputPortCount();

        /// <summary>获取指定输出端口指向的节点 ID。</summary>
        public abstract string GetOutputTargetId(int portIndex);

        /// <summary>设置指定输出端口指向的节点 ID。</summary>
        public abstract void SetOutputTargetId(int portIndex, string targetId);

        /// <summary>清理指向不存在节点的引用。</summary>
        public abstract void SanitizeReferences(HashSet<string> validIds);
    }

    /// <summary>
    /// 对话节点：展示 Speaker + Body，单出口沿 <see cref="NextNodeId"/> 推进。
    /// </summary>
    [Serializable]
    public sealed class DialogueNode : DialogueNodeBase
    {
        public override DialogueNodeKind Kind => DialogueNodeKind.Dialogue;

        [HorizontalGroup("标题", Width = 0.35f)]
        [LabelText("说话人")]
        public SpeakerType Speaker = SpeakerType.NPC;

        [Title("正文")]
        [HideLabel]
        [TextArea(4, 14)]
        public string Body = string.Empty;

        [LabelText("下一节点 ID")]
        [ReadOnly]
        [Tooltip("在节点编辑器中从右侧「下一节点」出口拖到目标节点即可自动填写。对话节点仅允许单出口。")]
        public string NextNodeId = string.Empty;

        public override void OnEnter()
        {
            // 1. 调用逻辑层自定义操作
            var logic = GameWorld.GetLogicLayer<DialogueLogicCtrl>();
            logic?.OnDialogueNodeEnter(this);

            // 2. 写入数据层
            var data = GameWorld.GetDataLayer<DialogueDataMgr>();
            data?.SetDialogueData(Body, Speaker);
            data?.SetContinueFlow(NextNodeId, canClickToContinue: !string.IsNullOrEmpty(NextNodeId));

            // 3. 弹出或显示对话窗口（勿用 GetWindow：其仅查可见列表，未显示时会误报错）
            UIModule.Instance.PopUpWindow<DialogueWindow>();

            // 4. 派发事件通知 Window 刷新 UI
            UIEventControl.DispensEvent(UIEventEnum.DialogueRefresh);
        }

        public override int GetOutputPortCount() => 1;

        public override string GetOutputTargetId(int portIndex) => portIndex == 0 ? NextNodeId : string.Empty;

        public override void SetOutputTargetId(int portIndex, string targetId)
        {
            if (portIndex == 0)
                NextNodeId = targetId ?? string.Empty;
        }

        public override void SanitizeReferences(HashSet<string> validIds)
        {
            if (!string.IsNullOrEmpty(NextNodeId) && !validIds.Contains(NextNodeId))
                NextNodeId = string.Empty;
        }
    }

    /// <summary>
    /// 分支节点：根据 <see cref="BranchRoutes"/> 条件判定走线。
    /// </summary>
    [Serializable]
    public sealed class BranchNode : DialogueNodeBase
    {
        public override DialogueNodeKind Kind => DialogueNodeKind.Branch;

        [Title("条件出口")]
        [InfoBox("运行时从数据层读取 CurrentBranchKey，与 ConditionKey 相等则走该出口；无匹配时走 ConditionKey 为空的 Else（建议放在列表末尾）。", InfoMessageType.None)]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
        [LabelText("分支")]
        public List<BranchRoute> BranchRoutes = new List<BranchRoute>
        {
            new BranchRoute { Label = "条件 A", ConditionKey = "a" },
            new BranchRoute { Label = "否则", ConditionKey = string.Empty },
        };

        public override int GetOutputPortCount() => BranchRoutes?.Count ?? 0;

        public override string GetOutputTargetId(int portIndex)
        {
            if (BranchRoutes == null || portIndex < 0 || portIndex >= BranchRoutes.Count)
                return string.Empty;
            return BranchRoutes[portIndex]?.TargetNodeId ?? string.Empty;
        }

        public override void SetOutputTargetId(int portIndex, string targetId)
        {
            if (BranchRoutes == null || portIndex < 0 || portIndex >= BranchRoutes.Count)
                return;
            var r = BranchRoutes[portIndex];
            if (r != null)
                r.TargetNodeId = targetId ?? string.Empty;
        }

        public override void OnEnter()
        {
            string targetNodeId = ResolveTargetNodeId();
            var data = GameWorld.GetDataLayer<DialogueDataMgr>();
            data?.SetContinueFlow(targetNodeId, canClickToContinue: false);

            if (!string.IsNullOrEmpty(targetNodeId))
                DialogueManager.Instance?.EnterNode(targetNodeId);
            else
                Debug.LogError("BranchNode.OnEnter: no valid branch route resolved.");
        }

        public override void SanitizeReferences(HashSet<string> validIds)
        {
            if (BranchRoutes == null)
                return;
            foreach (var r in BranchRoutes)
            {
                if (r != null && !string.IsNullOrEmpty(r.TargetNodeId) && !validIds.Contains(r.TargetNodeId))
                    r.TargetNodeId = string.Empty;
            }
        }

        private string ResolveTargetNodeId()
        {
            var data = GameWorld.GetDataLayer<DialogueDataMgr>();
            string key = data != null ? data.CurrentBranchKey : string.Empty;

            string targetNodeId = string.Empty;
            if (BranchRoutes != null)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    foreach (var r in BranchRoutes)
                    {
                        if (r == null || string.IsNullOrEmpty(r.TargetNodeId))
                            continue;
                        if (r.ConditionKey == key)
                        {
                            targetNodeId = r.TargetNodeId;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(targetNodeId))
                {
                    foreach (var r in BranchRoutes)
                    {
                        if (r == null || string.IsNullOrEmpty(r.TargetNodeId))
                            continue;
                        if (string.IsNullOrEmpty(r.ConditionKey))
                        {
                            targetNodeId = r.TargetNodeId;
                            break;
                        }
                    }
                }
            }

            return targetNodeId;
        }
    }

    /// <summary>
    /// 选项节点：进入时由 UI 展示 <see cref="PopupMessage"/> 与 <see cref="Choices"/> 列表。
    /// </summary>
    [Serializable]
    public sealed class OptionNode : DialogueNodeBase
    {
        public override DialogueNodeKind Kind => DialogueNodeKind.Option;

        [Title("选项说明")]
        [InfoBox("进入该节点时由 UI 展示下方说明与选项列表（与「对话」节点区分）。", InfoMessageType.None)]
        [HideLabel]
        [TextArea(3, 10)]
        [Tooltip("选项上方主文案（例如「请选择要去哪里」）。")]
        public string PopupMessage = "请选择：";

        [Title("选项列表")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
        [LabelText("选项")]
        public List<DialogueChoice> Choices = new List<DialogueChoice>
        {
            new DialogueChoice { ChoiceText = "选项 A" },
            new DialogueChoice { ChoiceText = "选项 B" },
        };

        public override int GetOutputPortCount() => Choices?.Count ?? 0;

        public override string GetOutputTargetId(int portIndex)
        {
            if (Choices == null || portIndex < 0 || portIndex >= Choices.Count)
                return string.Empty;
            return Choices[portIndex]?.TargetNodeId ?? string.Empty;
        }

        public override void SetOutputTargetId(int portIndex, string targetId)
        {
            if (Choices == null || portIndex < 0 || portIndex >= Choices.Count)
                return;
            var c = Choices[portIndex];
            if (c != null)
                c.TargetNodeId = targetId ?? string.Empty;
        }

        public override void OnEnter()
        {
            // 1. 组装选项数据写入数据层
            var data = GameWorld.GetDataLayer<DialogueDataMgr>();
            if (data != null && Choices != null)
            {
                int count = Choices.Count;
                var texts = new string[count];
                var targets = new string[count];
                for (int i = 0; i < count; i++)
                {
                    var c = Choices[i];
                    texts[i] = c?.ChoiceText ?? string.Empty;
                    targets[i] = c?.TargetNodeId ?? string.Empty;
                }
                data.SetOptionData(PopupMessage, texts, targets);
            }
            data?.SetContinueFlow(string.Empty, canClickToContinue: false);

            // 2. 与对话节点一致：弹出或显示同一 DialogueWindow（勿用 GetWindow：其仅查可见列表，未显示时会误报错）
            UIModule.Instance.PopUpWindow<DialogueWindow>();

            // 3. 派发事件通知 UI 刷新
            UIEventControl.DispensEvent(UIEventEnum.OptionRefresh);
        }

        public override void SanitizeReferences(HashSet<string> validIds)
        {
            if (Choices == null)
                return;
            foreach (var c in Choices)
            {
                if (c != null && !string.IsNullOrEmpty(c.TargetNodeId) && !validIds.Contains(c.TargetNodeId))
                    c.TargetNodeId = string.Empty;
            }
        }
    }

    /// <summary>
    /// 结束节点：流程终点，无输出端口。
    /// </summary>
    [Serializable]
    public sealed class EndNode : DialogueNodeBase
    {
        public override DialogueNodeKind Kind => DialogueNodeKind.End;

        public override void OnEnter()
        {
            DialogueManager.Instance?.FinishDialogueFromEndNode();
        }

        public override int GetOutputPortCount() => 0;

        public override string GetOutputTargetId(int portIndex) => string.Empty;

        public override void SetOutputTargetId(int portIndex, string targetId) { }

        public override void SanitizeReferences(HashSet<string> validIds) { }
    }

    /// <summary>
    /// 节点式对话图资产：供 UI Toolkit + GraphView 编辑器与运行时读取。
    /// </summary>
    [CreateAssetMenu(menuName = "DialogueSystem/对话SO", fileName = "DialogueGraph")]
    public sealed class DialogueGraphAsset : ScriptableObject
    {
        [Title("入口")]
        [ValueDropdown(nameof(GetEntryDropdown))]
        [LabelText("起始节点")]
        [Tooltip("对话从该节点开始。")]
        public string EntryNodeId = string.Empty;

        [Title("节点列表")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
        [LabelText("节点")]
        [SerializeReference]
        public List<DialogueNodeBase> Nodes = new List<DialogueNodeBase>();

        public DialogueNodeBase FindNode(string id)
        {
            if (string.IsNullOrEmpty(id) || Nodes == null)
                return null;
            for (var i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                if (n != null && n.Id == id)
                    return n;
            }

            return null;
        }

        /// <summary>
        /// 解析实际起始节点 ID：<see cref="EntryNodeId"/> 有效则用之，否则退回列表中第一个带 ID 的节点（避免删节点后入口残留）。
        /// </summary>
        public bool TryGetResolvedEntryNodeId(out string entryId)
        {
            entryId = string.Empty;
            if (Nodes == null || Nodes.Count == 0)
                return false;

            if (!string.IsNullOrEmpty(EntryNodeId) && FindNode(EntryNodeId) != null)
            {
                entryId = EntryNodeId;
                return true;
            }

            for (var i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                if (n != null && !string.IsNullOrEmpty(n.Id))
                {
                    entryId = n.Id;
                    return true;
                }
            }

            return false;
        }

        public int IndexOfNode(string id)
        {
            if (string.IsNullOrEmpty(id) || Nodes == null)
                return -1;
            for (var i = 0; i < Nodes.Count; i++)
            {
                var n = Nodes[i];
                if (n != null && n.Id == id)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 删除指向已不存在节点的引用，并保证至少有一个节点时入口有效。
        /// </summary>
        [Button("清理无效引用", ButtonSizes.Medium)]
        public void SanitizeReferences()
        {
            if (Nodes == null)
                return;
            var valid = new HashSet<string>(Nodes.Where(n => n != null && !string.IsNullOrEmpty(n.Id)).Select(n => n.Id));
            foreach (var n in Nodes)
            {
                if (n == null)
                    continue;
                n.SanitizeReferences(valid);
            }

            if (!string.IsNullOrEmpty(EntryNodeId) && !valid.Contains(EntryNodeId))
                EntryNodeId = Nodes.Count > 0 ? Nodes[0].Id : string.Empty;
        }

        private IEnumerable<ValueDropdownItem<string>> GetEntryDropdown()
        {
            if (Nodes == null)
                yield break;
            foreach (var n in Nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.Id))
                    continue;
                var shortId = n.Id.Length <= 8 ? n.Id : n.Id.Substring(0, 8) + "…";
                var tag = n.Kind switch
                {
                    DialogueNodeKind.Branch => "[分支] ",
                    DialogueNodeKind.Option => "[选项] ",
                    DialogueNodeKind.End => "[结束] ",
                    _ => "[对话] ",
                };
                var label = string.IsNullOrEmpty(n.Title) ? shortId : $"{n.Title}  ({shortId})";
                yield return new ValueDropdownItem<string>(tag + label, n.Id);
            }
        }
    }
