using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单次技能释放的运行时实例：持有释放者、当前逻辑帧，并由 <see cref="SkillManager"/> 推进。
/// </summary>
public sealed class SkillInstance
{
    readonly SkillPlayContext _context;

    public SkillModel Model { get; }
    public GameObject Caster { get; }
    public SkillPlayContext Context => _context;
    public int CurrentFrame { get; private set; }
    public bool IsFinished { get; private set; }

    /// <summary>当前逻辑帧是否允许被外部操作强制中断。</summary>
    public bool CanBeInterrupted =>
        !IsFinished && Model != null && Model.IsInterruptAllowedAtFrame(CurrentFrame);

    public SkillInstance(SkillModel model, GameObject caster)
    {
        Model = model;
        Caster = caster;
        _context = new SkillPlayContext
        {
            Model = model,
            Caster = caster,
            CurrentFrame = 0,
            IsPlaying = true,
        };
        CurrentFrame = 0;
        Begin();
    }

    void Begin()
    {
        var tracks = Model?.Tracks;
        if (tracks == null)
            return;

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            if (track != null && track.Enabled)
                track.OnSkillBegin(_context);
        }
    }

    /// <summary>推进一个逻辑帧；处理当前帧轨道事件，结束后标记 <see cref="IsFinished"/>。</summary>
    public void TickLogicFrame()
    {
        if (IsFinished || Model == null)
            return;

        _context.CurrentFrame = CurrentFrame;
        _context.IsPlaying = true;
        ApplyTracksOnFrame();

        var lastFrame = Model.GetClampedFrameCount() - 1;
        if (CurrentFrame >= lastFrame)
        {
            End();
            return;
        }

        CurrentFrame++;
    }

    void ApplyTracksOnFrame()
    {
        var tracks = Model.Tracks;
        if (tracks == null)
            return;

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            if (track != null && track.Enabled)
                track.OnFrame(_context);
        }
    }

    void End()
    {
        if (IsFinished)
            return;

        _context.IsPlaying = false;
        var tracks = Model?.Tracks;
        if (tracks != null)
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                if (track != null && track.Enabled)
                    track.OnSkillEnd(_context);
            }
        }

        _context.ClearAllTrackStates();
        IsFinished = true;
    }

    /// <summary>立即打断技能并清理轨道状态。</summary>
    public void Interrupt()
    {
        End();
    }
}
