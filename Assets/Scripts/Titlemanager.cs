using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public GameObject jumpButtonObj; // 拖曳你在標題畫面做好的跳轉按鈕

    [Header("場景設定")]
    public string gameSceneName = "GameScene"; // 🌟 確認這裡是你遊戲場景的名字！

    void Start()
    {
        // 檢查電腦記憶，是否通關過結局？
        if (jumpButtonObj != null)
        {
            if (PlayerPrefs.GetInt("HasClearedEnding", 0) == 1)
            {
                jumpButtonObj.SetActive(true); // 顯示跳躍按鈕
            }
            else
            {
                jumpButtonObj.SetActive(false); // 隱藏跳躍按鈕
            }
        }
    }

    // 1. 正常開始遊戲 (取代你原本單純的 StartGame)
    public void StartNormalGame()
    {
        Debug.Log("準備從頭開始遊戲...");

        // 🌟 核心保險：清除任何殘留的跳轉指令，確保絕對是「從頭開始」
        PlayerPrefs.DeleteKey("JumpTargetAnchor");

        SceneManager.LoadScene(gameSceneName);
    }

    // 2. 時空跳躍開始遊戲 (按鈕要在 Inspector 填入要跳去的錨點名稱)
    public void StartJumpGame(string targetAnchorName)
    {
        Debug.Log("準備時空跳躍至：" + targetAnchorName);

        // 把玩家想去的錨點名稱存進電腦裡
        PlayerPrefs.SetString("JumpTargetAnchor", targetAnchorName);
        PlayerPrefs.Save();

        // 載入遊戲場景
        SceneManager.LoadScene(gameSceneName);
    }

    // 3. 結束遊戲功能 (你原本寫好的，非常棒！)
    public void QuitGame()
    {
        Debug.Log("執行結束遊戲指令...");

        // 這是真正打包成 EXE 或遊戲檔後，用來關閉程式的語法
        Application.Quit();

        // 讓 Unity 編輯器裡面按 Play 測試時也能退出播放
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}