using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.Dialogue.Editor
{
    /// <summary>
    /// 节点式对话编辑器：左侧 GraphView（UI Toolkit），右侧 Odin + IMGUI 属性面板。
    /// </summary>
    public sealed class DialogueGraphEditorWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Scripts/Dialogue/Editor/DialogueGraphEditor.uxml";
        private const string UssPath = "Assets/Scripts/Dialogue/Editor/DialogueGraphEditor.uss";

        [SerializeField]
        private DialogueGraphAsset graph;

        private SerializedObject serializedGraph;
        private PropertyTree propertyTree;
        private DialogueGraphView graphView;
        private IMGUIContainer imgui;
        private VisualElement graphHost;

        [MenuItem("Window/DialogueSystem/对话图编辑器")]
        public static void Open()
        {
            var wnd = GetWindow<DialogueGraphEditorWindow>();
            wnd.titleContent = new GUIContent("对话图");
            wnd.minSize = new Vector2(960, 520);
        }

        public static void Open(DialogueGraphAsset asset)
        {
            var wnd = GetWindow<DialogueGraphEditorWindow>();
            wnd.titleContent = new GUIContent("对话图");
            wnd.minSize = new Vector2(960, 520);
            wnd.ApplyGraph(asset);
        }

        private void ApplyGraph(DialogueGraphAsset asset)
        {
            graph = asset;
            var objectField = rootVisualElement?.Q<ObjectField>("graph-object");
            if (objectField != null)
                objectField.SetValueWithoutNotify(graph);
            RebuildSerializedTree();
            graphView?.Bind(graph);
            Repaint();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new Label($"未找到 UXML：{UxmlPath}"));
                return;
            }

            uxml.CloneTree(rootVisualElement);
            rootVisualElement.style.flexGrow = 1;

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (uss != null)
                rootVisualElement.styleSheets.Add(uss);

            var objectField = rootVisualElement.Q<ObjectField>("graph-object");
            if (objectField != null)
            {
                objectField.objectType = typeof(DialogueGraphAsset);
                objectField.allowSceneObjects = false;
                objectField.value = graph;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    graph = evt.newValue as DialogueGraphAsset;
                    RebuildSerializedTree();
                    graphView?.Bind(graph);
                });
            }

            rootVisualElement.Q<Button>("btn-new")?.RegisterCallback<ClickEvent>(_ => CreateNewGraphAsset());
            rootVisualElement.Q<Button>("btn-add-node")?.RegisterCallback<ClickEvent>(_ => AddNodeAtViewportCenter(DialogueNodeKind.Dialogue));
            rootVisualElement.Q<Button>("btn-add-branch")?.RegisterCallback<ClickEvent>(_ => AddNodeAtViewportCenter(DialogueNodeKind.Branch));
            rootVisualElement.Q<Button>("btn-add-option")?.RegisterCallback<ClickEvent>(_ => AddNodeAtViewportCenter(DialogueNodeKind.Option));
            rootVisualElement.Q<Button>("btn-add-end")?.RegisterCallback<ClickEvent>(_ => AddNodeAtViewportCenter(DialogueNodeKind.End));
            rootVisualElement.Q<Button>("btn-refresh")?.RegisterCallback<ClickEvent>(_ =>
            {
                graphView?.Bind(graph);
            });

            graphHost = rootVisualElement.Q<VisualElement>("graph-host");
            graphView = new DialogueGraphView(this);
            graphHost.Add(graphView);
            graphView.StretchToParentSize();

            imgui = rootVisualElement.Q<IMGUIContainer>("inspector-imgui");
            if (imgui != null)
            {
                imgui.onGUIHandler += DrawInspectorImGui;
                imgui.style.flexGrow = 1;
                imgui.style.minHeight = 320;
                imgui.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                imgui.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
            }

            RebuildSerializedTree();
            graphView.Bind(graph);
        }

        private void OnDisable()
        {
            graphView?.PersistAllNodeLayoutsToAsset();
            EditorApplication.delayCall -= DeferredGraphVisualRefresh;
            DisposeTrees();
            if (imgui != null)
                imgui.onGUIHandler -= DrawInspectorImGui;
        }

        private void DisposeTrees()
        {
            propertyTree?.Dispose();
            propertyTree = null;
            serializedGraph = null;
        }

        private void RebuildSerializedTree()
        {
            DisposeTrees();
            if (graph == null)
                return;
            serializedGraph = new SerializedObject(graph);
            propertyTree = PropertyTree.Create(serializedGraph);
        }

        /// <summary>
        /// 图数据变更时只更新序列化树，避免在 Odin 下拉/弹窗期间 Dispose PropertyTree（会破坏 IMGUI 控件栈）。
        /// </summary>
        public void OnGraphStructureChanged()
        {
            if (graph == null)
                return;
            if (serializedGraph == null || serializedGraph.targetObject != graph)
                RebuildSerializedTree();
            else
            {
                serializedGraph.Update();
                propertyTree?.UpdateTree();
            }

            ScheduleDeferredGraphVisualRefresh();
            Repaint();
        }

        private void ScheduleDeferredGraphVisualRefresh()
        {
            EditorApplication.delayCall -= DeferredGraphVisualRefresh;
            EditorApplication.delayCall += DeferredGraphVisualRefresh;
        }

        private void DeferredGraphVisualRefresh()
        {
            if (this == null || graphView == null)
                return;
            graphView.RefreshAllNodePorts();
            foreach (var n in graphView.nodes.ToList().OfType<DialogueNodeView>())
            {
                n.SyncTitleFromData();
                n.ApplyKindVisuals();
            }
        }

        private void CreateNewGraphAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "新建对话图",
                "DialogueGraph",
                "asset",
                "选择保存路径");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<DialogueGraphAsset>();
            var first = new DialogueNode
            {
                Id = System.Guid.NewGuid().ToString("N"),
                Title = "开始",
                GraphPosition = new Vector2(80, 120),
                GraphSize = new Vector2(DialogueNodeBase.DefaultEditorNodeWidth, DialogueNodeBase.DefaultEditorNodeHeight),
            };

            asset.Nodes.Add(first);
            asset.EntryNodeId = first.Id;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            ApplyGraph(asset);
        }

        private void AddNodeAtViewportCenter(DialogueNodeKind kind)
        {
            if (graphView == null || graph == null)
                return;
            var local = graphView.contentViewContainer.WorldToLocal(graphHost.worldBound.center);
            graphView.AddNodeAt(local, kind);
        }

        private void DrawInspectorImGui()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            try
            {
                if (graph == null)
                {
                    EditorGUILayout.HelpBox("请指定或新建「对话图」资源。", MessageType.Info);
                    return;
                }

                if (serializedGraph == null || serializedGraph.targetObject != graph)
                    RebuildSerializedTree();
                if (serializedGraph == null || propertyTree == null)
                    return;

                serializedGraph.Update();
                propertyTree.UpdateTree();

                var selected = graphView?.selection?.OfType<DialogueNodeView>().FirstOrDefault();
                var selectedIndex = -1;
                if (selected != null)
                    selectedIndex = graph.IndexOfNode(selected.Data.Id);

                if (selectedIndex >= 0)
                {
                    EditorGUILayout.LabelField("选中节点", EditorStyles.boldLabel);
                    DrawOdinNodeOrFallback(selectedIndex);
                }
                else
                {
                    EditorGUILayout.LabelField("对话图", EditorStyles.boldLabel);
                    propertyTree.Draw(false);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }

            if (graph != null && serializedGraph != null && serializedGraph.targetObject == graph)
            {
                if (serializedGraph.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(graph);
                    ScheduleDeferredGraphVisualRefresh();
                }
            }
        }

        private void DrawOdinNodeOrFallback(int nodeIndex)
        {
            try
            {
                InspectorProperty nodesProp = null;
                foreach (var child in propertyTree.RootProperty.Children)
                {
                    if (child.Name == "Nodes")
                    {
                        nodesProp = child;
                        break;
                    }
                }

                if (nodesProp != null)
                {
                    var i = 0;
                    foreach (var child in nodesProp.Children)
                    {
                        if (i == nodeIndex)
                        {
                            if (child.State != null)
                                child.State.Expanded = true;
                            child.Draw();
                            return;
                        }

                        i++;
                    }
                }
            }
            catch
            {
                // Odin 属性树结构因版本差异可能不同，退回 Unity 默认绘制。
            }

            var sp = serializedGraph.FindProperty("Nodes");
            if (sp != null && nodeIndex < sp.arraySize)
            {
                var elem = sp.GetArrayElementAtIndex(nodeIndex);
                elem.isExpanded = true;
                EditorGUILayout.PropertyField(elem, true);
            }
        }
    }
}
