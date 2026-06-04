using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 简单玩家：维护可用技能列表，通过技能 ID 委托 <see cref="SkillManager"/> 释放。
/// </summary>
public sealed class Player : MonoBehaviour
{
    [Title("技能")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    [LabelText("技能列表")]
    [SerializeField]
    List<SkillModel> mSkills = new List<SkillModel>();

    public IReadOnlyList<SkillModel> Skills => mSkills;

    /// <summary>
    /// 按技能 ID 查找并释放；ID 与 <see cref="SkillModel.SkillId"/> 一致。
    /// </summary>
    public bool ReleaseSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            Debug.LogWarning($"{nameof(Player)}.{nameof(ReleaseSkill)}: skillId is empty.", this);
            return false;
        }

        var model = FindSkillById(skillId);
        if (model == null)
        {
            Debug.LogWarning($"{nameof(Player)}.{nameof(ReleaseSkill)}: skill '{skillId}' not found in list.", this);
            return false;
        }

        return SkillManager.Instance.ReleaseSkill(model, gameObject);
    }

    public SkillModel FindSkillById(string skillId)
    {
        if (mSkills == null || string.IsNullOrEmpty(skillId))
            return null;

        for (var i = 0; i < mSkills.Count; i++)
        {
            var skill = mSkills[i];
            if (skill != null && skill.SkillId == skillId)
                return skill;
        }

        return null;
    }
}
