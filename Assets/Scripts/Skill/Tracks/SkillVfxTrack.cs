using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 特效技能轨道：按帧实例化/销毁特效预制体。
/// </summary>
[System.Serializable]
public sealed class SkillVfxTrack : SkillClipTrackBase<SkillVfxClipSegment>
{
    sealed class VfxTrackRuntime
    {
        public readonly Dictionary<int, GameObject> ActiveInstances = new Dictionary<int, GameObject>();
    }

    public override SkillTrackKind Kind => SkillTrackKind.Vfx;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("特效片段")]
    public List<SkillVfxClipSegment> Clips = new List<SkillVfxClipSegment>();

    protected override IList<SkillVfxClipSegment> ClipList => Clips;

    protected override string GetSegmentMarkerLabel(SkillVfxClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetVfxLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillVfxClipSegment segment, int index)
    {
        if (segment.Prefab == null || context.Caster == null)
            return;

        var state = context.GetTrackState<VfxTrackRuntime>(TrackId);
        DestroySegmentInstance(state, index);

        var instance = Object.Instantiate(segment.Prefab, context.Caster.transform);
        instance.transform.localPosition = segment.Offset;
        instance.transform.localEulerAngles = segment.LocalEulerAngles;

        PlayVfx(instance);
        state.ActiveInstances[index] = instance;
    }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillVfxClipSegment segment, int index)
    {
        var state = context.GetTrackState<VfxTrackRuntime>(TrackId);
        DestroySegmentInstance(state, index);
    }

    protected override void StopAllSegments(SkillPlayContext context)
    {
        if (context == null)
            return;

        var state = context.GetTrackState<VfxTrackRuntime>(TrackId);
        var keys = new List<int>(state.ActiveInstances.Keys);
        for (var i = 0; i < keys.Count; i++)
            DestroySegmentInstance(state, keys[i]);

        state.ActiveInstances.Clear();
    }

    static void PlayVfx(GameObject instance)
    {
        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (ps != null)
                ps.Play(true);
        }
    }

    static void StopVfx(GameObject instance)
    {
        if (instance == null)
            return;

        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    static void DestroySegmentInstance(VfxTrackRuntime state, int index)
    {
        if (!state.ActiveInstances.TryGetValue(index, out var instance) || instance == null)
            return;

        StopVfx(instance);
        Object.Destroy(instance);
        state.ActiveInstances.Remove(index);
    }
}
