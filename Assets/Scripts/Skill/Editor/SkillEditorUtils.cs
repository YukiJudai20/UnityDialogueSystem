using System;
using UnityEditor;
using UnityEngine;

namespace MyGame.Skill.Editor
{
    internal static class SkillEditorUtils
    {
        public const float TrackListWidth = 200f;
        public const float ClipResizeEdgeWidth = 6f;
        public const float MinPixelsPerFrame = 4f;
        public const float MaxPixelsPerFrame = 48f;
        public const float DefaultPixelsPerFrame = MaxPixelsPerFrame;
        public const float RulerHeight = 22f;
        public const float TrackRowHeight = 44f;
        public const float TrackRowSeparatorHeight = 1f;
        public const float TrackRowAccentWidth = 3f;
        public const float TimelinePaddingLeft = 4f;
        public const float PlayheadHitWidth = 10f;

        public static bool IsClipTrack(SkillTrackBase track) =>
            track is SkillAnimationTrack or SkillAudioTrack or SkillVfxTrack or SkillMovementTrack
                or SkillHitboxTrack or SkillInterruptTrack or SkillEventTrack;

        public static bool IsSingleFrameClipTrack(SkillTrackBase track) => track is SkillEventTrack;

        public static SerializedProperty GetTracksProperty(SerializedObject skillObject) =>
            skillObject?.FindProperty("Tracks");

        public static SerializedProperty GetTrackProperty(SerializedObject skillObject, int trackIndex)
        {
            var tracks = GetTracksProperty(skillObject);
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.arraySize)
                return null;
            return tracks.GetArrayElementAtIndex(trackIndex);
        }

        public static SerializedProperty GetClipsProperty(SerializedProperty trackProperty) =>
            trackProperty?.FindPropertyRelative("Clips");

        public static SerializedProperty GetClipProperty(SerializedObject skillObject, int trackIndex, int clipIndex)
        {
            var clips = GetClipsProperty(GetTrackProperty(skillObject, trackIndex));
            if (clips == null || clipIndex < 0 || clipIndex >= clips.arraySize)
                return null;
            return clips.GetArrayElementAtIndex(clipIndex);
        }

        public static int GetClipCount(SkillTrackBase track)
        {
            return track switch
            {
                SkillAnimationTrack animationTrack => animationTrack.Clips?.Count ?? 0,
                SkillAudioTrack audioTrack => audioTrack.Clips?.Count ?? 0,
                SkillVfxTrack vfxTrack => vfxTrack.Clips?.Count ?? 0,
                SkillMovementTrack movementTrack => movementTrack.Clips?.Count ?? 0,
                SkillHitboxTrack hitboxTrack => hitboxTrack.Clips?.Count ?? 0,
                SkillInterruptTrack interruptTrack => interruptTrack.Clips?.Count ?? 0,
                SkillEventTrack eventTrack => eventTrack.Clips?.Count ?? 0,
                _ => 0,
            };
        }

