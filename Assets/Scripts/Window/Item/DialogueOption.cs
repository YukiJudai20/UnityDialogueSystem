/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Author:铸梦
 *Date:2026/5/12 17:21:18
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，再次生成后会以代码追加的形式新增,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine;
using UnityEngine.UI;
using SuperScrollView;

namespace ZM.UI
{
	public class DialogueOption:MonoBehaviour
	{
		#region 自定义字段
		public   Button  OptionButton;

		public   Text  OptionTextText;

		#endregion


		#region 生命周期
		//脚本初始化接口 (为保证生命周期的执行顺序，请在View层调用该接口确保需要初始化的数据正常执行)
		public void OnInitialize()
		{
		}

		public void SetItemData(string label, UnityEngine.Events.UnityAction onPick)
		{
			if (OptionTextText != null)
				OptionTextText.text = label ?? string.Empty;
			if (OptionButton != null)
			{
				OptionButton.onClick.RemoveAllListeners();
				if (onPick != null)
					OptionButton.onClick.AddListener(onPick);
			}
		}
		//物体销毁时执行 (为保证生命周期的执行顺序，请在View层调用该接口确保需要释放时的接口正常调用)
		public  void OnDispose()
		{
			if (OptionButton != null)
				OptionButton.onClick.RemoveAllListeners();
		}
		#endregion


		#region UI组件事件

		 #endregion


	}
}
