/// <summary>
/// 技能攻击碰撞体形状类型。
/// </summary>
public enum SkillHitboxColliderType
{
    /// <summary>轴对齐立方体（局部空间可调长宽高）。</summary>
    Box = 0,
}

public static class SkillHitboxColliderTypeExtensions
{
    public static string GetDisplayName(this SkillHitboxColliderType type) =>
        type switch
        {
            SkillHitboxColliderType.Box => "立方体",
            _ => "未知",
        };
}
