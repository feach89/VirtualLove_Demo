using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class DialogData
{
    public string eventID;          // 事件編號 (a01_dialogue)
    public string order;            // 順序 
    public string figure;           // 角色名 (玩家、琉璃、系統)
    public string dialogue;         // 對話內容
    public string background;       // 背景
    public string figure_show;      // 立繪演出
    public string BGM;              // BGM
    public string sfx;              // 音效
    public string vfx;              // 轉場
    public string nextEvent;        // 下一個事件
    public string UID;              // Debug_ID
    public string Anchor;           // 錨點
}

public class GameManager : MonoBehaviour
{
    [Header("檔案與UI綁定")]
    public SpriteRenderer figure;        // 角色圖 (未來可用來做立繪)
    public SpriteRenderer background_imgur;    // 背景圖
    public TMP_Text nameText;            // 角色名字文本
    public TMP_Text dialogText;          // 對話內容文本
    public GameObject uiPanel;          // 用來控制整個對話框 UI 的總開關！
    public GameObject clickPrompt;       //用來放「點擊跳轉」的提示文字物件

    [Header("圖片資料庫")]
    public List<Sprite> sprites = new List<Sprite>();             // 存放背景/CG的相簿
    Dictionary<string, Sprite> imageDic = new Dictionary<string, Sprite>(); // 圖片名對應

    [Header("劇本檔案庫 (多章節)")]
    public List<TextAsset> storyChapters = new List<TextAsset>();

    [Header("遊戲狀態")]
    public List<DialogData> dialogList = new List<DialogData>();  // 目前載入的該章節所有台詞
    public int currentChapterIndex = 0; // 書籤1：記錄目前演到第幾章
    public int currentLineIndex = 0;    // 書籤2：記錄目前演到第幾行

    [Header("閃爍設定")]
    public float blinkSpeed = 2.0f;            // 閃爍速度，數值越大越快

    // 👇 補上這兩個結局控制變數！
    public bool isEnding = false;        // 判斷是否已經進入 END 畫面
    public bool canClickToTitle = false; // 判斷 5 秒是否已經過了

    private void Awake()
    {
        // 建立圖片尋找捷徑
        if (sprites.Count >= 9)
        {
            imageDic["練習室"] = sprites[0];
            imageDic["CG1"] = sprites[1];
            imageDic["CG2"] = sprites[2];
            imageDic["END1"] = sprites[3];
            imageDic["END2"] = sprites[4];
            imageDic["黑背景"] = sprites[5];
            imageDic["琉璃立繪"] = sprites[6];
            imageDic["琉璃立繪2"] = sprites[7];
            imageDic["琉璃立繪3"] = sprites[8];
        }
    }

    void Start()
    {
        if (storyChapters.Count > 0)
        {
            LoadChapter(currentChapterIndex);
        }
    }

    void Update()
    {
        // 偵測滑鼠或鍵盤點擊
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            // 攔截邏輯：如果是結局狀態，就看 5 秒到了沒；如果不是，就正常播下一句
            if (isEnding)
            {
                if (canClickToTitle)
                {
                    SceneManager.LoadScene("TitleScene"); // 5秒後點擊回標題
                }
            }
            else
            {
                NextLine();
            }
        }

