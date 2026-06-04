using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 事件技能轨道：在片段所在帧发出 <see cref="SkillEventSignal"/>，由 <see cref="SkillEvents"/> 派发。
/// </summary>
[System.Serializable]
public sealed class SkillEventTrack : SkillTrackBase
{
    public override SkillTrackKind Kind => SkillTrackKind.Event;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("事件片段")]
    public List<SkillEventClipSegment> Clips = new List<SkillEventClipSegment>();

    public override void OnFrame(SkillPlayContext context)
    {
        if (!Enabled || context == null || !context.IsPlaying || Clips == null)
            return;

        var frame = context.CurrentFrame;
        for (var i = 0; i < Clips.Count; i++)
        {
            var segment = Clips[i];
            if (segment == null || segment.Signal == SkillEventSignal.None)
                continue;

            if (frame != segment.Frame)
                continue;

            SkillEvents.Raise(new SkillEventContext(
                context.Model,
                context.Caster,
                context,
                this,
                segment.Signal,
                frame,
                i));
        }
    }

    public override void ClampToSkillFrames(int frameCount)
    {
        if (Clips == null)
            return;

        for (var i = 0; i < Clips.Count; i++)
        {
            var segment = Clips[i];
            if (segment != null)
                segment.ClampToSkillFrames(frameCount);
        }
    }

    protected override void AppendTimelineMarkers(List<SkillTimelineMarker> markers)
    {
        if (Clips == null)
            return;

        for (var i = 0; i < Clips.Count; i++)
        {
            var segment = Clips[i];
            if (segment == null || segment.Signal == SkillEventSignal.None)
                continue;

            var label = SkillSegmentLabelUtil.GetEventLabel(segment);
            markers.Add(new SkillTimelineMarker(segment.Frame, $"⚡ {label}"));
        }
    }
}
