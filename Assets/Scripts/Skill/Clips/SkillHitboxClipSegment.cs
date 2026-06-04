using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 攻击碰撞体片段：持续帧内每逻辑帧执行一次物理重叠检测。
/// </summary>
[Serializable]
public sealed class SkillHitboxClipSegment : SkillFrameClipSegmentBase
{
    [LabelText("碰撞体类型")]
    public SkillHitboxColliderType ColliderType = SkillHitboxColliderType.Box;

    [LabelText("长 (Z)")]
    [MinValue(0.01f)]
    [ShowIf(nameof(IsBoxCollider))]
    public float Length = 1f;

    [LabelText("宽 (X)")]
    [MinValue(0.01f)]
    [ShowIf(nameof(IsBoxCollider))]
    public float Width = 1f;

    [LabelText("高 (Y)")]
    [MinValue(0.01f)]
    [ShowIf(nameof(IsBoxCollider))]
    public float Height = 1f;

    [LabelText("局部中心偏移")]
    [Tooltip("碰撞体中心相对释放者 Transform 的局部坐标。")]
    public Vector3 LocalOffset;

    [LabelText("局部旋转")]
    [ShowIf(nameof(IsBoxCollider))]
    public Vector3 LocalEulerAngles;

    [LabelText("检测层级")]
    public LayerMask HitLayers = ~0;

    public bool IsBoxCollider => ColliderType == SkillHitboxColliderType.Box;

    public Vector3 GetBoxHalfExtents() => new Vector3(Width, Height, Length) * 0.5f;
}
