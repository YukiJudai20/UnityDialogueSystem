using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 时间轴上的一个标记点，供技能编辑器绘制刻度与预览。
/// </summary>
[Serializable]
public struct SkillTimelineMarker
{
    public int Frame;
    public string Label;

    public SkillTimelineMarker(int frame, string label)
    {
        Frame = frame;
        Label = label ?? string.Empty;
    }
}

/// <summary>
/// 技能轨道基类：描述一条按帧推进的轨道数据，并在运行时由技能实例驱动回调。
/// </summary>
[Serializable]
public abstract class SkillTrackBase
{
    [HideInInspector]
    public string TrackId = string.Empty;

    [HorizontalGroup("轨道", Width = 0.55f)]
    [LabelText("名称")]
    public string DisplayName = "轨道";

    [HorizontalGroup("轨道", Width = 0.2f)]
    [LabelText("启用")]
    public bool Enabled = true;

    [HideInInspector]
    public int EditorOrder;

    public abstract SkillTrackKind Kind { get; }

    public virtual void OnSkillBegin(SkillPlayContext context) { }

    public virtual void OnFrame(SkillPlayContext context) { }

    public virtual void OnSkillEnd(SkillPlayContext context) { }

    public virtual void CollectTimelineMarkers(List<SkillTimelineMarker> markers)
    {
        if (markers == null)
            return;
        AppendTimelineMarkers(markers);
    }

    protected virtual void AppendTimelineMarkers(List<SkillTimelineMarker> markers) { }

    public virtual void ClampToSkillFrames(int frameCount) { }

    protected static int ClampFrame(int frame, int frameCount)
    {
        var max = Mathf.Max(1, frameCount) - 1;
        return Mathf.Clamp(frame, 0, max);
    }

    public void EnsureTrackId()
    {
        if (string.IsNullOrEmpty(TrackId))
            TrackId = Guid.NewGuid().ToString("N");
    }
}
