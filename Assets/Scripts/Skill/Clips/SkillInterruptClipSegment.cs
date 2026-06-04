using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 技能打断片段：在覆盖的帧区间内，当前技能可被外部操作强制中断。
/// </summary>
[Serializable]
public sealed class SkillInterruptClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("备注")]
    [Tooltip("仅用于编辑器标识，不影响运行时逻辑。")]
    public string Note = string.Empty;
}
