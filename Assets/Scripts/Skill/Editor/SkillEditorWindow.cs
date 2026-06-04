using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.Skill.Editor
{
    /// <summary>
    /// 技能时间轴编辑器：轨道列表 + 帧时间轴 + 右侧片段属性面板。
    /// </summary>
    public sealed class SkillEditorWindow : EditorWindow
    {
        const string UxmlPath = "Assets/Scripts/Skill/Editor/SkillEditor.uxml";
        const string UssPath = "Assets/Scripts/Skill/Editor/SkillEditor.uss";

        [SerializeField]
        SkillModel mSkill;

        SerializedObject mSerializedSkill;
        PropertyTree mPropertyTree;
        SkillEditorTimeline mTimeline;
        IMGUIContainer mInspectorImgui;
        Slider mZoomSlider;

        public SkillModel Skill => mSkill;
        public SerializedObject SerializedSkill => mSerializedSkill;
        public int PlayheadFrame => mTimeline?.PlayheadFrame ?? 0;

        [MenuItem("Window/SkillSystem/技能编辑器")]
        public static void Open()
        {
            var window = GetWindow<SkillEditorWindow>();
            window.titleContent = new GUIContent("技能编辑器");
            window.minSize = new Vector2(900, 480);
        }

        public static void Open(SkillModel skill)
        {
            var window = GetWindow<SkillEditorWindow>();
            window.titleContent = new GUIContent("技能编辑器");
            window.minSize = new Vector2(900, 480);
            window.ApplySkill(skill, resetZoom: true);
        }

        public void ApplySkill(SkillModel skill, bool resetZoom = false)
        {
            mSkill = skill;
            var objectField = rootVisualElement?.Q<ObjectField>("skill-object");
            if (objectField != null)
                objectField.SetValueWithoutNotify(mSkill);

            RebuildSerializedTree();
            ClearInspectorToSkill();

            if (resetZoom)
                ApplyDefaultZoom();

            mTimeline?.ResetPlayhead(mSkill != null ? mSkill.GetClampedFrameCount() : 1);
            RefreshTimeline();
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

            var objectField = rootVisualElement.Q<ObjectField>("skill-object");
            if (objectField != null)
            {
                objectField.objectType = typeof(SkillModel);
                objectField.allowSceneObjects = false;
                objectField.value = mSkill;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    mSkill = evt.newValue as SkillModel;
                    RebuildSerializedTree();
                    ClearInspectorToSkill();
                    RefreshTimeline();
                });
                objectField.RegisterCallback<PointerDownEvent>(_ => ClearInspectorToSkill());
                objectField.RegisterCallback<FocusInEvent>(_ => ClearInspectorToSkill());
            }

            rootVisualElement.Q<Button>("btn-play")?.RegisterCallback<ClickEvent>(_ => { });
            rootVisualElement.Q<Button>("btn-pause")?.RegisterCallback<ClickEvent>(_ => { });

            mZoomSlider = rootVisualElement.Q<Slider>("zoom-slider");
            if (mZoomSlider != null)
            {
                mZoomSlider.lowValue = SkillEditorUtils.MinPixelsPerFrame;
                mZoomSlider.highValue = SkillEditorUtils.MaxPixelsPerFrame;
                mZoomSlider.RegisterValueChangedCallback(evt =>
                {
                    if (mTimeline != null)
                        mTimeline.SetPixelsPerFrame(evt.newValue);
                });
            }

            var timelineHost = rootVisualElement.Q<VisualElement>("timeline-host");
            mTimeline = new SkillEditorTimeline(this);
            timelineHost.Add(mTimeline);
            mTimeline.style.flexGrow = 1;

            mInspectorImgui = rootVisualElement.Q<IMGUIContainer>("inspector-imgui");
            if (mInspectorImgui != null)
            {
                mInspectorImgui.onGUIHandler += DrawInspectorImGui;
                mInspectorImgui.style.flexGrow = 1;
                mInspectorImgui.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                mInspectorImgui.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
            }

            ApplyDefaultZoom();

            RebuildSerializedTree();
            RefreshTimeline();
        }

        void ApplyDefaultZoom()
        {
            var zoom = SkillEditorUtils.DefaultPixelsPerFrame;
            if (mZoomSlider != null)
            {
                mZoomSlider.lowValue = SkillEditorUtils.MinPixelsPerFrame;
                mZoomSlider.highValue = SkillEditorUtils.MaxPixelsPerFrame;
                mZoomSlider.SetValueWithoutNotify(zoom);
            }

            mTimeline?.SetPixelsPerFrame(zoom);
        }

        void OnDisable()
        {
            if (mSerializedSkill != null && mSkill != null)
            {
                mSerializedSkill.ApplyModifiedProperties();
                EditorUtility.SetDirty(mSkill);
            }

            DisposeTrees();
            if (mInspectorImgui != null)
                mInspectorImgui.onGUIHandler -= DrawInspectorImGui;
        }

        void DisposeTrees()
        {
            mPropertyTree?.Dispose();
            mPropertyTree = null;
            mSerializedSkill = null;
        }

        void RebuildSerializedTree()
        {
            DisposeTrees();
            if (mSkill == null)
                return;

            mSerializedSkill = new SerializedObject(mSkill);
            mPropertyTree = PropertyTree.Create(mSerializedSkill);
        }

        public void OnTimelineSelectionChanged() => Repaint();

        public void RefreshTimeline() => mTimeline?.MarkDirtyRepaint();

        public void SyncZoomSlider(float pixelsPerFrame)
        {
            if (mZoomSlider != null)
                mZoomSlider.SetValueWithoutNotify(pixelsPerFrame);
        }

        public void ClearInspectorToSkill()
        {
            mTimeline?.SetSelection(SkillEditorSelection.None, false);
            Repaint();
        }

        void DrawInspectorImGui()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            try
            {
                if (mSkill == null)
                {
                    EditorGUILayout.HelpBox("请拖入或指定技能资产。", MessageType.Info);
                    return;
                }

                if (mSerializedSkill == null || mSerializedSkill.targetObject != mSkill)
                    RebuildSerializedTree();
                if (mSerializedSkill == null || mPropertyTree == null)
                    return;

                mSerializedSkill.Update();
                mPropertyTree.UpdateTree();

                var selection = mTimeline?.Selection ?? SkillEditorSelection.None;

                if (selection.IsClip)
                {
                    EditorGUILayout.LabelField("片段属性", EditorStyles.boldLabel);
                    DrawClipInspector(selection.TrackIndex, selection.ClipIndex);
                }
                else if (selection.IsTrack)
                {
                    EditorGUILayout.LabelField("轨道属性", EditorStyles.boldLabel);
                    DrawTrackInspector(selection.TrackIndex);
                }
                else
                {
                    EditorGUILayout.LabelField("技能", EditorStyles.boldLabel);
                    mPropertyTree.Draw(false);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }

            if (mSkill == null || mSerializedSkill == null || mSerializedSkill.targetObject != mSkill)
                return;

            if (mSerializedSkill.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(mSkill);
                RefreshTimeline();
            }
        }

        void DrawClipInspector(int trackIndex, int clipIndex)
        {
            var clipProp = SkillEditorUtils.GetClipProperty(mSerializedSkill, trackIndex, clipIndex);
            if (clipProp == null)
            {
                EditorGUILayout.HelpBox("无法定位片段属性。", MessageType.Warning);
                return;
            }

            clipProp.isExpanded = true;
            EditorGUILayout.PropertyField(clipProp, true);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("删除片段"))
            {
                SkillEditorUtils.RemoveClip(mSkill, mSerializedSkill, trackIndex, clipIndex);
                mTimeline.SetSelection(SkillEditorSelection.None);
                RefreshTimeline();
            }
        }

        void DrawTrackInspector(int trackIndex)
        {
            var trackProp = SkillEditorUtils.GetTrackProperty(mSerializedSkill, trackIndex);
            if (trackProp == null)
            {
                EditorGUILayout.HelpBox("无法定位轨道属性。", MessageType.Warning);
                return;
            }

            trackProp.isExpanded = true;
            EditorGUILayout.PropertyField(trackProp, true);
        }
    }
}
