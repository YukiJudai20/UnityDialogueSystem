using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 音频轨道上的一段：在开始帧播放，在结束帧停止。
/// </summary>
[Serializable]
public sealed class SkillAudioClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("音频")]
    [Required]
    public AudioClip Clip;

    [LabelText("音量")]
    [Range(0f, 1f)]
    public float Volume = 1f;

    [LabelText("循环")]
    public bool Loop;
}