        // 🌟 閃爍特效邏輯 (同時支援 TMP 與舊版 Text)
        if (isEnding && canClickToTitle && clickPrompt != null && clickPrompt.activeSelf)
        {
            // 利用 Sin 函數讓 Alpha 值在 0 到 1 之間來回變動
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));

            // 嘗試抓取舊版 Unity Text 元件
            Text legacyText = clickPrompt.GetComponent<Text>();
            if (legacyText != null)
            {
                Color c = legacyText.color;
                c.a = alpha;
                legacyText.color = c;
            }

            // 嘗試抓取新版 TextMeshPro 元件
            TMP_Text tmpText = clickPrompt.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                Color c = tmpText.color;
                c.a = alpha;
                tmpText.color = c;
            }
        }
    }

    public void LoadChapter(int chapterIndex)
    {
        ReadText(storyChapters[chapterIndex]);

        if (dialogList.Count > 0)
        {
            currentLineIndex = 0;
            PlayCurrentLine();
        }
    }

    public void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogList.Count)
        {
            PlayCurrentLine();
        }
        else
        {
            currentChapterIndex++;

            if (currentChapterIndex < storyChapters.Count)
            {
                Debug.Log("正在切換到下一個章節...");
                LoadChapter(currentChapterIndex);
            }
            else
            {
                Debug.Log("所有的劇本都已經播放完畢！準備顯示結局畫面。");
                // 👇 補上呼叫結局的邏輯！(這裡先預設播放 END1 圖片，你之後可以改成判斷式)
                StartCoroutine(ShowEndScreen("END1"));
            }
        }
    }

    private void PlayCurrentLine()
    {
        DialogData currentLine = dialogList[currentLineIndex];

        UpdateText(currentLine.figure, currentLine.dialogue);

        if (!string.IsNullOrEmpty(currentLine.background))
        {
            UpdateBackground(currentLine.background, currentLine.UID);
            background_imgur.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        }

        if (!string.IsNullOrEmpty(currentLine.figure_show))
        {
            if (currentLine.figure_show == "None")
            {
                figure.sprite = null;
            }
            else
            {
                figure.sprite = GetSpriteByName(currentLine.figure_show);
            }
        }

        if (!string.IsNullOrEmpty(currentLine.sfx))
        {
            PlaySound(currentLine.sfx);
        }

        if (!string.IsNullOrEmpty(currentLine.vfx))
        {
            PlayTransition(currentLine.vfx);
        }

        if (!string.IsNullOrEmpty(currentLine.BGM))
        {
            PlaySound(currentLine.BGM);
        }
    }

    public Sprite GetSpriteByName(string _name)
    {
        if (imageDic.ContainsKey(_name))
        {
            return imageDic[_name];
        }
        else
        {
            Debug.LogWarning("找不到圖片：" + _name);
            return null;
        }
    }

    public void PlaySound(string soundName)
    {
        Debug.Log("🎵 播放音效：" + soundName);
    }

    public void PlayTransition(string transitionName)
    {
        Debug.Log("✨ 觸發轉場特效：" + transitionName);
    }

    public void UpdateText(string _name, string _text)
    {
        nameText.text = _name;
        dialogText.text = _text;
    }

    public void UpdateBackground(string _bgName, string currentUID)
    {
        if (imageDic.ContainsKey(_bgName))
        {
            background_imgur.sprite = imageDic[_bgName];
        }
        else
        {
            Debug.LogWarning("【警告】UID: " + currentUID + " 找不到背景圖片：" + _bgName);
        }
    }

    public void ReadText(TextAsset _textAsset)
    {
        dialogList.Clear();
        string[] rows = _textAsset.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            string[] cells = rows[i].Split(',');
            if (cells.Length < 12) continue; // 這裡你改得很好！變成 12 了

            DialogData lineData = new DialogData();
            lineData.eventID = cells[0];
            lineData.order = cells[1];
            lineData.figure = cells[2];
            lineData.dialogue = cells[3].Replace("\"", "");
            lineData.background = cells[4];
            lineData.figure_show = cells[5];
            lineData.BGM = cells[6];
            lineData.sfx = cells[7];
            lineData.vfx = cells[8];
            lineData.nextEvent = cells[9];
            lineData.UID = cells[10];
            lineData.Anchor = cells[11]; // 新增的 Anchor 完美讀取

            dialogList.Add(lineData);
        }
        Debug.Log("章節讀取完成！行數：" + dialogList.Count);
    }

    /// <summary>
    /// 處理 END 畫面與 5 秒計時 (支援多結局)
    /// </summary>
    private System.Collections.IEnumerator ShowEndScreen(string endImageName)
    {
        isEnding = true;
        canClickToTitle = false;

        background_imgur.sprite = GetSpriteByName(endImageName);
        figure.sprite = null;
        UpdateText("", "");

        // 隱藏原本的對話框
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // 🌟 確保一進入結局時，提示文字是隱藏的
        if (clickPrompt != null)
        {
            clickPrompt.SetActive(false);
        }

        // 等待 5 秒
        yield return new WaitForSeconds(5f);

        // 5 秒結束，解鎖點擊
        canClickToTitle = true;
        Debug.Log("5秒已過，現在點擊可以回到標題畫面了！");

        // 🌟 5 秒到了，把提示文字顯示出來！
        if (clickPrompt != null)
        {
            clickPrompt.SetActive(true);
        }
    }

}