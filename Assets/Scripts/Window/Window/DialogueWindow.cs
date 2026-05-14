/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Author:ZM 铸梦
 *Date:2026/5/14 12:14:58
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使用
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using ZMGC.Game;

namespace ZM.UI
{
	public class DialogueWindow:WindowBase
	{
		private const string OptionItemPrefabResource = "DialogueOption";

		private UIEventControl.EventHandler mOnDialogueRefresh;
		private UIEventControl.EventHandler mOnOptionRefresh;

		 public DialogueWindowDataComponent dataCompt;
	
		 #region 生命周期函数
		 //调用机制与Mono Awake一致
		 public override void OnAwake()
		 {
			 dataCompt=gameObject.GetComponent<DialogueWindowDataComponent>();
			 dataCompt.InitComponent(this);
			 base.OnAwake();

			 mOnDialogueRefresh = _ => RefreshFromDataLayer();
			 mOnOptionRefresh = _ => RefreshFromDataLayer();
			 UIEventControl.AddEvent(UIEventEnum.DialogueRefresh, mOnDialogueRefresh);
			 UIEventControl.AddEvent(UIEventEnum.OptionRefresh, mOnOptionRefresh);
		 }
		 //物体显示时执行
		 public override void OnShow()
		 {
			 RefreshFromDataLayer();
			 base.OnShow();
		 }
		 //物体隐藏时执行
		 public override void OnHide()
		 {
			 base.OnHide();
		 }
		 //物体销毁时执行
		 public override void OnDestroy()
		 {
			 UIEventControl.RemoveEvent(UIEventEnum.DialogueRefresh, mOnDialogueRefresh);
			 UIEventControl.RemoveEvent(UIEventEnum.OptionRefresh, mOnOptionRefresh);
			 ClearOptionItems();
			 base.OnDestroy();
		 }
		 #endregion
		 #region API Function
		public void SetDialogueData(DialogueDataMgr data)
		{
			if (data == null)
				return;

			switch (data.CurrentSpeaker)
			{
				case SpeakerType.Player:
					dataCompt.PlayerNameText.text = "玩家";
					dataCompt.NPCNameText.text = string.Empty;
					break;
				case SpeakerType.NPC:
					dataCompt.NPCNameText.text = "NPC";
					dataCompt.PlayerNameText.text = string.Empty;
					break;
				case SpeakerType.Narrator:
					dataCompt.PlayerNameText.text = string.Empty;
					dataCompt.NPCNameText.text = string.Empty;
					break;
				default:
					break;
			}
			dataCompt.DialogueText.text = data.CurrentBody;
			RebuildOptionList(data);
		}

		private void RefreshFromDataLayer()
		{
			var data = GameWorld.GetDataLayer<DialogueDataMgr>();
			SetDialogueData(data);
		}

		private void RebuildOptionList(DialogueDataMgr data)
		{
			ClearOptionItems();
			var root = dataCompt.OptionButtonRootGameObject;
			if (root == null)
				return;

			var texts = data.ChoiceTexts;
			var targets = data.ChoiceTargetIds;
			bool hasOptions = texts != null && texts.Length > 0;
			root.SetActive(hasOptions);
			if (!hasOptions)
				return;

			var prefab = Resources.Load<GameObject>(OptionItemPrefabResource);
			if (prefab == null)
			{
				Debug.LogError($"DialogueWindow: 未找到 Resources/{OptionItemPrefabResource}.prefab，无法生成选项。");
				return;
			}

			var parent = root.transform;
			for (int i = 0; i < texts.Length; i++)
			{
				var go = UnityEngine.Object.Instantiate(prefab, parent);
				var item = go.GetComponent<DialogueOption>();
				if (item == null)
				{
					Debug.LogError("DialogueWindow: DialogueOption 预制体根节点缺少 DialogueOption 组件。");
					UnityEngine.Object.Destroy(go);
					continue;
				}

				item.OnInitialize();
				string label = texts[i] ?? string.Empty;
				string targetId = (targets != null && i < targets.Length) ? targets[i] : string.Empty;
				item.SetItemData(label, () => OnOptionButtonClicked(targetId));
			}
		}

		private void OnOptionButtonClicked(string targetNodeId)
		{
			if (string.IsNullOrEmpty(targetNodeId))
			{
				Debug.LogWarning("DialogueWindow: 选项未绑定目标节点。");
				return;
			}
			DialogueManager.Instance?.EnterNode(targetNodeId);
		}

		private void ClearOptionItems()
		{
			var root = dataCompt != null ? dataCompt.OptionButtonRootGameObject : null;
			if (root == null)
				return;

			var t = root.transform;
			for (int i = t.childCount - 1; i >= 0; i--)
			{
				var child = t.GetChild(i).gameObject;
				var opt = child.GetComponent<DialogueOption>();
				opt?.OnDispose();
				UnityEngine.Object.Destroy(child);
			}
		}
		 #endregion
		 #region UI组件事件
		 #endregion
	}
}
