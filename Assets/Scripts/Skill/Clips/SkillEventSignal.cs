/// <summary>
/// 技能事件轨道发出的信号类型；可在项目中按需扩展枚举值。
/// </summary>
public enum SkillEventSignal
{
    None = 0,
    AttackHit = 1,
    DamageApply = 2,
    SpawnProjectile = 3,
    SpawnEffect = 4,
    Footstep = 5,
    ComboWindowOpen = 6,
    ComboWindowClose = 7,
    SkillEnd = 8,
}

public static class SkillEventSignalExtensions
{
    public static string GetDisplayName(this SkillEventSignal signal) =>
        signal switch
        {
            SkillEventSignal.None => "无",
            SkillEventSignal.AttackHit => "攻击判定",
            SkillEventSignal.DamageApply => "造成伤害",
            SkillEventSignal.SpawnProjectile => "生成弹道",
            SkillEventSignal.SpawnEffect => "生成特效",
            SkillEventSignal.Footstep => "脚步",
            SkillEventSignal.ComboWindowOpen => "连招窗口开启",
            SkillEventSignal.ComboWindowClose => "连招窗口关闭",
            SkillEventSignal.SkillEnd => "技能结束",
            _ => signal.ToString(),
        };
}
