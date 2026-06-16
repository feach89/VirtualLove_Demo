using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 🌟 必須加入這行，才能使用協程 (IEnumerator) 等待時間

public class TitleManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public GameObject jumpButtonObj; // 拖曳你在標題畫面做好的跳轉按鈕

    [Header("場景設定")]
    public string gameSceneName = "GameScene"; // 🌟 確認這裡是你遊戲場景的名字！
    public float delayTime = 0.5f; // 點擊後等待幾秒才換場景 (讓音效有時間播完)

    [Header("音效與喇叭綁定")]
    public AudioSource sfxPlayer; // 剛剛做的 SFX_Player 喇叭
    public AudioClip clickSound;  // 你的點擊音效檔案

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
    void Update()
    {
        //開發者專屬作弊鍵：在標題畫面按下鍵盤的 F12，就會瞬間清除所有紀錄！
        if (Input.GetKeyDown(KeyCode.F12))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("【系統提示】玩家記憶已全部清除！請重新啟動場景。");

            // 可選：如果你希望按下去之後，按鈕立刻消失給你看
            if (jumpButtonObj != null) jumpButtonObj.SetActive(false);
        }
    }

    private void PlayClickSound()
    {
        // 確保喇叭和音效檔案都有放，才執行播放
        if (sfxPlayer != null && clickSound != null)
        {
            // PlayOneShot 的好處是可以「疊加播放」，不會切斷聲音
            sfxPlayer.PlayOneShot(clickSound);
        }
    }

    // 1. 正常開始遊戲
    public void StartNormalGame()
    {
        PlayClickSound(); // 先播音效
        StartCoroutine(DelayStartNormalGame()); // 啟動倒數計時器
    }

    private IEnumerator DelayStartNormalGame()
    {
        yield return new WaitForSeconds(delayTime); // 🌟 等待 0.5 秒讓音效飛一會兒

        Debug.Log("準備從頭開始遊戲...");
        PlayerPrefs.DeleteKey("JumpTargetAnchor");
        SceneManager.LoadScene(gameSceneName);
    }

    // 2. 時空跳躍開始遊戲
    public void StartJumpGame(string targetAnchorName)
    {
        PlayClickSound(); // 先播音效
        StartCoroutine(DelayStartJumpGame(targetAnchorName)); // 啟動倒數計時器，並把錨點名字帶過去
    }

    private IEnumerator DelayStartJumpGame(string targetAnchorName)
    {
        yield return new WaitForSeconds(delayTime); // 🌟 等待 0.5 秒

        Debug.Log("準備時空跳躍至：" + targetAnchorName);
        PlayerPrefs.SetString("JumpTargetAnchor", targetAnchorName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    // 3. 結束遊戲功能
    public void QuitGame()
    {
        PlayClickSound(); // 先播音效
        StartCoroutine(DelayQuitGame()); // 啟動倒數計時器
    }

    private IEnumerator DelayQuitGame()
    {
        yield return new WaitForSeconds(delayTime); // 🌟 等待 0.5 秒

        Debug.Log("執行結束遊戲指令...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}