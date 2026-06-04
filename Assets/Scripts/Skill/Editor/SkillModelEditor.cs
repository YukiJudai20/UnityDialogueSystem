using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MyGame.Skill.Editor
{
    [CustomEditor(typeof(SkillModel))]
    public sealed class SkillModelEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("打开技能编辑器", GUILayout.Height(28)))
                SkillEditorWindow.Open(target as SkillModel);

            EditorGUILayout.Space(4);
            base.OnInspectorGUI();
        }
    }
}