        public static bool TryGetClipRange(
            SkillTrackBase track,
            int clipIndex,
            out int startFrame,
            out int endFrame,
            out string label)
        {
            startFrame = 0;
            endFrame = 0;
            label = "片段";

            switch (track)
            {
                case SkillAnimationTrack animationTrack:
                {
                    if (animationTrack.Clips == null || clipIndex < 0 || clipIndex >= animationTrack.Clips.Count)
                        return false;
                    var clip = animationTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetAnimationLabel(clip);
                    return true;
                }
                case SkillAudioTrack audioTrack:
                {
                    if (audioTrack.Clips == null || clipIndex < 0 || clipIndex >= audioTrack.Clips.Count)
                        return false;
                    var clip = audioTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetAudioLabel(clip);
                    return true;
                }
                case SkillVfxTrack vfxTrack:
                {
                    if (vfxTrack.Clips == null || clipIndex < 0 || clipIndex >= vfxTrack.Clips.Count)
                        return false;
                    var clip = vfxTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetVfxLabel(clip);
                    return true;
                }
                case SkillMovementTrack movementTrack:
                {
                    if (movementTrack.Clips == null || clipIndex < 0 || clipIndex >= movementTrack.Clips.Count)
                        return false;
                    var clip = movementTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetMovementLabel(clip);
                    return true;
                }
                case SkillHitboxTrack hitboxTrack:
                {
                    if (hitboxTrack.Clips == null || clipIndex < 0 || clipIndex >= hitboxTrack.Clips.Count)
                        return false;
                    var clip = hitboxTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetHitboxLabel(clip);
                    return true;
                }
                case SkillInterruptTrack interruptTrack:
                {
                    if (interruptTrack.Clips == null || clipIndex < 0 || clipIndex >= interruptTrack.Clips.Count)
                        return false;
                    var clip = interruptTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.StartFrame;
                    endFrame = clip.EndFrame;
                    label = SkillSegmentLabelUtil.GetInterruptLabel(clip);
                    return true;
                }
                case SkillEventTrack eventTrack:
                {
                    if (eventTrack.Clips == null || clipIndex < 0 || clipIndex >= eventTrack.Clips.Count)
                        return false;
                    var clip = eventTrack.Clips[clipIndex];
                    if (clip == null)
                        return false;
                    startFrame = clip.Frame;
                    endFrame = clip.Frame;
                    label = SkillSegmentLabelUtil.GetEventLabel(clip);
                    return true;
                }
                default:
                    return false;
            }
        }

        public static int AddTrack(SkillModel model, SerializedObject skillObject, SkillTrackBase track)
        {
            if (model == null || skillObject == null || track == null)
                return -1;

            model.Tracks ??= new System.Collections.Generic.List<SkillTrackBase>();
            track.EnsureTrackId();
            track.EditorOrder = model.Tracks.Count;

            var tracksProp = GetTracksProperty(skillObject);
            var index = tracksProp.arraySize;
            tracksProp.InsertArrayElementAtIndex(index);
            tracksProp.GetArrayElementAtIndex(index).managedReferenceValue = track;

            skillObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(model);
            return index;
        }

        public static int AddClip(SkillModel model, SerializedObject skillObject, int trackIndex, int startFrame)
        {
            var trackProp = GetTrackProperty(skillObject, trackIndex);
            var clipsProp = GetClipsProperty(trackProp);
            if (model == null || skillObject == null || clipsProp == null)
                return -1;

            var frameCount = model.GetClampedFrameCount();
            var lastFrame = Mathf.Max(0, frameCount - 1);
            startFrame = Mathf.Clamp(startFrame, 0, lastFrame);

            var index = clipsProp.arraySize;
            clipsProp.InsertArrayElementAtIndex(index);
            var clipProp = clipsProp.GetArrayElementAtIndex(index);

            if (model.Tracks[trackIndex] is SkillEventTrack)
                clipProp.FindPropertyRelative("Frame").intValue = startFrame;
            else
            {
                var endFrame = Mathf.Min(startFrame + 5, lastFrame);
                clipProp.FindPropertyRelative("StartFrame").intValue = startFrame;
                clipProp.FindPropertyRelative("EndFrame").intValue = endFrame;
            }

            skillObject.ApplyModifiedProperties();
            model.Tracks[trackIndex]?.ClampToSkillFrames(frameCount);
            EditorUtility.SetDirty(model);
            return index;
        }

        public static void SetClipFrameRange(
            SkillModel model,
            SerializedObject skillObject,
            int trackIndex,
            int clipIndex,
            int startFrame,
            int endFrame)
        {
            var clipProp = GetClipProperty(skillObject, trackIndex, clipIndex);
            if (model == null || skillObject == null || clipProp == null)
                return;

            var lastFrame = Mathf.Max(0, model.GetClampedFrameCount() - 1);
            startFrame = Mathf.Clamp(startFrame, 0, lastFrame);
            endFrame = Mathf.Clamp(endFrame, startFrame, lastFrame);

            if (model.Tracks[trackIndex] is SkillEventTrack)
            {
                clipProp.FindPropertyRelative("Frame").intValue = startFrame;
            }
            else
            {
                clipProp.FindPropertyRelative("StartFrame").intValue = startFrame;
                clipProp.FindPropertyRelative("EndFrame").intValue = endFrame;
            }
            skillObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(model);
        }

