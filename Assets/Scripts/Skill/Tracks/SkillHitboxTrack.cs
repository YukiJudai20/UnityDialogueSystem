using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 单次攻击碰撞检测结果（每帧检测时可能对多个 Collider 触发）。
/// </summary>
public readonly struct SkillHitboxHitInfo
{
    public GameObject Caster { get; }
    public SkillModel Model { get; }
    public Collider Target { get; }
    public int TrackIndex { get; }
    public int ClipIndex { get; }
    public int Frame { get; }

    public SkillHitboxHitInfo(
        GameObject caster,
        SkillModel model,
        Collider target,
        int trackIndex,
        int clipIndex,
        int frame)
    {
        Caster = caster;
        Model = model;
        Target = target;
        TrackIndex = trackIndex;
        ClipIndex = clipIndex;
        Frame = frame;
    }
}

/// <summary>
/// 攻击碰撞体轨道：在片段持续期间每逻辑帧对立方体区域做一次物理重叠检测。
/// </summary>
[Serializable]
public sealed class SkillHitboxTrack : SkillClipTrackBase<SkillHitboxClipSegment>
{
    static readonly Collider[] OverlapBuffer = new Collider[32];

    /// <summary>检测到有效碰撞体时触发（同一目标可在不同帧多次触发）。</summary>
    public static event Action<SkillHitboxHitInfo> HitDetected;

    sealed class HitboxTrackRuntime
    {
        public readonly List<ActiveHitbox> ActiveHitboxes = new List<ActiveHitbox>();
    }

    sealed class ActiveHitbox
    {
        public int SegmentIndex;
        public int StartFrame;
        public int EndFrame;
        public SkillHitboxColliderType ColliderType;
        public Vector3 LocalOffset;
        public Vector3 LocalEulerAngles;
        public Vector3 HalfExtents;
        public LayerMask HitLayers;
    }

    public override SkillTrackKind Kind => SkillTrackKind.HitBox;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("碰撞体片段")]
    public List<SkillHitboxClipSegment> Clips = new List<SkillHitboxClipSegment>();

    protected override IList<SkillHitboxClipSegment> ClipList => Clips;

    public override void OnFrame(SkillPlayContext context)
    {
        base.OnFrame(context);
        UpdateActiveHitboxes(context);
    }

    protected override string GetSegmentMarkerLabel(SkillHitboxClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetHitboxLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillHitboxClipSegment segment, int index)
    {
        if (context?.Caster == null)
            return;

        var state = context.GetTrackState<HitboxTrackRuntime>(TrackId);
        RemoveHitbox(state, index);

        state.ActiveHitboxes.Add(CreateActiveHitbox(segment, index));

        DetectForSegment(context, state.ActiveHitboxes[state.ActiveHitboxes.Count - 1], index);
    }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillHitboxClipSegment segment, int index)
    {
        if (context?.Caster == null)
            return;

        var state = context.GetTrackState<HitboxTrackRuntime>(TrackId);
        var hitbox = FindHitbox(state, index);
        if (hitbox != null && hitbox.StartFrame != hitbox.EndFrame)
            DetectForSegment(context, hitbox, index);

        RemoveHitbox(state, index);
    }

    protected override void StopAllSegments(SkillPlayContext context)
    {
        context?.GetTrackState<HitboxTrackRuntime>(TrackId).ActiveHitboxes.Clear();
    }

    void UpdateActiveHitboxes(SkillPlayContext context)
    {
        if (context?.Caster == null || !context.IsPlaying)
            return;

        var state = context.GetTrackState<HitboxTrackRuntime>(TrackId);
        if (state.ActiveHitboxes.Count == 0)
            return;

        var frame = context.CurrentFrame;
        for (var i = 0; i < state.ActiveHitboxes.Count; i++)
        {
            var hitbox = state.ActiveHitboxes[i];
            if (frame <= hitbox.StartFrame || frame >= hitbox.EndFrame)
                continue;

            DetectForSegment(context, hitbox, hitbox.SegmentIndex);
        }
    }

    static ActiveHitbox CreateActiveHitbox(SkillHitboxClipSegment segment, int index)
    {
        var hitbox = new ActiveHitbox
        {
            SegmentIndex = index,
            StartFrame = segment.StartFrame,
            EndFrame = segment.EndFrame,
            ColliderType = segment.ColliderType,
            LocalOffset = segment.LocalOffset,
            LocalEulerAngles = segment.LocalEulerAngles,
            HitLayers = segment.HitLayers,
        };

        if (segment.ColliderType == SkillHitboxColliderType.Box)
            hitbox.HalfExtents = segment.GetBoxHalfExtents();

        return hitbox;
    }

    void DetectForSegment(SkillPlayContext context, ActiveHitbox hitbox, int clipIndex)
    {
        var caster = context.Caster;
        var casterTransform = caster.transform;
        var count = hitbox.ColliderType switch
        {
            SkillHitboxColliderType.Box => OverlapBox(casterTransform, hitbox),
            _ => 0,
        };

        var trackIndex = FindTrackIndex(context.Model);

        for (var i = 0; i < count; i++)
        {
            var col = OverlapBuffer[i];
            if (col == null || IsCasterCollider(casterTransform, col))
                continue;

            HitDetected?.Invoke(new SkillHitboxHitInfo(
                caster,
                context.Model,
                col,
                trackIndex,
                clipIndex,
                context.CurrentFrame));
        }
    }

    static int OverlapBox(Transform casterTransform, ActiveHitbox hitbox)
    {
        var worldCenter = casterTransform.TransformPoint(hitbox.LocalOffset);
        var worldRotation = casterTransform.rotation * Quaternion.Euler(hitbox.LocalEulerAngles);
        return Physics.OverlapBoxNonAlloc(
            worldCenter,
            hitbox.HalfExtents,
            OverlapBuffer,
            worldRotation,
            hitbox.HitLayers,
            QueryTriggerInteraction.Collide);
    }

    int FindTrackIndex(SkillModel model)
    {
        if (model?.Tracks == null)
            return -1;

        for (var i = 0; i < model.Tracks.Count; i++)
        {
            if (ReferenceEquals(model.Tracks[i], this))
                return i;
        }

        return -1;
    }

    static bool IsCasterCollider(Transform casterTransform, Collider col)
    {
        if (col == null)
            return true;
        return col.transform == casterTransform || col.transform.IsChildOf(casterTransform);
    }

    static ActiveHitbox FindHitbox(HitboxTrackRuntime state, int segmentIndex)
    {
        for (var i = 0; i < state.ActiveHitboxes.Count; i++)
        {
            var hitbox = state.ActiveHitboxes[i];
            if (hitbox.SegmentIndex == segmentIndex)
                return hitbox;
        }

        return null;
    }

    static void RemoveHitbox(HitboxTrackRuntime state, int segmentIndex)
    {
        for (var i = state.ActiveHitboxes.Count - 1; i >= 0; i--)
        {
            if (state.ActiveHitboxes[i].SegmentIndex == segmentIndex)
                state.ActiveHitboxes.RemoveAt(i);
        }
    }
}
