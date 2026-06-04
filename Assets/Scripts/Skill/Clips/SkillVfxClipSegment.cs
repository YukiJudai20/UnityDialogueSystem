using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 特效轨道上的一段：在开始帧生成特效，在结束帧销毁。
/// </summary>
[Serializable]
public sealed class SkillVfxClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("特效预制体")]
    [Required]
    public GameObject Prefab;

    [LabelText("位置偏移")]
    [Tooltip("相对释放者 Transform 的局部坐标，用于控制特效生成位置。")]
    public Vector3 Offset;

    [LabelText("局部旋转")]
    [Tooltip("相对释放者的欧拉角旋转。")]
    public Vector3 LocalEulerAngles;
}
