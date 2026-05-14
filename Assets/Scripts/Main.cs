using System.Threading.Tasks;
using MyGame.Hall;
using ZMGC.Game;
using UnityEngine;
using ZM.ZMAsset;
using ZM.UI;

public class Main : MonoBehaviour
{
    public DialogueGraphAsset dialogueGraphAsset;
    public string branchKey = "a";

    private void Awake()
    {
        ZMAsset.InitFrameWork();
        UIModule.Instance.Initialize();
    }

    private void Start()
    {
        WorldManager.CreateWorld<GameWorld>();
        UIModule.Instance.PopUpWindow<BaseWindow>();
        GameWorld.GetDataLayer<DialogueDataMgr>().SetBranchKey(branchKey);
        DialogueManager.Instance.StartDialogue(dialogueGraphAsset);
    }
}
