using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 技能轨道上的一段帧区间：在 <see cref="StartFrame"/> 触发开始，在 <see cref="EndFrame"/> 触发结束。
/// </summary>
[Serializable]
public abstract class SkillFrameClipSegmentBase
{
    [HorizontalGroup("区间", Width = 0.5f)]
    [LabelText("开始帧")]
    [MinValue(0)]
    public int StartFrame;

    [HorizontalGroup("区间", Width = 0.5f)]
    [LabelText("结束帧")]
    [MinValue(0)]
    public int EndFrame;

    public virtual void ClampToSkillFrames(int frameCount)
    {
        StartFrame = ClampFrame(StartFrame, frameCount);
        EndFrame = ClampFrame(EndFrame, frameCount);
        if (EndFrame < StartFrame)
            EndFrame = StartFrame;
    }

    public bool IsValid() => EndFrame >= StartFrame;

    protected static int ClampFrame(int frame, int frameCount)
    {
        var max = Mathf.Max(1, frameCount) - 1;
        return Mathf.Clamp(frame, 0, max);
    }
}
