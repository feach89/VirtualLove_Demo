using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // 1. 開始遊戲功能 (你原本寫好的)
    public void StartGame()
    {
        Debug.Log("準備進入遊戲...");
        SceneManager.LoadScene("GameScene");
    }

    // 2. 結束遊戲功能 (新加入的)
    public void QuitGame()
    {
        Debug.Log("執行結束遊戲指令...");

        // 這是真正打包成 EXE 或遊戲檔後，用來關閉程式的語法
        Application.Quit();

        // 【貼心小提醒】Application.Quit() 在 Unity 編輯器裡面按 Play 測試時是不會有反應的！
        // 如果你想在編輯器裡也看到「退出播放」的效果，可以加上下面這三行：
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}