        public static void RemoveClip(SkillModel model, SerializedObject skillObject, int trackIndex, int clipIndex)
        {
            var clipsProp = GetClipsProperty(GetTrackProperty(skillObject, trackIndex));
            if (clipsProp == null || clipIndex < 0 || clipIndex >= clipsProp.arraySize)
                return;

            clipsProp.DeleteArrayElementAtIndex(clipIndex);
            skillObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(model);
        }

        /// <summary>
        /// 将轨道从 <paramref name="fromIndex"/> 移动到 <paramref name="insertBeforeIndex"/> 之前。
        /// </summary>
        public static bool ReorderTrack(
            SkillModel model,
            SerializedObject skillObject,
            int fromIndex,
            int insertBeforeIndex)
        {
            if (model?.Tracks == null || skillObject == null)
                return false;

            var count = model.Tracks.Count;
            if (count <= 1 || fromIndex < 0 || fromIndex >= count)
                return false;

            insertBeforeIndex = Mathf.Clamp(insertBeforeIndex, 0, count);
            var targetIndex = insertBeforeIndex;
            if (targetIndex > fromIndex)
                targetIndex--;

            if (targetIndex == fromIndex)
                return false;

            var tracksProp = GetTracksProperty(skillObject);
            if (tracksProp == null || tracksProp.arraySize != count)
                return false;

            tracksProp.MoveArrayElement(fromIndex, targetIndex);
            skillObject.ApplyModifiedProperties();

            SyncTrackEditorOrder(model);
            EditorUtility.SetDirty(model);
            return true;
        }

        /// <summary>
        /// 按 <see cref="SkillTrackKind"/> 枚举顺序排列所有轨道；同类型保持原有相对顺序。
        /// </summary>
        public static bool SortTracksByKind(SkillModel model, SerializedObject skillObject)
        {
            if (model?.Tracks == null || skillObject == null)
                return false;

            var count = model.Tracks.Count;
            if (count <= 1)
                return false;

            var indices = new int[count];
            for (var i = 0; i < count; i++)
                indices[i] = i;

            Array.Sort(indices, (a, b) =>
            {
                var trackA = model.Tracks[a];
                var trackB = model.Tracks[b];
                var kindA = trackA != null ? (int)trackA.Kind : 0;
                var kindB = trackB != null ? (int)trackB.Kind : 0;
                var cmp = kindA.CompareTo(kindB);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            var changed = false;
            for (var i = 0; i < count; i++)
            {
                if (indices[i] == i)
                    continue;
                changed = true;
                break;
            }

            if (!changed)
                return false;

            var sorted = new SkillTrackBase[count];
            for (var i = 0; i < count; i++)
                sorted[i] = model.Tracks[indices[i]];

            model.Tracks.Clear();
            for (var i = 0; i < count; i++)
                model.Tracks.Add(sorted[i]);

            var tracksProp = GetTracksProperty(skillObject);
            if (tracksProp == null || tracksProp.arraySize != count)
                return false;

            for (var i = 0; i < count; i++)
                tracksProp.GetArrayElementAtIndex(i).managedReferenceValue = sorted[i];

            skillObject.ApplyModifiedProperties();
            SyncTrackEditorOrder(model);
            EditorUtility.SetDirty(model);
            return true;
        }

        public static void SyncTrackEditorOrder(SkillModel model)
        {
            if (model?.Tracks == null)
                return;

            for (var i = 0; i < model.Tracks.Count; i++)
            {
                if (model.Tracks[i] != null)
                    model.Tracks[i].EditorOrder = i;
            }
        }

        public static void RemoveTrack(SkillModel model, SerializedObject skillObject, int trackIndex)
        {
            var tracksProp = GetTracksProperty(skillObject);
            if (tracksProp == null || trackIndex < 0 || trackIndex >= tracksProp.arraySize)
                return;

            tracksProp.DeleteArrayElementAtIndex(trackIndex);
            skillObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(model);
        }
    }
}
