/*--------------------------------------------------------------------------------------
* Title: 业务逻辑脚本自动生成工具
* Author: ZM
* Date:2026/5/13 23:06:19
* Description:业务逻辑层,主要负责游戏的业务逻辑处理
* Modify: 命名空间须与 GameWorld（ZMGC.Game）一致，否则 WorldTypeManager 不会注册本类。
* 注意:以下文件为自动生成，强制再次生成将会覆盖
----------------------------------------------------------------------------------------*/
namespace ZMGC.Game
{
	public class DialogueLogicCtrl : ILogicBehaviour
	{
		public void OnCreate()
		 {

		 }

		public void OnDestroy()
		 {

		 }

		/// <summary>
		/// 普通对话节点进入时调用（先于 <see cref="OnDialogueLineEvent"/>，且不受「逻辑事件 ID」是否为空影响）。
		/// 适合与具体节点类型绑定的通用逻辑。
		/// </summary>
		public void OnDialogueNodeEnter(DialogueNode node)
		{
			// 例如：记录「进入过某段对白」、播环境音等。
		}

		/// <summary>
		/// 对话节点上配置的「逻辑事件 ID」非空时，在本句数据写入、窗口弹出且 <c>DialogueRefresh</c> 派发之后调用。
		/// 在 <see cref="OnDialogueNodeEnter"/> 之后执行。
		/// </summary>
		/// <param name="eventId">节点上填写的字符串，建议用稳定常量如 <c>boss_intro_done</c>。</param>
		/// <param name="node">当前对话节点实例，可读取 Body、Speaker 等。</param>
		public void OnDialogueLineEvent(string eventId, DialogueNode node)
		{
			// 示例：
			// switch (eventId) { case "boss_intro_done": StartBossPhase1(); break; }
		}

		/// <summary>
		/// 选项上配置的「逻辑事件 ID」非空时，在玩家点选该选项之后、进入目标节点之前调用。
		/// </summary>
		public void OnDialogueChoiceEvent(string eventId, OptionNode optionNode, int choiceIndex, DialogueChoice choice)
		{
			// 示例：
			// switch (eventId) { case "give_potion": Inventory.Add("potion"); break; }
		}

		/// <summary>
		/// 结束节点上配置的「逻辑事件 ID」非空时，在关闭对话窗口与清空数据层之前调用。
		/// 可与 <see cref="DialogueManager.StartDialogue"/> 的全局结束回调配合使用（本方法更贴近图里某一结束出口）。
		/// </summary>
		public void OnDialogueEndEvent(string eventId, EndNode endNode)
		{
			// 示例：
			// switch (eventId) { case "start_boss_fight": BossBattle.Begin(); break; }
		}
	}
}
