/*--------------------------------------------------------------------------------------
* Title: 数据脚本自动生成工具
* Author: ZM
* Date:2026/5/13 23:06:24
* Description:数据层,主要负责游戏数据的存储、更新和获取
* Modify: 命名空间须与 GameWorld（ZMGC.Game）一致，否则 WorldTypeManager 不会注册本类，GameWorld.GetDataLayer 将失败。
* 注意:以下文件为自动生成，强制再次生成将会覆盖
----------------------------------------------------------------------------------------*/
using System.Collections.Generic;
using UnityEngine;

namespace ZMGC.Game
{
	public class DialogueDataMgr : IDataBehaviour
	{
		/// <summary>当前对话节点 ID（由 DialogueManager 在进入节点时写入）。</summary>
		public string CurrentNodeId { get; private set; }

		/// <summary>「点击继续」时的下一节点 ID；由各类节点在 <c>OnEnter</c> 中写入。</summary>
		public string NextNodeId { get; private set; }

		/// <summary>是否允许通过「继续」进入下一段对白或 <see cref="NextNodeId"/>。</summary>
		public bool CanClickToContinue { get; private set; }

		public string CurrentBody { get; private set; }
		public SpeakerType CurrentSpeaker { get; private set; }

		/// <summary>当前节点配置的对话/选项背景图（可为 null）。</summary>
		public Sprite CurrentBackgroundSprite { get; private set; }

		/// <summary>进入当前节点时播放的音效（可为 null）。</summary>
		public AudioClip CurrentEnterSfx { get; private set; }

		public string OptionMessage { get; private set; }
		public string[] ChoiceTexts { get; private set; }
		public string[] ChoiceTargetIds { get; private set; }

		/// <summary>
		/// 条件分支节点解析时使用的键：与 <see cref="BranchRoute.ConditionKey"/> 一致；由逻辑层在到达分支前写入。
		/// </summary>
		public string CurrentBranchKey { get; private set; }

		/// <summary>当前对白正文按换行拆分后的段落索引（首段为 0）。</summary>
		public int DialogueBodySegmentIndex { get; private set; }

		private string[] _dialogueBodySegments;

		public void OnCreate()
		 {

		 }

		public void OnDestroy()
		 {

		 }

		public void SetDialogueData(string body, SpeakerType speaker, Sprite background = null, AudioClip enterSfx = null)
		{
			_dialogueBodySegments = SplitBodyIntoParagraphs(body);
			DialogueBodySegmentIndex = 0;
			CurrentBody = _dialogueBodySegments.Length > 0 ? _dialogueBodySegments[0] : string.Empty;
			CurrentSpeaker = speaker;
			OptionMessage = string.Empty;
			ChoiceTexts = new string[0];
			ChoiceTargetIds = new string[0];
			CurrentBackgroundSprite = background;
			CurrentEnterSfx = enterSfx;
		}

		public void SetOptionData(string body, string[] texts, string[] targetIds, Sprite background = null, AudioClip enterSfx = null)
		{
			ClearDialogueParagraphState();
			CurrentBody = body ?? string.Empty;
			CurrentSpeaker = SpeakerType.Narrator;
			OptionMessage = body ?? string.Empty;
			ChoiceTexts = texts ?? new string[0];
			ChoiceTargetIds = targetIds ?? new string[0];
			CurrentBackgroundSprite = background;
			CurrentEnterSfx = enterSfx;
		}

		public void SetBranchKey(string key)
		{
			CurrentBranchKey = key ?? string.Empty;
		}

		public void SetCurrentNodeId(string nodeId)
		{
			CurrentNodeId = nodeId ?? string.Empty;
		}

		public void SetContinueFlow(string nextNodeId)
		{
			NextNodeId = nextNodeId ?? string.Empty;
			RecomputeCanClickToContinue();
		}

		/// <summary>
		/// 离开对白分段状态（进入分支/选项等不会沿用上一节点的分段数据时调用）。
		/// </summary>
		public void ClearDialogueParagraphState()
		{
			_dialogueBodySegments = null;
			DialogueBodySegmentIndex = 0;
		}

		/// <summary>
		/// 若当前对白还有下一段（换行后的正文），推进一段并更新 <see cref="CurrentBody"/>。
		/// </summary>
		/// <returns>已推进到下一段则为 true；否则 false（应改走 <see cref="NextNodeId"/>）。</returns>
		public bool TryAdvanceToNextDialogueSegment()
		{
			if (_dialogueBodySegments == null || _dialogueBodySegments.Length <= 1)
				return false;
			if (DialogueBodySegmentIndex >= _dialogueBodySegments.Length - 1)
				return false;
			DialogueBodySegmentIndex++;
			CurrentBody = _dialogueBodySegments[DialogueBodySegmentIndex];
			RecomputeCanClickToContinue();
			return true;
		}

		private void RecomputeCanClickToContinue()
		{
			bool moreParagraphs = _dialogueBodySegments != null &&
			                      _dialogueBodySegments.Length > 0 &&
			                      DialogueBodySegmentIndex < _dialogueBodySegments.Length - 1;
			bool hasNextNode = !string.IsNullOrEmpty(NextNodeId);
			bool onLastParagraph = _dialogueBodySegments == null ||
			                       _dialogueBodySegments.Length <= 1 ||
			                       DialogueBodySegmentIndex >= _dialogueBodySegments.Length - 1;
			CanClickToContinue = moreParagraphs || (hasNextNode && onLastParagraph);
		}

		private static string[] SplitBodyIntoParagraphs(string body)
		{
			if (body == null)
				return new[] { string.Empty };
			var normalized = body.Replace("\r\n", "\n").Replace('\r', '\n');
			var raw = normalized.Split('\n');
			var list = new List<string>(raw.Length);
			for (var i = 0; i < raw.Length; i++)
			{
				var t = raw[i].Trim();
				if (t.Length > 0)
					list.Add(t);
			}
			return list.Count > 0 ? list.ToArray() : new[] { string.Empty };
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

			CurrentBackgroundSprite = null;
			CurrentEnterSfx = null;

			CurrentBranchKey = string.Empty;
			ClearDialogueParagraphState();
		}
	}
}
