using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 技能打断轨道：片段覆盖的帧区间内允许强制中断当前技能（由 <see cref="SkillManager"/> 在释放新技能时判定）。
/// </summary>
[System.Serializable]
public sealed class SkillInterruptTrack : SkillClipTrackBase<SkillInterruptClipSegment>
{
    public override SkillTrackKind Kind => SkillTrackKind.Interrupt;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("打断片段")]
    public List<SkillInterruptClipSegment> Clips = new List<SkillInterruptClipSegment>();

    protected override IList<SkillInterruptClipSegment> ClipList => Clips;

    /// <summary>指定逻辑帧是否落在任一有效打断片段内（含起止帧）。</summary>
    public bool ContainsFrame(int frame)
    {
        if (Clips == null)
            return false;

        for (var i = 0; i < Clips.Count; i++)
        {
            var seg = Clips[i];
            if (seg == null || !seg.IsValid())
                continue;

            if (frame >= seg.StartFrame && frame <= seg.EndFrame)
                return true;
        }

        return false;
    }

    protected override string GetSegmentMarkerLabel(SkillInterruptClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetInterruptLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillInterruptClipSegment segment, int index) { }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillInterruptClipSegment segment, int index) { }

    protected override void StopAllSegments(SkillPlayContext context) { }
}
