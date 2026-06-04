using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 位移技能轨道：在片段的起始帧与结束帧之间，将释放者从起点插值移动到局部偏移终点。
/// </summary>
[System.Serializable]
public sealed class SkillMovementTrack : SkillClipTrackBase<SkillMovementClipSegment>
{
    sealed class MovementTrackRuntime
    {
        public readonly List<ActiveMovement> ActiveMovements = new List<ActiveMovement>();
    }

    sealed class ActiveMovement
    {
        public int SegmentIndex;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public Vector3 LocalOffset;
        public bool PlanarOnly;
        public int StartFrame;
        public int EndFrame;
    }

    public override SkillTrackKind Kind => SkillTrackKind.Movement;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("位移片段")]
    public List<SkillMovementClipSegment> Clips = new List<SkillMovementClipSegment>();

    protected override IList<SkillMovementClipSegment> ClipList => Clips;

    public override void OnFrame(SkillPlayContext context)
    {
        base.OnFrame(context);
        UpdateActiveMovements(context);
    }

    protected override string GetSegmentMarkerLabel(SkillMovementClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetMovementLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillMovementClipSegment segment, int index)
    {
        if (context?.Caster == null)
            return;

        var state = context.GetTrackState<MovementTrackRuntime>(TrackId);
        RemoveMovement(state, index);

        var caster = context.Caster.transform;
        state.ActiveMovements.Add(new ActiveMovement
        {
            SegmentIndex = index,
            StartPosition = caster.position,
            StartRotation = caster.rotation,
            LocalOffset = segment.LocalOffset,
            PlanarOnly = segment.PlanarOnly,
            StartFrame = segment.StartFrame,
            EndFrame = segment.EndFrame,
        });

        ApplyMovementAtFrame(context, state.ActiveMovements[state.ActiveMovements.Count - 1], context.CurrentFrame);
    }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillMovementClipSegment segment, int index)
    {
        if (context?.Caster == null)
            return;

        var state = context.GetTrackState<MovementTrackRuntime>(TrackId);
        var movement = FindMovement(state, index);
        if (movement != null)
            ApplyMovementAtFrame(context, movement, movement.EndFrame);

        RemoveMovement(state, index);
    }

    protected override void StopAllSegments(SkillPlayContext context)
    {
        if (context?.Caster == null)
            return;

        var state = context.GetTrackState<MovementTrackRuntime>(TrackId);
        for (var i = 0; i < state.ActiveMovements.Count; i++)
            ApplyMovementAtFrame(context, state.ActiveMovements[i], state.ActiveMovements[i].EndFrame);

        state.ActiveMovements.Clear();
    }

    void UpdateActiveMovements(SkillPlayContext context)
    {
        if (context?.Caster == null || !context.IsPlaying)
            return;

        var state = context.GetTrackState<MovementTrackRuntime>(TrackId);
        if (state.ActiveMovements.Count == 0)
            return;

        var frame = context.CurrentFrame;
        for (var i = 0; i < state.ActiveMovements.Count; i++)
        {
            var movement = state.ActiveMovements[i];
            if (frame < movement.StartFrame || frame > movement.EndFrame)
                continue;

            ApplyMovementAtFrame(context, movement, frame);
        }
    }

    static void ApplyMovementAtFrame(SkillPlayContext context, ActiveMovement movement, int frame)
    {
        var caster = context.Caster.transform;

        var offset = movement.LocalOffset;
        if (movement.PlanarOnly)
            offset.y = 0f;

        var worldOffset = movement.StartRotation * offset;
        var duration = movement.EndFrame - movement.StartFrame;
        if (duration <= 0)
        {
            caster.position = movement.StartPosition + worldOffset;
            return;
        }

        var t = Mathf.Clamp01((frame - movement.StartFrame) / (float)duration);
        caster.position = movement.StartPosition + worldOffset * t;
    }

    static ActiveMovement FindMovement(MovementTrackRuntime state, int segmentIndex)
    {
        for (var i = 0; i < state.ActiveMovements.Count; i++)
        {
            var movement = state.ActiveMovements[i];
            if (movement.SegmentIndex == segmentIndex)
                return movement;
        }

        return null;
    }

    static void RemoveMovement(MovementTrackRuntime state, int segmentIndex)
    {
        for (var i = state.ActiveMovements.Count - 1; i >= 0; i--)
        {
            if (state.ActiveMovements[i].SegmentIndex == segmentIndex)
                state.ActiveMovements.RemoveAt(i);
        }
    }
}
