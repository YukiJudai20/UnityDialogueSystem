using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能播放时的运行时上下文；由技能管理器在实例化技能后填充并随帧推进。
/// </summary>
public sealed class SkillPlayContext
{
    readonly Dictionary<string, object> _trackStates = new Dictionary<string, object>();

    public SkillModel Model { get; set; }
    public GameObject Caster { get; set; }
    public int CurrentFrame { get; set; }
    public bool IsPlaying { get; set; }

    public T GetTrackState<T>(string trackId) where T : class, new()
    {
        if (string.IsNullOrEmpty(trackId))
            return new T();

        if (!_trackStates.TryGetValue(trackId, out var state) || state is not T typed)
        {
            typed = new T();
            _trackStates[trackId] = typed;
        }

        return typed;
    }

    public void ReleaseTrackState(string trackId)
    {
        if (!string.IsNullOrEmpty(trackId))
            _trackStates.Remove(trackId);
    }

    public void ClearAllTrackStates() => _trackStates.Clear();
}
