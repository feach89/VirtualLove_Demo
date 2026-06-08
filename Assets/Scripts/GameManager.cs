using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[System.Serializable]
public class DialogData
{
    public string eventID;          // 事件編號 (a01_dialogue)
    public string order;            // 順序 
    public string figure;           // 角色名 (玩家、琉璃、系統)
    public string dialogue;         // 對話內容
    public string background;       // 背景
    public string figure_show;      //立繪演出
    public string BGM;              //BGM
    public string sfx;              //音效
    public string vfx;              //轉場
    public string nextEvent;        //下一個事件
    public string UID;              //Debug_ID
}

public class GameManager : MonoBehaviour
{
    [Header("檔案與UI綁定")]
    public SpriteRenderer figure;        // 角色圖 (未來可用來做立繪)
    public SpriteRenderer background_imgur;    // 背景圖
    public TMP_Text nameText;            // 角色名字文本
    public TMP_Text dialogText;          // 對話內容文本

    [Header("圖片資料庫")]
    public List<Sprite> sprites = new List<Sprite>();             // 存放背景/CG的相簿
    Dictionary<string, Sprite> imageDic = new Dictionary<string, Sprite>(); // 圖片名對應

    [Header("劇本檔案庫 (多章節)")]
    // 取代了原本單一的 dialogDataFile，變成可以放多個檔案的清單
    public List<TextAsset> storyChapters = new List<TextAsset>();

    [Header("遊戲狀態")]
    public List<DialogData> dialogList = new List<DialogData>();  // 目前載入的該章節所有台詞
    public int currentChapterIndex = 0; // 書籤1：記錄目前演到第幾章
    public int currentLineIndex = 0;    // 書籤2：記錄目前演到第幾行

    private void Awake()
    {
        // 建立圖片尋找捷徑
        // 防呆機制：確保你的 sprites 裡面真的有放進 4 張圖才執行，避免遊戲一開始就崩潰
        if (sprites.Count >= 5)
        {
            imageDic["練習室"] = sprites[0];
            imageDic["CG1"] = sprites[1];
            imageDic["CG2"] = sprites[2];
            imageDic["END"] = sprites[3];
            imageDic["琉璃立繪"]= sprites[4];
        }
    }
    void Start()
    {
        // 遊戲開始時，從第 0 章開始載入
        if (storyChapters.Count > 0)
        {
            LoadChapter(currentChapterIndex);
        }
    }

    void Update()
    {
        // 偵測：如果按下「滑鼠左鍵 (0)」 或者是(||) 按下「鍵盤空白鍵 (Space)」
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            NextLine(); // 執行切換下一句的動作
        }
    }

    /// <summary>
    /// 載入指定章節的劇本
    /// </summary>
    public void LoadChapter(int chapterIndex)
    {
        ReadText(storyChapters[chapterIndex]);

        // 讀取完後，從第一句話開始播
        if (dialogList.Count > 0)
        {
            currentLineIndex = 0;
            PlayCurrentLine(); // 呼叫播放功能
        }
    }

    /// <summary>
    /// 切換到下一行對話
    /// </summary>
    public void NextLine()
    {
        currentLineIndex++; // 書籤往下一行

        if (currentLineIndex < dialogList.Count)
        {
            // 還沒播完，繼續播下一句
            PlayCurrentLine();
        }
        else
        {
            // 這個檔案播完了！準備切換到下一個檔案
            currentChapterIndex++;

            if (currentChapterIndex < storyChapters.Count)
            {
                Debug.Log("正在切換到下一個章節...");
                LoadChapter(currentChapterIndex);
            }
            else
            {
                Debug.Log("所有的劇本都已經播放完畢！遊戲結束！");

                // 【新增這行】直接讀取名為 TitleScene 的場景
                SceneManager.LoadScene("TitleScene");
            }
        }
    }

    /// <summary>
    /// 【新增】播放目前書籤指定的這一行台詞 (自動處理換字和換圖)
    /// </summary>
    private void PlayCurrentLine()
    {
        DialogData currentLine = dialogList[currentLineIndex];

        // 1. 更新對話
        UpdateText(currentLine.figure, currentLine.dialogue);

        // 2. 處理背景欄位
        if (!string.IsNullOrEmpty(currentLine.background))
        {
            UpdateBackground(currentLine.background, currentLine.UID);

            background_imgur.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        }

        // 3. 處理立繪控制 (使用 figure_show 欄位)
        if (!string.IsNullOrEmpty(currentLine.figure_show))
        {
            if (currentLine.figure_show == "None")
            {
                figure.sprite = null; // 強制隱藏
            }
            else
            {
                // 呼叫下方我們定義的查找函數
                figure.sprite = GetSpriteByName(currentLine.figure_show);
            }
        }

        // 4. 判斷音效
        if (!string.IsNullOrEmpty(currentLine.sfx))
        {
            PlaySound(currentLine.sfx);
        }

        // 5. 判斷轉場
        if (!string.IsNullOrEmpty(currentLine.vfx))
        {
            PlayTransition(currentLine.vfx);
        }

        // 6. 判斷 BGM
        if (!string.IsNullOrEmpty(currentLine.BGM))
        {
            PlaySound(currentLine.BGM);
        }
    }

    /// <summary>
    /// 從圖庫字典中找圖片的捷徑函數
    /// </summary>
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
    /// <summary>
    /// 播放音效的功能 (這裡先寫個骨架，稍後可以補上真正的音效程式)
    /// </summary>
    public void PlaySound(string soundName)
    {
        Debug.Log("🎵 播放音效：" + soundName);
        // 未來這裡會寫：用 AudioSource 播放對應的聲音檔案
    }

    /// <summary>
    /// 播放轉場的功能
    /// </summary>
    public void PlayTransition(string transitionName)
    {
        Debug.Log("✨ 觸發轉場特效：" + transitionName);
        // 未來這裡會寫：呼叫 Unity 動畫系統，讓畫面變黑或震動
    }

    /// <summary>
    /// 更新對話文本
    /// </summary>
    public void UpdateText(string _name, string _text)
    {
        nameText.text = _name;
        dialogText.text = _text;
    }

    /// <summary>
    /// 更新背景圖片
    /// </summary>
    public void UpdateBackground(string _bgName, string currentUID)
    {
        if (imageDic.ContainsKey(_bgName))
        {
            background_imgur.sprite = imageDic[_bgName];
        }
        else
        {
            // 只要圖片找不到，Unity 就會大喊：「UID: xxx 的背景找不到啦！」
            Debug.LogWarning("【警告】UID: " + currentUID + " 找不到背景圖片：" + _bgName);
        }
    }
    /// <summary>
    /// 讀取 CSV 劇本資料
    /// </summary>
    public void ReadText(TextAsset _textAsset)
    {
        dialogList.Clear();
        string[] rows = _textAsset.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            string[] cells = rows[i].Split(',');
            if (cells.Length < 11) continue;

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

            dialogList.Add(lineData);
        }
        Debug.Log("章節讀取完成！行數：" + dialogList.Count);
    }
}