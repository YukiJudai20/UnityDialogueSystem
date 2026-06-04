using UnityEngine;

/// <summary>
/// 技能事件轨道在指定帧触发信号时携带的上下文。
/// </summary>
public readonly struct SkillEventContext
{
    public SkillModel Model { get; }
    public GameObject Caster { get; }
    public SkillPlayContext PlayContext { get; }
    public SkillEventTrack Track { get; }
    public SkillEventSignal Signal { get; }
    public int Frame { get; }
    public int ClipIndex { get; }

    public SkillEventContext(
        SkillModel model,
        GameObject caster,
        SkillPlayContext playContext,
        SkillEventTrack track,
        SkillEventSignal signal,
        int frame,
        int clipIndex)
    {
        Model = model;
        Caster = caster;
        PlayContext = playContext;
        Track = track;
        Signal = signal;
        Frame = frame;
        ClipIndex = clipIndex;
    }
}
