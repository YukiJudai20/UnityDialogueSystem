using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 技能数据资产：以帧为时间单位的技能定义，包含多条可扩展的 <see cref="SkillTrackBase"/> 轨道。
/// 运行时由技能管理器读取本资产并生成技能实例。
/// </summary>
[CreateAssetMenu(menuName = "SkillSystem/技能数据", fileName = "SkillModel")]
public sealed class SkillModel : ScriptableObject
{
    public const int MinFrameCount = 1;
    public const int DefaultFrameCount = 30;

    [Title("基础")]
    [LabelText("技能 ID")]
    [Tooltip("策划或代码中用于查找、释放的唯一标识。")]
    public string SkillId = string.Empty;

    [LabelText("技能名称")]
    public string SkillName = "新技能";

    [LabelText("技能帧数")]
    [MinValue(MinFrameCount)]
    [Tooltip("技能总时长 = 帧数 × 每帧时长；轨道上的事件帧不得超过 FrameCount - 1。")]
    public int FrameCount = DefaultFrameCount;

    [Title("轨道")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("技能轨道")]
    [SerializeReference]
    public List<SkillTrackBase> Tracks = new List<SkillTrackBase>();

    /// <summary>有效帧数（至少为 <see cref="MinFrameCount"/>）。</summary>
    public int GetClampedFrameCount() => Mathf.Max(MinFrameCount, FrameCount);

    public SkillTrackBase FindTrack(string trackId)
    {
        if (string.IsNullOrEmpty(trackId) || Tracks == null)
            return null;

        for (var i = 0; i < Tracks.Count; i++)
        {
            var track = Tracks[i];
            if (track != null && track.TrackId == trackId)
                return track;
        }

        return null;
    }

    public int IndexOfTrack(string trackId)
    {
        if (string.IsNullOrEmpty(trackId) || Tracks == null)
            return -1;

        for (var i = 0; i < Tracks.Count; i++)
        {
            var track = Tracks[i];
            if (track != null && track.TrackId == trackId)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 指定逻辑帧是否处于任一启用的打断轨道片段内（含起止帧）。
    /// </summary>
    public bool IsInterruptAllowedAtFrame(int frame)
    {
        if (Tracks == null)
            return false;

        for (var i = 0; i < Tracks.Count; i++)
        {
            if (Tracks[i] is not SkillInterruptTrack interruptTrack || !interruptTrack.Enabled)
                continue;

            if (interruptTrack.ContainsFrame(frame))
                return true;
        }

        return false;
    }

    /// <summary>为所有轨道补全 ID，并按技能帧数校正数据。</summary>
    [Button("校正轨道数据", ButtonSizes.Medium)]
    public void SanitizeTracks()
    {
        if (Tracks == null)
            return;

        var frameCount = GetClampedFrameCount();
        FrameCount = frameCount;

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < Tracks.Count; i++)
        {
            var track = Tracks[i];
            if (track == null)
                continue;

            track.EnsureTrackId();
            while (!usedIds.Add(track.TrackId))
                track.TrackId = Guid.NewGuid().ToString("N");

            track.EditorOrder = i;
            track.ClampToSkillFrames(frameCount);
        }
    }

    private void OnValidate()
    {
        FrameCount = Mathf.Max(MinFrameCount, FrameCount);
        if (Tracks == null)
            return;

        var frameCount = GetClampedFrameCount();
        for (var i = 0; i < Tracks.Count; i++)
        {
            var track = Tracks[i];
            if (track == null)
                continue;
            track.ClampToSkillFrames(frameCount);
        }
    }
}
