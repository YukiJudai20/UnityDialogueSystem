using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 动画轨道上的一段：在开始帧播放 Animator 状态，在结束帧停止该层动画。
/// </summary>
[Serializable]
public sealed class SkillAnimationClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("动画片段")]
    [Required]
    public AnimationClip Clip;

    [LabelText("状态名")]
    [Tooltip("Animator Controller 中的状态名；留空则使用动画资源名。")]
    public string StateName = string.Empty;

    [LabelText("Animator 层")]
    [MinValue(0)]
    public int Layer;

    [LabelText("过渡时间")]
    [MinValue(0f)]
    [Tooltip("大于 0 时使用 CrossFade，否则直接 Play。")]
    public float CrossFadeDuration;
}
