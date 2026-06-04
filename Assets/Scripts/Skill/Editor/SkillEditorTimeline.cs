using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.Skill.Editor
{
    internal enum SkillEditorSelectionKind
    {
        None,
        Skill,
        Track,
        Clip,
    }

    enum ClipDragMode
    {
        None,
        Move,
        ResizeStart,
        ResizeEnd,
    }

    struct ClipDragState
    {
        public ClipDragMode Mode;
        public int TrackIndex;
        public int ClipIndex;
        public int GrabFrame;
        public int OrigStart;
        public int OrigEnd;

        public bool Active => Mode != ClipDragMode.None;
    }

    struct TrackDragState
    {
        public bool Pending;
        public bool Active;
        public int SourceIndex;
        public int InsertBeforeIndex;
        public Vector2 StartMouse;

        public bool IsBusy => Pending || Active;
    }

    internal readonly struct SkillEditorSelection : IEquatable<SkillEditorSelection>
    {
        public static readonly SkillEditorSelection None = new(SkillEditorSelectionKind.None, -1, -1);

        public SkillEditorSelectionKind Kind { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }

        public SkillEditorSelection(SkillEditorSelectionKind kind, int trackIndex, int clipIndex)
        {
            Kind = kind;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
        }

        public bool IsClip => Kind == SkillEditorSelectionKind.Clip;
        public bool IsTrack => Kind == SkillEditorSelectionKind.Track;

        public bool Equals(SkillEditorSelection other) =>
            Kind == other.Kind && TrackIndex == other.TrackIndex && ClipIndex == other.ClipIndex;

        public override bool Equals(object obj) => obj is SkillEditorSelection other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)Kind, TrackIndex, ClipIndex);
    }

    /// <summary>
    /// 左侧轨道列表 + 右侧帧时间轴（IMGUI）。
    /// </summary>
    internal sealed class SkillEditorTimeline : VisualElement
    {
        readonly SkillEditorWindow mWindow;
        readonly IMGUIContainer mGui;

        Vector2 mScroll;
        float mPixelsPerFrame = SkillEditorUtils.DefaultPixelsPerFrame;
        SkillEditorSelection mSelection = SkillEditorSelection.None;
        ClipDragState mClipDrag;
        TrackDragState mTrackDrag;
        int mPlayheadFrame;
        bool mPlayheadDragging;

        const float TrackDragThreshold = 4f;

        public int PlayheadFrame => mPlayheadFrame;

        public float PixelsPerFrame
        {
            get => mPixelsPerFrame;
            set => mPixelsPerFrame = Mathf.Clamp(value, SkillEditorUtils.MinPixelsPerFrame, SkillEditorUtils.MaxPixelsPerFrame);
        }

        public SkillEditorSelection Selection => mSelection;

        public SkillEditorTimeline(SkillEditorWindow window)
        {
            mWindow = window;
            style.flexGrow = 1;

            mGui = new IMGUIContainer(DrawImGui)
            {
                style = { flexGrow = 1 },
            };
            Add(mGui);

            RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
        }

        public void SetSelection(SkillEditorSelection selection, bool notify = true)
        {
            if (mSelection.Equals(selection))
                return;

            mSelection = selection;
            if (notify)
                mWindow.OnTimelineSelectionChanged();
            mGui.MarkDirtyRepaint();
        }

        public void SetPixelsPerFrame(float value)
        {
            PixelsPerFrame = value;
            mGui.MarkDirtyRepaint();
        }

        public void MarkDirtyRepaint() => mGui?.MarkDirtyRepaint();

        public void SetPlayheadFrame(int frame, int frameCount)
        {
            var lastFrame = Mathf.Max(0, frameCount - 1);
            mPlayheadFrame = Mathf.Clamp(frame, 0, lastFrame);
            mGui?.MarkDirtyRepaint();
        }

        public void ResetPlayhead(int frameCount) => SetPlayheadFrame(0, frameCount);

        void OnWheel(WheelEvent evt)
        {
            if (!ContainsPoint(evt.localMousePosition))
                return;

            var delta = evt.delta.y > 0f ? -1f : 1f;
            PixelsPerFrame = Mathf.Clamp(
                PixelsPerFrame + delta,
                SkillEditorUtils.MinPixelsPerFrame,
                SkillEditorUtils.MaxPixelsPerFrame);
            mWindow.SyncZoomSlider(PixelsPerFrame);
            evt.StopPropagation();
            mGui.MarkDirtyRepaint();
        }

        void DrawImGui()
        {
            var model = mWindow.Skill;
            var skillObject = mWindow.SerializedSkill;
            if (model == null || skillObject == null)
            {
                EditorGUILayout.HelpBox("请指定技能资产。", MessageType.Info);
                return;
            }

            skillObject.Update();
            var frameCount = model.GetClampedFrameCount();
            var trackCount = model.Tracks?.Count ?? 0;
            var timelineWidth = frameCount * PixelsPerFrame + SkillEditorUtils.TimelinePaddingLeft;
            var contentWidth = SkillEditorUtils.TrackListWidth + timelineWidth + 24f;

            var viewportHeight = Mathf.Max(mGui.contentRect.height, SkillEditorUtils.RulerHeight + SkillEditorUtils.TrackRowHeight);
            var tracksAreaHeight = Mathf.Max(
                Mathf.Max(1, trackCount) * SkillEditorUtils.TrackRowHeight,
                viewportHeight - SkillEditorUtils.RulerHeight - 8f);
            var contentHeight = SkillEditorUtils.RulerHeight + tracksAreaHeight + 8f;

            mScroll = GUILayout.BeginScrollView(mScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var contentRect = GUILayoutUtility.GetRect(contentWidth, contentHeight);
            DrawTimelineContent(model, skillObject, contentRect, frameCount, trackCount);
            DrawTrackReorderIndicator(contentRect, trackCount);
            HandleTrackReorderEvents(model, skillObject, contentRect, frameCount);
            DrawPlayhead(contentRect, frameCount);
            HandlePlayheadEvents(contentRect, frameCount);
            HandleClipDragEvents(model, skillObject, contentRect, frameCount, trackCount);

            if (mClipDrag.Active || mTrackDrag.Active)
                EditorGUIUtility.AddCursorRect(contentRect, MouseCursor.MoveArrow);

            if (Event.current.type == EventType.ContextClick && contentRect.Contains(Event.current.mousePosition) &&
                !mClipDrag.Active && !mTrackDrag.IsBusy && !mPlayheadDragging)
            {
                var local = Event.current.mousePosition - contentRect.position;
                if (local.x < SkillEditorUtils.TrackListWidth)
                {
                    var tracksTop = contentRect.y + SkillEditorUtils.RulerHeight;
                    var trackIndex = GetTrackIndexFromMouseY(tracksTop, trackCount, Event.current.mousePosition.y);
                    ShowTrackListContextMenu(model, skillObject, trackIndex);
                }
                else
                    ShowTimelineContextMenu(model, skillObject, local, frameCount, trackCount);
                Event.current.Use();
            }

            GUILayout.EndScrollView();

            if (skillObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(model);
                mGui.MarkDirtyRepaint();
            }
        }

        void DrawTimelineContent(SkillModel model, SerializedObject skillObject, Rect contentRect, int frameCount, int trackCount)
        {
            var tracks = model.Tracks;
            var timelineLeft = contentRect.x + SkillEditorUtils.TrackListWidth;
            var timelineWidth = frameCount * PixelsPerFrame + SkillEditorUtils.TimelinePaddingLeft;

            var listRect = new Rect(contentRect.x, contentRect.y, SkillEditorUtils.TrackListWidth, contentRect.height);
            var rulerRect = new Rect(timelineLeft, contentRect.y, timelineWidth, SkillEditorUtils.RulerHeight);
            var tracksTop = contentRect.y + SkillEditorUtils.RulerHeight;

            DrawTrackList(model, skillObject, listRect, tracksTop, trackCount);
            DrawRuler(rulerRect, frameCount);
            DrawTrackRows(model, tracks, new Rect(timelineLeft, tracksTop, timelineWidth, contentRect.height - SkillEditorUtils.RulerHeight), frameCount, trackCount);
        }

        void DrawTrackList(SkillModel model, SerializedObject skillObject, Rect listRect, float tracksTop, int trackCount)
        {
            EditorGUI.DrawRect(listRect, new Color(0.22f, 0.22f, 0.24f));

            var headerRect = new Rect(listRect.x, listRect.y, listRect.width, SkillEditorUtils.RulerHeight);
            EditorGUI.DrawRect(headerRect, new Color(0.26f, 0.26f, 0.28f));
            GUI.Label(headerRect, "轨道", EditorStyles.boldLabel);

            var bodyRect = new Rect(listRect.x, tracksTop, listRect.width, listRect.yMax - tracksTop);
            EditorGUI.DrawRect(bodyRect, new Color(0.2f, 0.2f, 0.22f));

            for (var i = 0; i < trackCount; i++)
            {
                var track = model.Tracks[i];
                var fullRowRect = new Rect(
                    listRect.x,
                    tracksTop + i * SkillEditorUtils.TrackRowHeight,
                    listRect.width,
                    SkillEditorUtils.TrackRowHeight);

                var isDraggingThis = mTrackDrag.IsBusy && mTrackDrag.SourceIndex == i;
                var isSelected = mSelection.IsTrack && mSelection.TrackIndex == i && !isDraggingThis;
                DrawTrackRowChrome(fullRowRect, i, track, isSelected, isDraggingThis ? 0.45f : 1f);

                var rowRect = new Rect(
                    fullRowRect.x + SkillEditorUtils.TrackRowAccentWidth + 4f,
                    fullRowRect.y + 4f,
                    fullRowRect.width - SkillEditorUtils.TrackRowAccentWidth - 8f,
                    fullRowRect.height - SkillEditorUtils.TrackRowSeparatorHeight - 8f);

                var label = track != null ? track.DisplayName : "(空)";
                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = isDraggingThis ? new Color(1f, 1f, 1f, 0.5f) : Color.white },
                };
                GUI.Label(rowRect, label, labelStyle);

                if (Event.current.type == EventType.ContextClick && fullRowRect.Contains(Event.current.mousePosition) &&
                    !mTrackDrag.IsBusy)
                {
                    ShowTrackListContextMenu(model, skillObject, i);
                    Event.current.Use();
                }
            }
        }

        void DrawTrackReorderIndicator(Rect contentRect, int trackCount)
        {
            if (!mTrackDrag.Active || trackCount == 0)
                return;

            var listRect = new Rect(contentRect.x, contentRect.y, SkillEditorUtils.TrackListWidth, contentRect.height);
            var tracksTop = contentRect.y + SkillEditorUtils.RulerHeight;
            var y = tracksTop + mTrackDrag.InsertBeforeIndex * SkillEditorUtils.TrackRowHeight;
            EditorGUI.DrawRect(
                new Rect(listRect.x + 2f, y - 1f, listRect.width - 4f, 2f),
                new Color(0.35f, 0.75f, 1f, 0.95f));
        }

        void HandleTrackReorderEvents(SkillModel model, SerializedObject skillObject, Rect contentRect, int trackCount)
        {
            var listRect = new Rect(contentRect.x, contentRect.y, SkillEditorUtils.TrackListWidth, contentRect.height);
            var tracksTop = contentRect.y + SkillEditorUtils.RulerHeight;

            if (mTrackDrag.IsBusy)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    if (!mTrackDrag.Active &&
                        Vector2.Distance(Event.current.mousePosition, mTrackDrag.StartMouse) >= TrackDragThreshold)
                        mTrackDrag.Active = true;

                    if (mTrackDrag.Active)
                    {
                        mTrackDrag.InsertBeforeIndex = GetTrackInsertBeforeIndex(tracksTop, trackCount, Event.current.mousePosition.y);
                        Event.current.Use();
                        mGui.MarkDirtyRepaint();
                    }
                }

                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    if (mTrackDrag.Active)
                    {
                        if (SkillEditorUtils.ReorderTrack(model, skillObject, mTrackDrag.SourceIndex, mTrackDrag.InsertBeforeIndex))
                            RemapSelectionAfterTrackReorder(mTrackDrag.SourceIndex, mTrackDrag.InsertBeforeIndex);

                        mWindow.RefreshTimeline();
                    }
                    else if (mTrackDrag.Pending && listRect.Contains(Event.current.mousePosition))
                    {
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, mTrackDrag.SourceIndex, -1));
                    }

                    mTrackDrag = default;
                    Event.current.Use();
                    return;
                }

                if (Event.current.type == EventType.Repaint && mTrackDrag.Active)
                    EditorGUIUtility.AddCursorRect(listRect, MouseCursor.MoveArrow);

                return;
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || trackCount == 0)
                return;

            if (!listRect.Contains(Event.current.mousePosition))
                return;

            for (var i = 0; i < trackCount; i++)
            {
                var fullRowRect = new Rect(
                    listRect.x,
                    tracksTop + i * SkillEditorUtils.TrackRowHeight,
                    listRect.width,
                    SkillEditorUtils.TrackRowHeight);

                if (!fullRowRect.Contains(Event.current.mousePosition))
                    continue;

                mTrackDrag.Pending = true;
                mTrackDrag.SourceIndex = i;
                mTrackDrag.InsertBeforeIndex = i;
                mTrackDrag.StartMouse = Event.current.mousePosition;
                Event.current.Use();
                return;
            }
        }

        static int GetTrackInsertBeforeIndex(float tracksTop, int trackCount, float mouseY)
        {
            if (trackCount <= 0)
                return 0;

            var y = mouseY - tracksTop;
            if (y < 0f)
                return 0;

            var row = Mathf.FloorToInt(y / SkillEditorUtils.TrackRowHeight);
            if (row >= trackCount)
                return trackCount;

            var rowY = tracksTop + row * SkillEditorUtils.TrackRowHeight;
            return mouseY >= rowY + SkillEditorUtils.TrackRowHeight * 0.5f ? row + 1 : row;
        }

        void RemapSelectionAfterTrackReorder(int fromIndex, int insertBeforeIndex)
        {
            var targetIndex = insertBeforeIndex;
            if (targetIndex > fromIndex)
                targetIndex--;

            if (mSelection.IsTrack)
            {
                SetSelection(new SkillEditorSelection(
                    SkillEditorSelectionKind.Track,
                    RemapTrackIndex(mSelection.TrackIndex, fromIndex, targetIndex),
                    -1));
            }
            else if (mSelection.IsClip)
            {
                SetSelection(new SkillEditorSelection(
                    SkillEditorSelectionKind.Clip,
                    RemapTrackIndex(mSelection.TrackIndex, fromIndex, targetIndex),
                    mSelection.ClipIndex));
            }
        }

        static int RemapTrackIndex(int index, int fromIndex, int targetIndex)
        {
            if (index == fromIndex)
                return targetIndex;

            if (fromIndex < targetIndex)
            {
                if (index > fromIndex && index <= targetIndex)
                    return index - 1;
            }
            else if (index >= targetIndex && index < fromIndex)
            {
                return index + 1;
            }

            return index;
        }

        void DrawRuler(Rect rulerRect, int frameCount)
        {
            EditorGUI.DrawRect(rulerRect, new Color(0.26f, 0.26f, 0.28f));
            var step = PixelsPerFrame < 8f ? 5 : 1;
            for (var f = 0; f < frameCount; f += step)
            {
                var x = rulerRect.x + SkillEditorUtils.TimelinePaddingLeft + f * PixelsPerFrame;
                var tickRect = new Rect(x, rulerRect.y, 1f, rulerRect.height);
                EditorGUI.DrawRect(tickRect, new Color(0.4f, 0.4f, 0.42f));
                var labelRect = new Rect(x + 2f, rulerRect.y + 2f, 40f, 16f);
                GUI.Label(labelRect, f.ToString(), EditorStyles.miniLabel);
            }
        }

        void DrawTrackRows(SkillModel model, System.Collections.Generic.List<SkillTrackBase> tracks, Rect areaRect, int frameCount, int trackCount)
        {
            for (var t = 0; t < trackCount; t++)
            {
                var rowY = areaRect.y + t * SkillEditorUtils.TrackRowHeight;
                var fullRowRect = new Rect(areaRect.x, rowY, areaRect.width, SkillEditorUtils.TrackRowHeight);
                var rowRect = new Rect(
                    fullRowRect.x,
                    fullRowRect.y,
                    fullRowRect.width,
                    fullRowRect.height - SkillEditorUtils.TrackRowSeparatorHeight);

                var track = tracks[t];
                var isDraggingThis = mTrackDrag.IsBusy && mTrackDrag.SourceIndex == t;
                var isTrackSelected = mSelection.IsTrack && mSelection.TrackIndex == t && !isDraggingThis;
                DrawTrackRowChrome(fullRowRect, t, track, isTrackSelected, isDraggingThis ? 0.45f : 1f);

                DrawFrameGrid(rowRect, frameCount);

                if (track == null || !SkillEditorUtils.IsClipTrack(track))
                    continue;

                var clipCount = SkillEditorUtils.GetClipCount(track);
                for (var c = 0; c < clipCount; c++)
                {
                    if (!SkillEditorUtils.TryGetClipRange(track, c, out var start, out var end, out var label))
                        continue;

                    var clipRect = FrameRangeToRect(rowRect, start, end);
                    var isSelected = mSelection.IsClip && mSelection.TrackIndex == t && mSelection.ClipIndex == c;
                    var isDragging = mClipDrag.Active && mClipDrag.TrackIndex == t && mClipDrag.ClipIndex == c;
                    var clipColor = GetClipColor(track, isDragging);

                    EditorGUI.DrawRect(clipRect, clipColor);
                    if (isSelected || isDragging)
                        DrawOutline(clipRect, new Color(1f, 0.85f, 0.2f), 2f);

                    var style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                    };
                    GUI.Label(clipRect, label, style);

                    if (!mClipDrag.Active && Event.current.type == EventType.Repaint)
                    {
                        if (clipRect.Contains(Event.current.mousePosition))
                        {
                            if (SkillEditorUtils.IsSingleFrameClipTrack(track))
                                EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.MoveArrow);
                            else
                            {
                                var edge = SkillEditorUtils.ClipResizeEdgeWidth;
                                if (Event.current.mousePosition.x <= clipRect.x + edge)
                                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.ResizeHorizontal);
                                else if (Event.current.mousePosition.x >= clipRect.xMax - edge)
                                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.ResizeHorizontal);
                                else
                                    EditorGUIUtility.AddCursorRect(clipRect, MouseCursor.MoveArrow);
                            }
                        }
                    }
                }
            }
        }

        void DrawPlayhead(Rect contentRect, int frameCount)
        {
            if (frameCount <= 0)
                return;

            var timelineLeft = contentRect.x + SkillEditorUtils.TrackListWidth;
            var x = FrameToTimelineX(timelineLeft, mPlayheadFrame);
            var columnRect = new Rect(timelineLeft, contentRect.y, contentRect.xMax - timelineLeft, contentRect.height);
            var lineRect = new Rect(x - 1f, contentRect.y, 2f, contentRect.height);

            EditorGUI.DrawRect(lineRect, new Color(0.95f, 0.35f, 0.3f, 0.95f));

            var headRect = new Rect(x, contentRect.y, 8f, SkillEditorUtils.RulerHeight);
            EditorGUI.DrawRect(headRect, new Color(0.95f, 0.35f, 0.3f, 1f));

            if (Event.current.type == EventType.Repaint && columnRect.Contains(Event.current.mousePosition))
            {
                if (Mathf.Abs(Event.current.mousePosition.x - x) <= SkillEditorUtils.PlayheadHitWidth * 0.5f)
                    EditorGUIUtility.AddCursorRect(columnRect, MouseCursor.SlideArrow);
            }
        }

        void HandlePlayheadEvents(Rect contentRect, int frameCount)
        {
            if (mTrackDrag.IsBusy)
                return;

            if (frameCount <= 0)
                return;

            var timelineLeft = contentRect.x + SkillEditorUtils.TrackListWidth;
            var timelineWidth = frameCount * PixelsPerFrame + SkillEditorUtils.TimelinePaddingLeft;
            var timelineColumn = new Rect(timelineLeft, contentRect.y, timelineWidth, contentRect.height);
            var lastFrame = Mathf.Max(0, frameCount - 1);

            if (mPlayheadDragging)
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    mPlayheadDragging = false;
                    Event.current.Use();
                    return;
                }

                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    mPlayheadFrame = GetFrameFromTimelineMouseX(timelineLeft, lastFrame, Event.current.mousePosition.x);
                    Event.current.Use();
                    mGui.MarkDirtyRepaint();
                    return;
                }
            }

            if (mClipDrag.Active)
                return;

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
                return;

            if (!timelineColumn.Contains(Event.current.mousePosition))
                return;

            var rulerRect = new Rect(timelineLeft, contentRect.y, timelineWidth, SkillEditorUtils.RulerHeight);
            var playheadX = FrameToTimelineX(timelineLeft, mPlayheadFrame);
            var onRuler = rulerRect.Contains(Event.current.mousePosition);
            var onPlayhead = Mathf.Abs(Event.current.mousePosition.x - playheadX) <= SkillEditorUtils.PlayheadHitWidth * 0.5f;

            if (!onRuler && !onPlayhead)
                return;

            mPlayheadDragging = true;
            mPlayheadFrame = GetFrameFromTimelineMouseX(timelineLeft, lastFrame, Event.current.mousePosition.x);
            Event.current.Use();
        }

        void HandleClipDragEvents(SkillModel model, SerializedObject skillObject, Rect contentRect, int frameCount, int trackCount)
        {
            if (mPlayheadDragging || mTrackDrag.IsBusy)
                return;

            var tracksTop = contentRect.y + SkillEditorUtils.RulerHeight;
            var timelineLeft = contentRect.x + SkillEditorUtils.TrackListWidth;
            var timelineWidth = frameCount * PixelsPerFrame + SkillEditorUtils.TimelinePaddingLeft;
            var lastFrame = Mathf.Max(0, frameCount - 1);

            if (mClipDrag.Active)
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    mClipDrag = default;
                    Event.current.Use();
                    return;
                }

                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    var rowRect = GetTrackRowRect(timelineLeft, tracksTop, timelineWidth, mClipDrag.TrackIndex);
                    var currentFrame = GetFrameFromMouseInRow(rowRect, Event.current.mousePosition);
                    var delta = currentFrame - mClipDrag.GrabFrame;
                    ApplyClipDrag(model, skillObject, lastFrame, delta);
                    Event.current.Use();
                    mGui.MarkDirtyRepaint();
                    return;
                }
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
                return;

            var tracks = model.Tracks;
            if (tracks == null)
                return;

            for (var t = trackCount - 1; t >= 0; t--)
            {
                var track = tracks[t];
                if (track == null || !SkillEditorUtils.IsClipTrack(track))
                    continue;

                var rowRect = GetTrackRowRect(timelineLeft, tracksTop, timelineWidth, t);
                var clipCount = SkillEditorUtils.GetClipCount(track);
                for (var c = clipCount - 1; c >= 0; c--)
                {
                    if (!SkillEditorUtils.TryGetClipRange(track, c, out var start, out var end, out _))
                        continue;

                    var clipRect = FrameRangeToRect(rowRect, start, end);
                    if (!clipRect.Contains(Event.current.mousePosition))
                        continue;

                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, t, c));
                    BeginClipDrag(t, c, start, end, clipRect, rowRect);
                    Event.current.Use();
                    return;
                }
            }
        }

        void BeginClipDrag(int trackIndex, int clipIndex, int start, int end, Rect clipRect, Rect rowRect)
        {
            var mode = ClipDragMode.Move;
            if (!IsSingleFrameClipDrag(trackIndex))
            {
                var edge = SkillEditorUtils.ClipResizeEdgeWidth;
                if (Event.current.mousePosition.x <= clipRect.x + edge)
                    mode = ClipDragMode.ResizeStart;
                else if (Event.current.mousePosition.x >= clipRect.xMax - edge)
                    mode = ClipDragMode.ResizeEnd;
            }

            mClipDrag = new ClipDragState
            {
                Mode = mode,
                TrackIndex = trackIndex,
                ClipIndex = clipIndex,
                GrabFrame = GetFrameFromMouseInRow(rowRect, Event.current.mousePosition),
                OrigStart = start,
                OrigEnd = end,
            };
        }

        bool IsSingleFrameClipDrag(int trackIndex)
        {
            var model = mWindow.Skill;
            if (model?.Tracks == null || trackIndex < 0 || trackIndex >= model.Tracks.Count)
                return false;

            return SkillEditorUtils.IsSingleFrameClipTrack(model.Tracks[trackIndex]);
        }

        void ApplyClipDrag(SkillModel model, SerializedObject skillObject, int lastFrame, int delta)
        {
            if (!mClipDrag.Active)
                return;

            var start = mClipDrag.OrigStart;
            var end = mClipDrag.OrigEnd;
            var duration = end - start;
            var singleFrame = IsSingleFrameClipDrag(mClipDrag.TrackIndex);

            switch (mClipDrag.Mode)
            {
                case ClipDragMode.Move:
                {
                    var newStart = singleFrame
                        ? Mathf.Clamp(mClipDrag.OrigStart + delta, 0, lastFrame)
                        : Mathf.Clamp(mClipDrag.OrigStart + delta, 0, Mathf.Max(0, lastFrame - duration));
                    var newEnd = singleFrame ? newStart : newStart + duration;
                    SkillEditorUtils.SetClipFrameRange(model, skillObject, mClipDrag.TrackIndex, mClipDrag.ClipIndex, newStart, newEnd);
                    break;
                }
                case ClipDragMode.ResizeStart:
                {
                    var newStart = Mathf.Clamp(mClipDrag.OrigStart + delta, 0, mClipDrag.OrigEnd);
                    SkillEditorUtils.SetClipFrameRange(model, skillObject, mClipDrag.TrackIndex, mClipDrag.ClipIndex, newStart, mClipDrag.OrigEnd);
                    break;
                }
                case ClipDragMode.ResizeEnd:
                {
                    var newEnd = Mathf.Clamp(mClipDrag.OrigEnd + delta, mClipDrag.OrigStart, lastFrame);
                    SkillEditorUtils.SetClipFrameRange(model, skillObject, mClipDrag.TrackIndex, mClipDrag.ClipIndex, mClipDrag.OrigStart, newEnd);
                    break;
                }
            }
        }

        static Rect GetTrackRowRect(float timelineLeft, float tracksTop, float timelineWidth, int trackIndex)
        {
            var rowY = tracksTop + trackIndex * SkillEditorUtils.TrackRowHeight;
            return new Rect(timelineLeft, rowY, timelineWidth, SkillEditorUtils.TrackRowHeight - 1f);
        }

        int GetFrameFromMouseInRow(Rect rowRect, Vector2 mousePosition)
        {
            var localX = mousePosition.x - rowRect.x - SkillEditorUtils.TimelinePaddingLeft;
            return Mathf.Max(0, Mathf.FloorToInt(localX / PixelsPerFrame));
        }

        void DrawFrameGrid(Rect rowRect, int frameCount)
        {
            for (var f = 0; f < frameCount; f++)
            {
                var x = rowRect.x + SkillEditorUtils.TimelinePaddingLeft + f * PixelsPerFrame;
                EditorGUI.DrawRect(new Rect(x, rowRect.y, 1f, rowRect.height), new Color(0.12f, 0.12f, 0.14f, 0.6f));
            }
        }

        Rect FrameRangeToRect(Rect rowRect, int startFrame, int endFrame)
        {
            var x0 = rowRect.x + SkillEditorUtils.TimelinePaddingLeft + startFrame * PixelsPerFrame + 1f;
            var x1 = rowRect.x + SkillEditorUtils.TimelinePaddingLeft + (endFrame + 1) * PixelsPerFrame - 1f;
            return new Rect(x0, rowRect.y + 2f, Mathf.Max(4f, x1 - x0), rowRect.height - 4f);
        }

        static void DrawTrackRowChrome(Rect fullRowRect, int trackIndex, SkillTrackBase track, bool isSelected, float alpha = 1f)
        {
            var bg = GetTrackRowBackgroundColor(trackIndex, track);
            bg.a *= alpha;
            EditorGUI.DrawRect(fullRowRect, bg);

            var accentRect = new Rect(
                fullRowRect.x,
                fullRowRect.y,
                SkillEditorUtils.TrackRowAccentWidth,
                fullRowRect.height - SkillEditorUtils.TrackRowSeparatorHeight);
            var accent = GetTrackAccentColor(track);
            accent.a *= alpha;
            EditorGUI.DrawRect(accentRect, accent);

            if (isSelected)
            {
                var overlay = new Rect(
                    fullRowRect.x + SkillEditorUtils.TrackRowAccentWidth,
                    fullRowRect.y,
                    fullRowRect.width - SkillEditorUtils.TrackRowAccentWidth,
                    fullRowRect.height - SkillEditorUtils.TrackRowSeparatorHeight);
                EditorGUI.DrawRect(overlay, new Color(0.28f, 0.42f, 0.62f, 0.45f * alpha));
            }

            var separatorY = fullRowRect.yMax - SkillEditorUtils.TrackRowSeparatorHeight;
            EditorGUI.DrawRect(
                new Rect(fullRowRect.x, separatorY, fullRowRect.width, SkillEditorUtils.TrackRowSeparatorHeight),
                new Color(0.08f, 0.08f, 0.1f, 1f));
        }

        static Color GetTrackRowBackgroundColor(int trackIndex, SkillTrackBase track)
        {
            var even = trackIndex % 2 == 0;
            var zebra = even ? new Color(0.21f, 0.21f, 0.235f) : new Color(0.175f, 0.175f, 0.195f);

            return track switch
            {
                SkillAnimationTrack => Color.Lerp(zebra, new Color(0.2f, 0.24f, 0.32f), 0.4f),
                SkillAudioTrack => Color.Lerp(zebra, new Color(0.18f, 0.28f, 0.2f), 0.4f),
                SkillVfxTrack => Color.Lerp(zebra, new Color(0.3f, 0.22f, 0.16f), 0.4f),
                SkillMovementTrack => Color.Lerp(zebra, new Color(0.26f, 0.2f, 0.34f), 0.4f),
                SkillHitboxTrack => Color.Lerp(zebra, new Color(0.34f, 0.16f, 0.16f), 0.4f),
                SkillInterruptTrack => Color.Lerp(zebra, new Color(0.34f, 0.3f, 0.14f), 0.4f),
                SkillEventTrack => Color.Lerp(zebra, new Color(0.16f, 0.28f, 0.34f), 0.4f),
                _ => zebra,
            };
        }

        static Color GetTrackAccentColor(SkillTrackBase track)
        {
            return track switch
            {
                SkillAnimationTrack => new Color(0.35f, 0.55f, 0.85f),
                SkillAudioTrack => new Color(0.4f, 0.75f, 0.45f),
                SkillVfxTrack => new Color(0.85f, 0.5f, 0.28f),
                SkillMovementTrack => new Color(0.62f, 0.45f, 0.9f),
                SkillHitboxTrack => new Color(0.92f, 0.32f, 0.32f),
                SkillInterruptTrack => new Color(0.95f, 0.78f, 0.22f),
                SkillEventTrack => new Color(0.28f, 0.82f, 0.95f),
                _ => new Color(0.45f, 0.45f, 0.48f),
            };
        }

        static Color GetClipColor(SkillTrackBase track, bool isDragging)
        {
            var alpha = isDragging ? 1f : 0.9f;
            return track switch
            {
                SkillAnimationTrack => new Color(0.25f, 0.45f, 0.75f, alpha),
                SkillAudioTrack => new Color(0.3f, 0.65f, 0.4f, alpha),
                SkillVfxTrack => new Color(0.78f, 0.42f, 0.22f, alpha),
                SkillMovementTrack => new Color(0.55f, 0.38f, 0.82f, alpha),
                SkillHitboxTrack => new Color(0.85f, 0.28f, 0.28f, alpha),
                SkillInterruptTrack => new Color(0.88f, 0.72f, 0.18f, alpha),
                SkillEventTrack => new Color(0.2f, 0.62f, 0.78f, alpha),
                _ => new Color(0.5f, 0.5f, 0.5f, alpha),
            };
        }

        static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        static int GetTrackIndexFromMouseY(float tracksTop, int trackCount, float mouseY)
        {
            var y = mouseY - tracksTop;
            if (y < 0f)
                return -1;
            var index = Mathf.FloorToInt(y / SkillEditorUtils.TrackRowHeight);
            return index >= 0 && index < trackCount ? index : -1;
        }

        static int GetTrackIndexAtY(float tracksTop, int trackCount, Vector2 localPos)
        {
            return GetTrackIndexFromMouseY(tracksTop, trackCount, localPos.y);
        }

        float FrameToTimelineX(float timelineLeft, int frame)
        {
            return timelineLeft + SkillEditorUtils.TimelinePaddingLeft + frame * PixelsPerFrame;
        }

        int GetFrameFromTimelineMouseX(float timelineLeft, int lastFrame, float mouseX)
        {
            var localX = mouseX - timelineLeft - SkillEditorUtils.TimelinePaddingLeft;
            var frame = Mathf.FloorToInt(localX / PixelsPerFrame);
            return Mathf.Clamp(frame, 0, lastFrame);
        }

        static int GetFrameAtPosition(float timelineLocalX, float pixelsPerFrame)
        {
            var frame = Mathf.FloorToInt((timelineLocalX - SkillEditorUtils.TimelinePaddingLeft) / pixelsPerFrame);
            return Mathf.Max(0, frame);
        }

        void ShowTrackListContextMenu(SkillModel model, SerializedObject skillObject, int trackIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("添加动画轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillAnimationTrack { DisplayName = "动画" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加音频轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillAudioTrack { DisplayName = "音频" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加特效轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillVfxTrack { DisplayName = "特效" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加位移轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillMovementTrack { DisplayName = "位移" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加碰撞体轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillHitboxTrack { DisplayName = "碰撞体" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加打断轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillInterruptTrack { DisplayName = "打断" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });
            menu.AddItem(new GUIContent("添加事件轨道"), false, () =>
            {
                var index = SkillEditorUtils.AddTrack(model, skillObject, new SkillEventTrack { DisplayName = "事件" });
                if (index >= 0)
                    SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Track, index, -1));
                mWindow.RefreshTimeline();
            });

            var trackCount = model.Tracks?.Count ?? 0;
            if (trackCount > 1)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("自动排序"), false, () =>
                {
                    SkillTrackBase selectedTrack = null;
                    if (mSelection.IsTrack || mSelection.IsClip)
                    {
                        var selectedIndex = mSelection.TrackIndex;
                        if (selectedIndex >= 0 && selectedIndex < trackCount)
                            selectedTrack = model.Tracks[selectedIndex];
                    }

                    if (SkillEditorUtils.SortTracksByKind(model, skillObject))
                        RemapSelectionAfterTrackSort(model, selectedTrack);

                    mWindow.RefreshTimeline();
                });
            }

            if (trackIndex >= 0 && trackIndex < trackCount)
                AddDeleteTrackMenuItem(menu, model, skillObject, trackIndex);

            menu.ShowAsContext();
        }

        void RemapSelectionAfterTrackSort(SkillModel model, SkillTrackBase selectedTrack)
        {
            if (!mSelection.IsTrack && !mSelection.IsClip)
                return;

            if (selectedTrack == null || model.Tracks == null)
            {
                SetSelection(SkillEditorSelection.None);
                return;
            }

            for (var i = 0; i < model.Tracks.Count; i++)
            {
                if (!ReferenceEquals(model.Tracks[i], selectedTrack))
                    continue;

                SetSelection(new SkillEditorSelection(
                    mSelection.Kind,
                    i,
                    mSelection.IsClip ? mSelection.ClipIndex : -1));
                return;
            }

            SetSelection(SkillEditorSelection.None);
        }

        void AddDeleteTrackMenuItem(GenericMenu menu, SkillModel model, SerializedObject skillObject, int trackIndex)
        {
            var track = model.Tracks[trackIndex];
            var trackName = track != null ? track.DisplayName : $"轨道{trackIndex}";
            menu.AddSeparator("");
            menu.AddItem(new GUIContent($"删除轨道「{trackName}」"), false, () =>
            {
                SkillEditorUtils.RemoveTrack(model, skillObject, trackIndex);
                SetSelection(SkillEditorSelection.None);
                mWindow.RefreshTimeline();
            });
        }

        void ShowTimelineContextMenu(SkillModel model, SerializedObject skillObject, Vector2 localPos, int frameCount, int trackCount)
        {
            var tracksTop = SkillEditorUtils.RulerHeight;
            var trackIndex = GetTrackIndexAtY(tracksTop, trackCount, localPos);
            if (trackIndex < 0 || trackIndex >= trackCount)
            {
                if (localPos.x < SkillEditorUtils.TrackListWidth)
                    ShowTrackListContextMenu(model, skillObject, -1);
                return;
            }

            var track = model.Tracks[trackIndex];
            var frame = GetFrameAtPosition(localPos.x - SkillEditorUtils.TrackListWidth, PixelsPerFrame);
            frame = Mathf.Clamp(frame, 0, frameCount - 1);

            var menu = new GenericMenu();
            AddDeleteTrackMenuItem(menu, model, skillObject, trackIndex);

            if (!SkillEditorUtils.IsClipTrack(track))
            {
                menu.ShowAsContext();
                return;
            }

            menu.AddSeparator("");
            if (track is SkillAnimationTrack)
            {
                menu.AddItem(new GUIContent("添加动画片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillAudioTrack)
            {
                menu.AddItem(new GUIContent("添加音频片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillVfxTrack)
            {
                menu.AddItem(new GUIContent("添加特效片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillMovementTrack)
            {
                menu.AddItem(new GUIContent("添加位移片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillHitboxTrack)
            {
                menu.AddItem(new GUIContent("添加碰撞体片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillInterruptTrack)
            {
                menu.AddItem(new GUIContent("添加打断片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }
            else if (track is SkillEventTrack)
            {
                menu.AddItem(new GUIContent("添加事件片段"), false, () =>
                {
                    var clipIndex = SkillEditorUtils.AddClip(model, skillObject, trackIndex, frame);
                    if (clipIndex >= 0)
                        SetSelection(new SkillEditorSelection(SkillEditorSelectionKind.Clip, trackIndex, clipIndex));
                    mWindow.RefreshTimeline();
                });
            }

            menu.ShowAsContext();
        }
    }
}
