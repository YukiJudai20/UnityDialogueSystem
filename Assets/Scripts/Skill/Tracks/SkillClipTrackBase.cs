using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 由多段「开始帧 / 结束帧」片段组成的技能轨道基类。
/// </summary>
[Serializable]
public abstract class SkillClipTrackBase<TSegment> : SkillTrackBase
    where TSegment : SkillFrameClipSegmentBase
{
    protected abstract IList<TSegment> ClipList { get; }

    public override void ClampToSkillFrames(int frameCount)
    {
        var list = ClipList;
        if (list == null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            var seg = list[i];
            if (seg != null)
                seg.ClampToSkillFrames(frameCount);
        }
    }

    public override void OnFrame(SkillPlayContext context)
    {
        if (!Enabled || context == null || !context.IsPlaying)
            return;

        var frame = context.CurrentFrame;
        var list = ClipList;
        if (list == null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            var seg = list[i];
            if (seg == null || !seg.IsValid())
                continue;

            if (frame == seg.StartFrame)
                OnSegmentStart(context, seg, i);

            if (frame == seg.EndFrame)
                OnSegmentEnd(context, seg, i);
        }
    }

    public override void OnSkillEnd(SkillPlayContext context)
    {
        StopAllSegments(context);
        context?.ReleaseTrackState(TrackId);
    }

    protected override void AppendTimelineMarkers(List<SkillTimelineMarker> markers)
    {
        var list = ClipList;
        if (list == null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            var seg = list[i];
            if (seg == null || !seg.IsValid())
                continue;

            var label = GetSegmentMarkerLabel(seg, i);
            markers.Add(new SkillTimelineMarker(seg.StartFrame, $"▶ {label}"));
            markers.Add(new SkillTimelineMarker(seg.EndFrame, $"■ {label}"));
        }
    }

    protected virtual string GetSegmentMarkerLabel(TSegment segment, int index) => $"#{index}";

    protected abstract void OnSegmentStart(SkillPlayContext context, TSegment segment, int index);
    protected abstract void OnSegmentEnd(SkillPlayContext context, TSegment segment, int index);
    protected abstract void StopAllSegments(SkillPlayContext context);
}
