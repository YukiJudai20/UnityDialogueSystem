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
		/// 普通对话节点进入时由逻辑层执行自定义操作。
		/// </summary>
		public void OnDialogueNodeEnter(DialogueNode node)
		{
			// 在这里编写普通对话节点的业务逻辑，例如：
			// 触发任务检查、播放音效、记录对话日志等。
		}
	}
}
