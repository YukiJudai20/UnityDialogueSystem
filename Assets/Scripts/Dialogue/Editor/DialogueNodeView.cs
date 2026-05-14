using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.Dialogue.Editor
{
    /// <summary>
    /// GraphView 中的单个对话节点视图。
    /// </summary>
    public sealed class DialogueNodeView : Node
    {
        public const string InputPortName = "In";

        private const string UssPath = "Assets/Scripts/Dialogue/Editor/DialogueGraphNodes.uss";

        private static StyleSheet s_nodeStyleSheet;

        public DialogueNodeBase Data { get; }

        private readonly DialogueGraphAsset _owner;
        private VisualElement _rootGridLayer;

        public DialogueNodeView(DialogueGraphAsset owner, DialogueNodeBase data)
        {
            _owner = owner;
            Data = data ?? throw new ArgumentNullException(nameof(data));
            viewDataKey = data.Id;

            EnsureSharedStyleSheet();

            style.minWidth = 215;
            SyncTitleFromData();
            EnsureRootGridLayer();
            RebuildChoicePorts();
            ApplyKindVisuals();

            RegisterCallback<MouseUpEvent>(_ => PersistLayoutIfChanged());
            RegisterCallback<PointerCaptureOutEvent>(_ => PersistLayoutIfChanged());
        }

        private static void EnsureSharedStyleSheet()
        {
            if (s_nodeStyleSheet != null)
                return;
            s_nodeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        }

        public static void TryAddStylesTo(VisualElement host)
        {
            EnsureSharedStyleSheet();
            if (s_nodeStyleSheet != null && host != null && !host.styleSheets.Contains(s_nodeStyleSheet))
                host.styleSheets.Add(s_nodeStyleSheet);
        }

        private void EnsureRootGridLayer()
        {
            if (_rootGridLayer != null)
                return;
            _rootGridLayer = new VisualElement { name = "dg-node-root-grid", pickingMode = PickingMode.Ignore };
            _rootGridLayer.AddToClassList("dg-node-root-grid");
            _rootGridLayer.style.position = Position.Absolute;
            _rootGridLayer.style.left = 0;
            _rootGridLayer.style.top = 0;
            _rootGridLayer.style.right = 0;
            _rootGridLayer.style.bottom = 0;
            Insert(0, _rootGridLayer);
        }

        private void PersistLayoutIfChanged()
        {
            if (_owner == null || Data == null)
                return;
            var r = GetPosition();
            if (r.width < 8f || r.height < 8f)
                return;

            var oldRect = Data.GetEditorLayoutRectOrDefault();
            if ((r.position - oldRect.position).sqrMagnitude < 0.0001f &&
                Mathf.Abs(r.width - oldRect.width) < 0.5f &&
                Mathf.Abs(r.height - oldRect.height) < 0.5f)
                return;

            Undo.RecordObject(_owner, "调整对话节点布局");
            Data.SetEditorLayoutFromRect(r);
            EditorUtility.SetDirty(_owner);
        }

        public void SyncTitleFromData()
        {
            if (!string.IsNullOrEmpty(Data.Title))
            {
                title = Data.Title;
                return;
            }

            title = Data.Kind switch
            {
                DialogueNodeKind.Branch => "分支",
                DialogueNodeKind.Option => "选项",
                DialogueNodeKind.End => "结束",
                _ => "对话",
            };
        }

        public void ApplyKindVisuals()
        {
            RemoveFromClassList("dg-node-kind-dialogue");
            RemoveFromClassList("dg-node-kind-branch");
            RemoveFromClassList("dg-node-kind-popup");
            RemoveFromClassList("dg-node-kind-end");

            Color titleBg;
            Color titleFg;
            switch (Data.Kind)
            {
                case DialogueNodeKind.Branch:
                    AddToClassList("dg-node-kind-branch");
                    titleBg = new Color(1f, 0.55f, 0.12f, 1f);
                    titleFg = new Color(0.12f, 0.12f, 0.12f, 1f);
                    break;
                case DialogueNodeKind.Option:
                    AddToClassList("dg-node-kind-popup");
                    titleBg = new Color(0.18f, 0.45f, 0.95f, 1f);
                    titleFg = new Color(0.98f, 0.98f, 1f, 1f);
                    break;
                case DialogueNodeKind.End:
                    AddToClassList("dg-node-kind-end");
                    titleBg = new Color(1f, 1f, 1f, 1f);
                    titleFg = new Color(0.12f, 0.12f, 0.12f, 1f);
                    break;
                default:
                    AddToClassList("dg-node-kind-dialogue");
                    titleBg = new Color(0f, 0f, 0f, 1f);
                    titleFg = new Color(0.96f, 0.96f, 0.98f, 1f);
                    break;
            }

            if (titleContainer != null)
            {
                titleContainer.style.backgroundColor = titleBg;
                titleContainer.style.color = titleFg;
                ApplyTitleTextColorRecursive(titleContainer, titleFg);
                schedule.Execute(() => ApplyTitleTextColorRecursive(titleContainer, titleFg)).StartingIn(0);
            }

            if (_rootGridLayer != null)
            {
                var g = Data.Kind switch
                {
                    DialogueNodeKind.Branch => 0.04f,
                    DialogueNodeKind.Option => 0.035f,
                    DialogueNodeKind.End => 0.025f,
                    _ => 0.03f,
                };
                _rootGridLayer.style.opacity = 1f;
                _rootGridLayer.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, g);
            }
        }

        public void RebuildChoicePorts()
        {
            inputContainer.Clear();
            outputContainer.Clear();

            var input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            input.portName = InputPortName;
            input.userData = new PortUserData(Data.Id, -1, true);
            inputContainer.Add(input);

            if (Data is BranchNode branch)
            {
                var routes = branch.BranchRoutes ?? new System.Collections.Generic.List<BranchRoute>();
                for (var i = 0; i < routes.Count; i++)
                {
                    var r = routes[i];
                    var key = r == null ? string.Empty : r.ConditionKey?.Trim() ?? string.Empty;
                    var label = r == null || string.IsNullOrWhiteSpace(r.Label)
                        ? (string.IsNullOrEmpty(key) ? $"Else {i + 1}" : Truncate(key, 20))
                        : Truncate(r.Label.Trim(), 22);
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = label;
                    port.userData = new PortUserData(Data.Id, i, false);
                    outputContainer.Add(port);
                }
            }
            else if (Data is DialogueNode)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.portName = "下一节点";
                port.userData = new PortUserData(Data.Id, 0, false);
                outputContainer.Add(port);
            }
            else if (Data is EndNode)
            {
            }
            else if (Data is OptionNode option)
            {
                var choices = option.Choices ?? new System.Collections.Generic.List<DialogueChoice>();
                for (var i = 0; i < choices.Count; i++)
                {
                    var c = choices[i];
                    var label = c == null || string.IsNullOrWhiteSpace(c.ChoiceText)
                        ? $"选项 {i + 1}"
                        : Truncate(c.ChoiceText.Trim(), 22);
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = label;
                    port.userData = new PortUserData(Data.Id, i, false);
                    outputContainer.Add(port);
                }
            }

            ApplyKindVisuals();
        }

        private static void ApplyTitleTextColorRecursive(VisualElement root, Color color)
        {
            if (root == null)
                return;
            if (root is TextElement te)
                te.style.color = color;
            foreach (var child in root.Children())
                ApplyTitleTextColorRecursive(child, color);
        }

        /// <summary>
        /// 将当前 GraphView 矩形写回资产（不写 Undo，由调用方统一 RecordUndo）。
        /// </summary>
        public void WriteLayoutToDataWithoutUndo()
        {
            if (_owner == null || Data == null)
                return;
            var r = GetPosition();
            if (r.width < 8f || r.height < 8f)
                return;
            Data.SetEditorLayoutFromRect(r);
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max)
                return s;
            return s.Substring(0, max - 1) + "…";
        }

        public readonly struct PortUserData
        {
            public readonly string NodeId;
            public readonly int ChoiceIndex;
            public readonly bool IsInput;

            public PortUserData(string nodeId, int choiceIndex, bool isInput)
            {
                NodeId = nodeId;
                ChoiceIndex = choiceIndex;
                IsInput = isInput;
            }
        }
    }
}
