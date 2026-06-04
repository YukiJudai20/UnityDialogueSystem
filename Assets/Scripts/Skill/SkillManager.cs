using System.Collections.Generic;
using UnityEngine;
using ZM.ZMAsset;

/// <summary>
/// 技能管理器：创建技能实例、维护当前释放中的技能队列，并按固定逻辑帧率驱动更新。
/// </summary>
public sealed class SkillManager : MonoSingleton<SkillManager>
{
    [SerializeField]
    [Tooltip("每秒推进的逻辑帧数，与技能数据中的帧序号对应。")]
    float mLogicFramesPerSecond = 30f;

    readonly List<SkillInstance> mActiveInstances = new List<SkillInstance>();
    float mLogicFrameAccumulator;

    public IReadOnlyList<SkillInstance> ActiveInstances => mActiveInstances;

    /// <summary>当前逻辑帧率（至少 1）。</summary>
    public float LogicFramesPerSecond
    {
        get => mLogicFramesPerSecond;
        set => mLogicFramesPerSecond = Mathf.Max(1f, value);
    }

    protected override void OnAwake()
    {
        LogicFramesPerSecond = mLogicFramesPerSecond;
    }

    void Update()
    {
        if (mActiveInstances.Count == 0)
            return;

        var interval = 1f / LogicFramesPerSecond;
        mLogicFrameAccumulator += Time.deltaTime;

        while (mLogicFrameAccumulator >= interval)
        {
            mLogicFrameAccumulator -= interval;
            TickLogicFrame();
        }
    }

    /// <summary>
    /// 根据技能数据创建实例并加入释放队列。
    /// 若释放者已有进行中的技能：仅当其处于打断片段覆盖的帧内时才先中断旧技能；否则释放失败。
    /// </summary>
    public bool ReleaseSkill(SkillModel model, GameObject caster)
    {
        if (model == null)
        {
            Debug.LogError("SkillManager.ReleaseSkill: model is null.");
            return false;
        }

        if (caster == null)
        {
            Debug.LogError("SkillManager.ReleaseSkill: caster is null.");
            return false;
        }

        if (!TryInterruptActiveSkillsByCaster(caster))
            return false;

        var instance = new SkillInstance(model, caster);
        mActiveInstances.Add(instance);
        return true;
    }

    /// <summary>释放者当前是否有进行中的技能。</summary>
    public bool HasActiveSkill(GameObject caster)
    {
        if (caster == null)
            return false;

        for (var i = 0; i < mActiveInstances.Count; i++)
        {
            if (mActiveInstances[i].Caster == caster && !mActiveInstances[i].IsFinished)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 打断释放者所有进行中的技能；若任一技能当前帧不可打断则返回 false 且不修改队列。
    /// </summary>
    public bool TryInterruptActiveSkillsByCaster(GameObject caster)
    {
        if (caster == null)
            return false;

        for (var i = 0; i < mActiveInstances.Count; i++)
        {
            var instance = mActiveInstances[i];
            if (instance.Caster != caster || instance.IsFinished)
                continue;

            if (!instance.CanBeInterrupted)
            {
                Debug.LogWarning(
                    $"SkillManager: 释放者 {caster.name} 的技能「{instance.Model.SkillName}」当前帧 {instance.CurrentFrame} 不可打断。",
                    caster);
                return false;
            }
        }

        for (var i = mActiveInstances.Count - 1; i >= 0; i--)
        {
            var instance = mActiveInstances[i];
            if (instance.Caster != caster || instance.IsFinished)
                continue;

            instance.Interrupt();
            mActiveInstances.RemoveAt(i);
        }

        return true;
    }

    /// <summary>推进所有进行中技能的一个逻辑帧。</summary>
    public void TickLogicFrame()
    {
        for (var i = mActiveInstances.Count - 1; i >= 0; i--)
        {
            var instance = mActiveInstances[i];
            instance.TickLogicFrame();
            if (instance.IsFinished)
                mActiveInstances.RemoveAt(i);
        }
    }

    /// <summary>打断指定释放者的所有技能。</summary>
    public void InterruptAllByCaster(GameObject caster)
    {
        if (caster == null)
            return;

        for (var i = mActiveInstances.Count - 1; i >= 0; i--)
        {
            var instance = mActiveInstances[i];
            if (instance.Caster != caster)
                continue;

            instance.Interrupt();
            mActiveInstances.RemoveAt(i);
        }
    }

    /// <summary>清空队列并打断所有技能。</summary>
    public void ClearAll()
    {
        for (var i = 0; i < mActiveInstances.Count; i++)
            mActiveInstances[i].Interrupt();

        mActiveInstances.Clear();
        mLogicFrameAccumulator = 0f;
    }
}
