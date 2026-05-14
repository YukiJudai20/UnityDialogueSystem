using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.Dialogue.Editor
{
    /// <summary>
    /// 对话图主画布：负责节点/边与 <see cref="DialogueGraphAsset"/> 的双向同步。
    /// </summary>
    public sealed class DialogueGraphView : GraphView
    {
        private readonly DialogueGraphEditorWindow _window;
        private DialogueGraphAsset _graph;
        private readonly Dictionary<string, DialogueNodeView> _byId = new Dictionary<string, DialogueNodeView>();
        private int _suppressGraphCallbacks;

        private void BeginSuppressGraphCallbacks()
        {
            _suppressGraphCallbacks++;
        }

        private void EndSuppressGraphCallbacks()
        {
            if (_suppressGraphCallbacks > 0)
                _suppressGraphCallbacks--;
        }

        private bool IsGraphCallbacksSuppressed => _suppressGraphCallbacks > 0;

        public DialogueGraphView(DialogueGraphEditorWindow window)
        {
            _window = window;
            DialogueNodeView.TryAddStylesTo(this);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());

            var grid = new GridBackground();
            grid.AddToClassList("dg-graph-grid");
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1;
            graphViewChanged += OnGraphViewChanged;

            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            SetupContextMenu();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete)
                return;
            DeleteSelection();
            evt.StopPropagation();
        }

        private void SetupContextMenu()
        {
            ((VisualElement)this).AddManipulator(new ContextualMenuManipulator(menuEvent =>
            {
                var ve = (VisualElement)this;
                var localOnGraph = contentViewContainer.WorldToLocal(ve.LocalToWorld(menuEvent.localMousePosition));
                menuEvent.menu.AppendAction(
                    "添加对话节点",
                    _ => AddNodeAt(localOnGraph, DialogueNodeKind.Dialogue));
                menuEvent.menu.AppendAction(
                    "添加分支节点",
                    _ => AddNodeAt(localOnGraph, DialogueNodeKind.Branch));
                menuEvent.menu.AppendAction(
                    "添加选项节点",
                    _ => AddNodeAt(localOnGraph, DialogueNodeKind.Option));
                menuEvent.menu.AppendAction(
                    "添加结束节点",
                    _ => AddNodeAt(localOnGraph, DialogueNodeKind.End));
            }));
        }

        public void Bind(DialogueGraphAsset graph)
        {
            if (_graph != null && _graph != graph)
                PersistAllNodeLayoutsToAsset();
            _graph = graph;
            ReloadFromAsset();
        }

        public void ReloadFromAsset()
        {
            BeginSuppressGraphCallbacks();
            try
            {
                foreach (var e in edges.ToList())
                    RemoveElement(e);
                foreach (var n in nodes.ToList())
                    RemoveElement(n);
                _byId.Clear();

                if (_graph == null || _graph.Nodes == null)
                    return;

                foreach (var data in _graph.Nodes)
                {
                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;
                    var nodeView = new DialogueNodeView(_graph, data);
                    nodeView.SetPosition(data.GetEditorLayoutRectOrDefault());
                    AddElement(nodeView);
                    _byId[data.Id] = nodeView;
                }

                RebuildEdgesFromAsset();
            }
            finally
            {
                EndSuppressGraphCallbacks();
            }
        }

        public void RebuildEdgesFromAsset()
        {
            BeginSuppressGraphCallbacks();
            try
            {
                var toRemove = edges.ToList();
                foreach (var e in toRemove)
                    RemoveElement(e);

                if (_graph?.Nodes == null)
                    return;

                foreach (var data in _graph.Nodes)
                {
                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;
                    if (!_byId.TryGetValue(data.Id, out var fromNode))
                        continue;
                    var outCount = data.GetOutputPortCount();
                    for (var ci = 0; ci < outCount; ci++)
                    {
                        var tid = data.GetOutputTargetId(ci);
                        if (string.IsNullOrEmpty(tid))
                            continue;
                        if (!_byId.TryGetValue(tid, out var toNode))
                            continue;
                        var outPort = GetOutputPort(fromNode, ci);
                        var inPort = GetInputPort(toNode);
                        if (outPort == null || inPort == null)
                            continue;
                        var edge = outPort.ConnectTo(inPort);
                        AddElement(edge);
                    }
                }
            }
            finally
            {
                EndSuppressGraphCallbacks();
            }
        }

        public void RefreshAllNodePorts()
        {
            foreach (var n in nodes.OfType<DialogueNodeView>())
                n.RebuildChoicePorts();
            RebuildEdgesFromAsset();
        }

        private static Port GetInputPort(DialogueNodeView node)
        {
            foreach (var p in node.inputContainer.Query<Port>().ToList())
            {
                if (p.portName == DialogueNodeView.InputPortName)
                    return p;
            }

            return null;
        }

        private static Port GetOutputPort(DialogueNodeView node, int choiceIndex)
        {
            var ports = node.outputContainer.Query<Port>().ToList();
            return choiceIndex >= 0 && choiceIndex < ports.Count ? ports[choiceIndex] : null;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var list = new List<Port>();
            if (startPort.userData is not DialogueNodeView.PortUserData startData)
                return list;

            foreach (var node in nodes.ToList())
            {
                if (node is not DialogueNodeView)
                    continue;
                CollectPorts(node.inputContainer, startPort, startData, list);
                CollectPorts(node.outputContainer, startPort, startData, list);
            }

            return list;
        }

        private static void CollectPorts(VisualElement container, Port startPort, DialogueNodeView.PortUserData startData, List<Port> list)
        {
            container.Query<Port>().ForEach(port =>
            {
                if (port.node == startPort.node)
                    return;
                if (port.userData is not DialogueNodeView.PortUserData other)
                    return;
                if (startData.IsInput == other.IsInput)
                    return;
                list.Add(port);
            });
        }

        public void PersistAllNodeLayoutsToAsset()
        {
            if (_graph == null)
                return;
            var changed = false;
            foreach (var n in nodes.ToList().OfType<DialogueNodeView>())
            {
                var r = n.GetPosition();
                if (r.width < 8f || r.height < 8f)
                    continue;
                var oldRect = n.Data.GetEditorLayoutRectOrDefault();
                if ((r.position - oldRect.position).sqrMagnitude < 0.0001f &&
                    Mathf.Abs(r.width - oldRect.width) < 0.5f &&
                    Mathf.Abs(r.height - oldRect.height) < 0.5f)
                    continue;
                n.WriteLayoutToDataWithoutUndo();
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(_graph);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (IsGraphCallbacksSuppressed || _graph == null)
                return change;

            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove.ToList())
                {
                    if (el is Edge edge)
                        ClearChoiceTarget(edge);
                    else if (el is DialogueNodeView nodeView)
                        RemoveNodeAsset(nodeView);
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                    ApplyChoiceTarget(edge);
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                var moved = change.movedElements.OfType<DialogueNodeView>().Where(n => n != null).ToList();
                if (moved.Count > 0)
                {
                    Undo.RecordObject(_graph, "移动对话节点");
                    foreach (var nv in moved)
                        nv.WriteLayoutToDataWithoutUndo();
                }
            }

            EditorUtility.SetDirty(_graph);
            _window.OnGraphStructureChanged();
            return change;
        }

        private void ClearChoiceTarget(Edge edge)
        {
            if (edge.output?.userData is not DialogueNodeView.PortUserData outData || outData.IsInput || outData.ChoiceIndex < 0)
                return;
            var node = _graph.FindNode(outData.NodeId);
            if (node == null || outData.ChoiceIndex >= node.GetOutputPortCount())
                return;
            Undo.RecordObject(_graph, "断开对话连线");
            node.SetOutputTargetId(outData.ChoiceIndex, string.Empty);
        }

        private void ApplyChoiceTarget(Edge edge)
        {
            if (edge.output?.userData is not DialogueNodeView.PortUserData outData || outData.IsInput || outData.ChoiceIndex < 0)
                return;
            if (edge.input?.userData is not DialogueNodeView.PortUserData inData || !inData.IsInput)
                return;

            var fromNode = _graph.FindNode(outData.NodeId);
            if (fromNode == null || outData.ChoiceIndex < 0 || outData.ChoiceIndex >= fromNode.GetOutputPortCount())
                return;

            Undo.RecordObject(_graph, "连接对话节点");

            fromNode.SetOutputTargetId(outData.ChoiceIndex, inData.NodeId);

            BeginSuppressGraphCallbacks();
            try
            {
                foreach (var e in edges.ToList())
                {
                    if (e == edge)
                        continue;
                    if (e.output == edge.output)
                        RemoveElement(e);
                }
            }
            finally
            {
                EndSuppressGraphCallbacks();
            }
        }

        private void RemoveNodeAsset(DialogueNodeView nodeView)
        {
            if (_graph?.Nodes == null || nodeView.Data == null)
                return;
            var id = nodeView.Data.Id;
            Undo.RecordObject(_graph, "删除对话节点");
            _graph.Nodes.RemoveAll(n => n != null && n.Id == id);
            var validIds = new HashSet<string>(_graph.Nodes.Where(x => x != null && !string.IsNullOrEmpty(x.Id)).Select(x => x.Id));
            foreach (var n in _graph.Nodes)
            {
                if (n == null)
                    continue;
                n.SanitizeReferences(validIds);
            }

            if (_graph.EntryNodeId == id)
                _graph.EntryNodeId = _graph.Nodes.FirstOrDefault(x => x != null)?.Id ?? string.Empty;

            _byId.Remove(id);
        }

        public DialogueNodeView AddNodeAt(Vector2 graphPosition, DialogueNodeKind kind = DialogueNodeKind.Dialogue)
        {
            if (_graph == null)
                return null;

            Undo.RecordObject(_graph, "添加对话节点");
            DialogueNodeBase data = kind switch
            {
                DialogueNodeKind.Branch => new BranchNode
                {
                    Title = "分支",
                },
                DialogueNodeKind.Option => new OptionNode
                {
                    Title = "选项",
                    PopupMessage = "请选择：",
                },
                DialogueNodeKind.End => new EndNode
                {
                    Title = "结束",
                },
                _ => new DialogueNode
                {
                    Title = "对话",
                },
            };

            data.Id = Guid.NewGuid().ToString("N");
            data.GraphPosition = graphPosition;
            data.GraphSize = new Vector2(DialogueNodeBase.DefaultEditorNodeWidth, DialogueNodeBase.DefaultEditorNodeHeight);

            _graph.Nodes.Add(data);
            if (string.IsNullOrEmpty(_graph.EntryNodeId))
                _graph.EntryNodeId = data.Id;

            EditorUtility.SetDirty(_graph);
            var view = new DialogueNodeView(_graph, data);
            view.SetPosition(data.GetEditorLayoutRectOrDefault());
            AddElement(view);
            _byId[data.Id] = view;
            _window.OnGraphStructureChanged();
            return view;
        }

        public IReadOnlyDictionary<string, DialogueNodeView> NodesById => _byId;
    }
}
