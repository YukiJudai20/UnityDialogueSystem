using System;

/// <summary>
/// 技能事件信号的全局派发入口；在 <see cref="SkillEventTrack"/> 到达对应帧时触发。
/// </summary>
public static class SkillEvents
{
    /// <summary>技能事件信号触发时调用。</summary>
    public static event Action<SkillEventContext> Raised;

    internal static void Raise(SkillEventContext context) => Raised?.Invoke(context);
}
