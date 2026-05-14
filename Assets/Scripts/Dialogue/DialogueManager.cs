using System;
using UnityEngine;
using ZMGC.Game;
using ZM.UI;
using ZM.ZMAsset;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class DialogueManager : MonoSingleton<DialogueManager>
{

    [SerializeField]
    private DialogueGraphAsset mCurrentGraph;

    private DialogueNodeBase mCurrentNode;

    private Action mOnDialogueEnded;

    #region 生命周期

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Continue();
        }
    }

    #endregion

    #region 对话控制

    /// <summary>
    /// 开始播放指定的对话图。
    /// </summary>
    public void StartDialogue(DialogueGraphAsset graph, Action onDialogueEnded = null)
    {
        if (graph == null)
        {
            Debug.LogError("DialogueManager.StartDialogue: graph is null.");
            return;
        }

        mOnDialogueEnded = onDialogueEnded;

        mCurrentGraph = graph;
        mCurrentNode = null;

        if (!graph.TryGetResolvedEntryNodeId(out var entryId))
        {
            Debug.LogError("DialogueManager.StartDialogue: graph has no valid entry node.");
            return;
        }

        if (entryId != graph.EntryNodeId)
        {
            Debug.LogWarning(
                $"DialogueManager.StartDialogue: EntryNodeId '{graph.EntryNodeId}' has no matching node; starting at '{entryId}'. " +
                "Run 「清理无效引用」 on the graph asset or fix 起始节点 in the editor.");
        }

        EnterNode(entryId);
    }

    /// <summary>
    /// 进入指定节点：更新数据层当前节点 ID，并调用节点 <see cref="DialogueNodeBase.OnEnter"/>。
    /// </summary>
    public void EnterNode(string nodeId)
    {
        if (mCurrentGraph == null)
        {
            Debug.LogError("DialogueManager.EnterNode: no active graph.");
            return;
        }

        mCurrentNode = mCurrentGraph.FindNode(nodeId);
        if (mCurrentNode == null)
        {
            Debug.LogError($"DialogueManager.EnterNode: node '{nodeId}' not found.");
            return;
        }

        var data = GameWorld.GetDataLayer<DialogueDataMgr>();
        data?.SetCurrentNodeId(nodeId);

        mCurrentNode.OnEnter();
    }

    /// <summary>
    /// 对话窗口「继续」：仅当数据层允许且已记录下一节点时，进入下一节点。
    /// </summary>
    public void Continue()
    {
        var data = GameWorld.GetDataLayer<DialogueDataMgr>();
        if (data == null)
            return;

        if (!data.CanClickToContinue)
            return;

        if (string.IsNullOrEmpty(data.NextNodeId))
        {
            Debug.Log("DialogueManager.Continue: no next node.");
            return;
        }

        EnterNode(data.NextNodeId);
    }

    /// <summary>
    /// 由 <see cref="EndNode"/> 调用：关闭对话与选项窗口、清空数据层、结束会话并触发回调。
    /// </summary>
    public void FinishDialogueFromEndNode()
    {
        mCurrentNode = null;
        mCurrentGraph = null;

        if (UIModule.Instance != null)
        {
            UIModule.Instance.HideWindow<DialogueWindow>();
        }

        GameWorld.GetDataLayer<DialogueDataMgr>()?.ClearRuntimeState();

        mOnDialogueEnded?.Invoke();
    }

    #endregion
}
