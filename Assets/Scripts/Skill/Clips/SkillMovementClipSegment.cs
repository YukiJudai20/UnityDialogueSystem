using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 位移片段：起始帧开始位移，结束帧到达目标；在帧区间内按逻辑帧线性插值。
/// </summary>
[Serializable]
public sealed class SkillMovementClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("局部位移")]
    [Tooltip("相对位移开始时刻角色朝向的偏移量；结束帧时到达「起点 + 该偏移」。")]
    public Vector3 LocalOffset;

    [LabelText("仅水平面")]
    [Tooltip("勾选时忽略 Y 轴位移（适用于横版/地面移动）。")]
    public bool PlanarOnly;
}
