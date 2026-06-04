using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 音频技能轨道：按帧播放与停止 AudioSource（每段使用独立临时音源，支持多段重叠）。
/// </summary>
[System.Serializable]
public sealed class SkillAudioTrack : SkillClipTrackBase<SkillAudioClipSegment>
{
    sealed class AudioTrackRuntime
    {
        public readonly Dictionary<int, AudioSource> ActiveSources = new Dictionary<int, AudioSource>();
    }

    public override SkillTrackKind Kind => SkillTrackKind.Audio;

    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("音频片段")]
    public List<SkillAudioClipSegment> Clips = new List<SkillAudioClipSegment>();

    protected override IList<SkillAudioClipSegment> ClipList => Clips;

    protected override string GetSegmentMarkerLabel(SkillAudioClipSegment segment, int index) =>
        SkillSegmentLabelUtil.GetAudioLabel(segment);

    protected override void OnSegmentStart(SkillPlayContext context, SkillAudioClipSegment segment, int index)
    {
        if (segment.Clip == null || context.Caster == null)
            return;

        var state = context.GetTrackState<AudioTrackRuntime>(TrackId);
        StopSegmentSource(state, index);

        var host = new GameObject($"SkillAudio_{TrackId}_{index}");
        host.transform.SetParent(context.Caster.transform, false);

        var source = host.AddComponent<AudioSource>();
        source.clip = segment.Clip;
        source.volume = segment.Volume;
        source.loop = segment.Loop;
        source.Play();

        state.ActiveSources[index] = source;
    }

    protected override void OnSegmentEnd(SkillPlayContext context, SkillAudioClipSegment segment, int index)
    {
        var state = context.GetTrackState<AudioTrackRuntime>(TrackId);
        StopSegmentSource(state, index);
    }

    protected override void StopAllSegments(SkillPlayContext context)
    {
        if (context == null)
            return;

        var state = context.GetTrackState<AudioTrackRuntime>(TrackId);
        var keys = new List<int>(state.ActiveSources.Keys);
        for (var i = 0; i < keys.Count; i++)
            StopSegmentSource(state, keys[i]);

        state.ActiveSources.Clear();
    }

    static void StopSegmentSource(AudioTrackRuntime state, int index)
    {
        if (!state.ActiveSources.TryGetValue(index, out var source) || source == null)
            return;

        if (source.isPlaying)
            source.Stop();

        if (source.gameObject != null)
            Object.Destroy(source.gameObject);

        state.ActiveSources.Remove(index);
    }
}
