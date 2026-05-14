using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MyGame.Dialogue.Editor
{
    [CustomEditor(typeof(DialogueGraphAsset))]
    public sealed class DialogueGraphAssetEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("打开节点编辑器", GUILayout.Height(28)))
                DialogueGraphEditorWindow.Open(target as DialogueGraphAsset);
            EditorGUILayout.Space(4);
            base.OnInspectorGUI();
        }
    }
}
