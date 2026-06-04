/// <summary>
/// 技能时间轴片段在编辑器中的显示名称。
/// </summary>
public static class SkillSegmentLabelUtil
{
    public static string GetAnimationLabel(SkillAnimationClipSegment segment) =>
        segment?.Clip != null ? segment.Clip.name : "动画";

    public static string GetAudioLabel(SkillAudioClipSegment segment) =>
        segment?.Clip != null ? segment.Clip.name : "音频";

    public static string GetVfxLabel(SkillVfxClipSegment segment) =>
        segment?.Prefab != null ? segment.Prefab.name : "特效";

    public static string GetMovementLabel(SkillMovementClipSegment segment) => "位移";

    public static string GetHitboxLabel(SkillHitboxClipSegment segment) =>
        segment != null ? segment.ColliderType.GetDisplayName() : "碰撞";

    public static string GetInterruptLabel(SkillInterruptClipSegment segment)
    {
        if (segment == null)
            return "打断";

        return string.IsNullOrEmpty(segment.Note) ? "打断" : segment.Note;
    }

    public static string GetEventLabel(SkillEventClipSegment segment) =>
        segment != null ? segment.Signal.GetDisplayName() : "事件";
}
