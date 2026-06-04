using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 动画技能轨道：按帧驱动 Animator 播放与停止。
/// </summary>
[System.Serializable]
public sealed class SkillAnimationTrack : SkillClipTrackBase<SkillAnimationClipSegment>
{
    sealed class AnimationTrackRuntime
    {
        public readonly HashSet<int> ActiveSegmentIndices = new HashSet<int>();
    }

    public override SkillTrackKind Kind => SkillTrackKind.Animation;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("动画片段")]
    public List<SkillAnimationClipSegment> Clips = new List<SkillAnimationClipSegment>();

    protected override IList<SkillAnimationClipSegment> ClipList => Clips;

    protected override string GetSegmentMarkerLabel(SkillAnimationClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetAnimationLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillAnimationClipSegment segment, int index)
    {
        if (segment.Clip == null || context.Caster == null)
            return;

        var animator = context.Caster.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[SkillAnimationTrack] 释放者 {context.Caster.name} 上未找到 Animator。", context.Caster);
            return;
        }

        var stateName = string.IsNullOrEmpty(segment.StateName) ? segment.Clip.name : segment.StateName;
        var layer = segment.Layer;

        animator.SetLayerWeight(layer, 1f);
        if (segment.CrossFadeDuration > 0f)
            animator.CrossFade(stateName, segment.CrossFadeDuration, layer, 0f);
        else
            animator.Play(stateName, layer, 0f);

        context.GetTrackState<AnimationTrackRuntime>(TrackId).ActiveSegmentIndices.Add(index);
    }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillAnimationClipSegment segment, int index)
    {
        StopSegment(context, segment, index);
    }

    protected override void StopAllSegments(SkillPlayContext context)
    {
        if (context == null || Clips == null)
            return;

        var state = context.GetTrackState<AnimationTrackRuntime>(TrackId);
        var indices = new List<int>(state.ActiveSegmentIndices);
        for (var i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            if (index < 0 || index >= Clips.Count)
                continue;
            var seg = Clips[index];
            if (seg != null)
                StopSegment(context, seg, index);
        }

        state.ActiveSegmentIndices.Clear();
    }

    void StopSegment(SkillPlayContext context, SkillAnimationClipSegment segment, int index)
    {
        if (context?.Caster == null)
            return;

        var animator = context.Caster.GetComponentInChildren<Animator>();
        if (animator != null)
            animator.SetLayerWeight(segment.Layer, 0f);

        context.GetTrackState<AnimationTrackRuntime>(TrackId).ActiveSegmentIndices.Remove(index);
    }
}
