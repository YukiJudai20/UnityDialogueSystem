/*--------------------------------------------------------------------------------------
* Title: 数据脚本自动生成工具
* Author: ZM
* Date:2026/5/13 23:06:24
* Description:数据层,主要负责游戏数据的存储、更新和获取
* Modify: 命名空间须与 GameWorld（ZMGC.Game）一致，否则 WorldTypeManager 不会注册本类，GameWorld.GetDataLayer 将失败。
* 注意:以下文件为自动生成，强制再次生成将会覆盖
----------------------------------------------------------------------------------------*/
namespace ZMGC.Game
{
	public class DialogueDataMgr : IDataBehaviour
	{
		/// <summary>当前对话节点 ID（由 DialogueManager 在进入节点时写入）。</summary>
		public string CurrentNodeId { get; private set; }

		/// <summary>「点击继续」时的下一节点 ID；由各类节点在 <c>OnEnter</c> 中写入。</summary>
		public string NextNodeId { get; private set; }

		/// <summary>是否允许通过「继续」进入 <see cref="NextNodeId"/>（由节点在 <c>OnEnter</c> 中写入）。</summary>
		public bool CanClickToContinue { get; private set; }

		public string CurrentBody { get; private set; }
		public SpeakerType CurrentSpeaker { get; private set; }

		public string OptionMessage { get; private set; }
		public string[] ChoiceTexts { get; private set; }
		public string[] ChoiceTargetIds { get; private set; }

		/// <summary>
		/// 条件分支节点解析时使用的键：与 <see cref="BranchRoute.ConditionKey"/> 一致；由逻辑层在到达分支前写入。
		/// </summary>
		public string CurrentBranchKey { get; private set; }

		public void OnCreate()
		 {

		 }

		public void OnDestroy()
		 {

		 }

		public void SetDialogueData(string body, SpeakerType speaker)
		{
			CurrentBody = body ?? string.Empty;
			CurrentSpeaker = speaker;
			OptionMessage = string.Empty;
			ChoiceTexts = new string[0];
			ChoiceTargetIds = new string[0];
		}

		public void SetOptionData(string body, string[] texts, string[] targetIds)
		{
			CurrentBody = body ?? string.Empty;
			CurrentSpeaker = SpeakerType.Narrator;
			OptionMessage = body ?? string.Empty;
			ChoiceTexts = texts ?? new string[0];
			ChoiceTargetIds = targetIds ?? new string[0];
		}

		public void SetBranchKey(string key)
		{
			CurrentBranchKey = key ?? string.Empty;
		}

		public void SetCurrentNodeId(string nodeId)
		{
			CurrentNodeId = nodeId ?? string.Empty;
		}

		public void SetContinueFlow(string nextNodeId, bool canClickToContinue)
		{
			NextNodeId = nextNodeId ?? string.Empty;
			CanClickToContinue = canClickToContinue && !string.IsNullOrEmpty(NextNodeId);
		}

		/// <summary>结束对话时清空运行时展示与流程相关数据。</summary>
		public void ClearRuntimeState()
		{
			CurrentNodeId = string.Empty;
			NextNodeId = string.Empty;
			CanClickToContinue = false;

			CurrentBody = string.Empty;
			CurrentSpeaker = SpeakerType.NPC;

			OptionMessage = string.Empty;
			ChoiceTexts = new string[0];
			ChoiceTargetIds = new string[0];

			CurrentBranchKey = string.Empty;
		}
	}
}
