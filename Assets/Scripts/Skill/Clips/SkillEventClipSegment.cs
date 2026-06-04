using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 事件片段：仅占一帧，在该帧发出所选 <see cref="SkillEventSignal"/>。
/// </summary>
[Serializable]
public sealed class SkillEventClipSegment
{
    [LabelText("帧")]
    [MinValue(0)]
    public int Frame;

    [LabelText("信号")]
    public SkillEventSignal Signal = SkillEventSignal.None;

    public void ClampToSkillFrames(int frameCount)
    {
        var max = Mathf.Max(1, frameCount) - 1;
        Frame = Mathf.Clamp(Frame, 0, max);
    }
}
