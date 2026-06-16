    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI; // 支援舊版 Text 與 Button 組件


    [System.Serializable]
    public class DialogData
    {
        public string eventID;          // 事件編號 (a01_dialogue)
        public string order;            // 順序 
        public string figure;           // 角色名 (玩家、琉璃、系統、選項)
        public string dialogue;         // 對話內容 / 選項文字
        public string background;       // 背景
        public string figure_show;      // 立繪演出
        public string BGM;              // BGM
        public string sfx;              // 音效
        public string vfx;              // 轉場
        public string nextEvent;        // 下一個事件 / 分歧跳轉目標
        public string UID;              // Debug_ID
        public string Anchor;           // 錨點
    }

    public class GameManager : MonoBehaviour
    {
        [Header("檔案與UI綁定")]
        public SpriteRenderer figure;              // 角色圖
        public SpriteRenderer background_imgur;    // 背景圖
        public TMP_Text nameText;                  // 角色名字文本
        public TMP_Text dialogText;                // 對話內容文本
        public GameObject uiPanel;                 // 用來控制整個對話框 UI 的總開關
        public GameObject clickPrompt;             // 用來放「點擊跳轉」的提示文字物件
        public GameObject Divergent_texts;         // 用來放「選項提示」的提示文字物件

        [Header("選項UI綁定")]
        public GameObject choicePanel;             // 選項視窗的總開關 (預設設為關閉)
        public List<Button> choiceButtons;         // 畫面上的選項按鈕清單 (可拉 2~3 個預設按鈕)

        [Header("閃爍設定")]
        public float blinkSpeed = 3.0f;            // 閃爍速度

        [Header("圖片資料庫")]
        public List<Sprite> sprites = new List<Sprite>();
        Dictionary<string, Sprite> imageDic = new Dictionary<string, Sprite>();

        [Header("劇本檔案庫")]
        public List<TextAsset> storyChapters = new List<TextAsset>();
        // 🌟 新增：用來透過「檔案名稱」快速尋找劇本的字典
        private Dictionary<string, TextAsset> chapterDic = new Dictionary<string, TextAsset>();

        [Header("遊戲狀態")]
        public List<DialogData> dialogList = new List<DialogData>();
        public int currentLineIndex = 0;    // 記錄目前演到第幾行

        public bool isEnding = false;
        public bool canClickToTitle = false;
        private bool isSelectingChoice = false; // 🌟 新增：是否正在選擇分歧選項
        public bool isTransitioning = false; // 是否正在播放轉場動畫
        public bool isStartingSequence = false; // 標記目前是否正在播開場動畫

        [Header("轉場特效綁定")]
        public Image transitionPanel; // 拖曳剛剛做的全螢幕 Image 放到這裡

        [Header("前導設定")]
        public GameObject prologueUI;         // 🌟 注意這裡變成了 GameObject！
        public float prologueWaitTime = 2.0f; // 前導圖片要在畫面上停留幾秒

        [Header("音樂庫與喇叭綁定")]
        public AudioSource bgmPlayer; // 專門播背景音樂的喇叭
        public List<AudioClip> bgmClips = new List<AudioClip>(); // 讓你拖曳音樂檔案進來的清單
        private Dictionary<string, AudioClip> bgmDic = new Dictionary<string, AudioClip>(); // 讓程式碼用名字找音樂的字典

        [Header("循環音效與喇叭綁定")]
        public AudioSource sfxPlayer; // 專門播循環音效的喇叭 (例如雨聲)
        public List<AudioClip> sfxClips = new List<AudioClip>(); // 拖曳音效檔案進來的清單
        private Dictionary<string, AudioClip> sfxDic = new Dictionary<string, AudioClip>(); // 用名字找音效的字典

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

            // 🌟 自動將 storyChapters 清單轉換為名稱字典，方便後續跳轉
            foreach (var chapter in storyChapters)
            {
                if (chapter != null && !chapterDic.ContainsKey(chapter.name))
                {
                    chapterDic[chapter.name] = chapter;
                }
            }
            // 🌟 自動將 bgmClips 清單轉換為名稱字典，方便後續透過 CSV 名字播放
            foreach (var clip in bgmClips)
            {
                if (clip != null && !bgmDic.ContainsKey(clip.name))
                {
                bgmDic[clip.name] = clip;
                }
            }
            // 🌟 自動將 sfxClips 清單轉換為名稱字典
            foreach (var clip in sfxClips)
            {
                if (clip != null && !sfxDic.ContainsKey(clip.name))
                {
                sfxDic[clip.name] = clip;
                }
            }

    }

    void Start()
    {
        // 啟動全新的「前導與開場演出」協程
        StartCoroutine(GameStartSequence());
    }

    // 🌟 修正版：開場專屬的演出時間軸（支援跳過前導圖）
    private System.Collections.IEnumerator GameStartSequence()
    {
        isTransitioning = true;
        isStartingSequence = true; // 鎖定劇本特效

        // 1. 強制隱藏對話框，黑布瞬間拉上 (畫面維持全黑)
        if (uiPanel != null) uiPanel.SetActive(false);
        transitionPanel.gameObject.SetActive(true);
        transitionPanel.color = Color.black;

        // 🌟 【關鍵修改】提早去檢查電腦記憶，看看這次是不是「時空跳躍」
        string targetAnchor = PlayerPrefs.GetString("JumpTargetAnchor", "");

        // 2. 判斷要不要執行前導演出
        // 條件：必須有綁定物件，而且這次「不是時空跳躍（targetAnchor 是空的）」才播前導圖！
        if (prologueUI != null && string.IsNullOrEmpty(targetAnchor))
        {
            // 確保前導圖開啟
            prologueUI.SetActive(true);

            // 先把背景畫布隱藏，確保前導圖的背後是純黑的
            if (background_imgur != null) background_imgur.gameObject.SetActive(false);
            if (figure != null) figure.sprite = null;

            // 黑布慢慢拉開 (淡出)，顯示出你排好版的前導圖
            yield return StartCoroutine(FadeTransition(0f, 1.0f));

            // 停留設定的秒數
            yield return new WaitForSeconds(prologueWaitTime);

            // 黑布再度拉上 (淡入)
            yield return StartCoroutine(FadeTransition(1f, 1.0f));

            // 前導圖功成身退，把它關掉
            prologueUI.SetActive(false);

            // 把背景畫布重新打開，準備迎接正式遊戲
            if (background_imgur != null) background_imgur.gameObject.SetActive(true);
        }
        else
        {
            // 🌟 防呆：如果是時空跳躍，確保前導圖是關閉的，且正常的背景畫布是開啟的
            if (prologueUI != null) prologueUI.SetActive(false);
            if (background_imgur != null) background_imgur.gameObject.SetActive(true);

            // 讓畫面在全黑狀態下稍微停頓 0.5 秒，換場視覺感會更流暢
            yield return new WaitForSeconds(0.5f);
        }

        // 3. 正式讀取劇本指令 (判斷跳躍或新遊戲)
        if (!string.IsNullOrEmpty(targetAnchor))
        {
            PlayerPrefs.DeleteKey("JumpTargetAnchor");
            ExecuteAnchorJump(targetAnchor); // 在全黑的幕後偷偷把舞台和對話準備好
        }
        else
        {
            if (storyChapters.Count > 0 && storyChapters[0] != null)
            {
                LoadChapter(storyChapters[0].name);
            }
        }

        // 4. 在錨點舞台都擺好後，黑布慢慢拉開，並把對話框正式顯示出來！
        if (uiPanel != null) uiPanel.SetActive(true);
        yield return StartCoroutine(FadeTransition(0f, 1.0f));

        transitionPanel.gameObject.SetActive(false); // 徹底關閉黑布
        isTransitioning = false;
        isStartingSequence = false; // 解鎖劇本特效！
    }

    // 專門給開場用的小幫手：平滑控制黑布的透明度
    private System.Collections.IEnumerator FadeTransition(float targetAlpha, float duration)
    {
        float startAlpha = transitionPanel.color.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Mathf.Lerp 可以平滑地在兩個數字之間推移
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            transitionPanel.color = new Color(0f, 0f, 0f, currentAlpha);
            yield return null;
        }
        transitionPanel.color = new Color(0f, 0f, 0f, targetAlpha); // 確保最終數值精準
    }

    /// <summary>
    /// 🌟 全新新增：跨章節搜尋錨點並空降
    /// </summary>
    private void ExecuteAnchorJump(string targetAnchor)
    {
        // 掃描劇本檔案庫裡的所有章節
        foreach (var chapter in storyChapters)
        {
            ReadText(chapter); // 先把這個章節讀進 dialogList 裡看看

            for (int i = 0; i < dialogList.Count; i++)
            {
                if (dialogList[i].Anchor == targetAnchor)
                {
                    Debug.Log("✅ 找到錨點了！位於章節 [" + chapter.name + "] 的第 " + i + " 行");
                    currentLineIndex = i; // 插上書籤
                    PlayCurrentLine();    // 開始播放
                    return; // 任務完成，直接結束函式
                }
            }
        }

        // 如果全部章節都找完了還是沒看到該錨點
        Debug.LogError("【跳轉失敗】在所有劇本中都找不到名為 [" + targetAnchor + "] 的錨點！");

        // 防呆：找不到就從頭開始播
        if (storyChapters.Count > 0) LoadChapter(storyChapters[0].name);
    }

    void Update()
    {
        // 如果在選分歧選項中，或【正在轉場中】，攔截點擊！
        if (isSelectingChoice || isTransitioning)
        {
            if (isSelectingChoice) ApplyBlinkEffect(Divergent_texts);
            return;
        }

        // 2. 結局點擊跳轉 (原本的邏輯)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isEnding && canClickToTitle)
            {
                SceneManager.LoadScene("TitleScene");
            }
            else if (!isEnding)
            {
                NextLine();
            }
        }

        // 3. 結局提示閃爍
        if (isEnding && canClickToTitle)
        {
            ApplyBlinkEffect(clickPrompt);
        }
    }

    /// <summary>
    /// 🌟 升級：現在支援直接用「檔案名稱 (string)」載入指定章節劇本
    /// </summary>
    public void LoadChapter(string chapterName)
        {
            if (chapterDic.ContainsKey(chapterName))
            {
                ReadText(chapterDic[chapterName]);

                if (dialogList.Count > 0)
                {
                    currentLineIndex = 0;
                    PlayCurrentLine();
                }
            }
            else
            {
                Debug.LogError("【錯誤】在劇本檔案庫中找不到名為 " + chapterName + " 的 CSV 檔案！");
            }
        }

        public void NextLine()
        {
            // 1. 檢查「剛剛播完的這句話」，它的 nextEvent 欄位有沒有填寫東西？
            string jumpTarget = dialogList[currentLineIndex].nextEvent;

            // 🌟【關鍵新增】結局攔截機制：如果 nextEvent 填的是 END1 或 END2，直接導向對應的結局圖！
            if (jumpTarget == "END1" || jumpTarget == "END2")
            {
                Debug.Log("劇本偵測到結局指令，準備顯示：" + jumpTarget);
                StartCoroutine(ShowEndScreen(jumpTarget));
                return; // 中斷程式，直接進入結局畫面
            }

            // 2. 如果有填寫跳轉目標 (平時的分歧跳轉)
            if (!string.IsNullOrEmpty(jumpTarget))
            {
                // 情況 A：找找看目前的 CSV 裡，有沒有這個 eventID？
                for (int i = 0; i < dialogList.Count; i++)
                {
                    if (dialogList[i].eventID == jumpTarget)
                    {
                        currentLineIndex = i;
                        PlayCurrentLine();
                        return; // 找到了，跳轉成功，結束這回合
                    }
                }

                // 情況 B：如果同一個檔案裡面找不到，代表要換「下一個 CSV 章節檔案」
                if (chapterDic.ContainsKey(jumpTarget))
                {
                    Debug.Log("本章結束，跳轉至新章節檔案：" + jumpTarget);
                    LoadChapter(jumpTarget);
                    return;
                }
            }

            // 3. 如果 nextEvent 是空的，就乖乖前往「下一行」
            currentLineIndex++;

            if (currentLineIndex < dialogList.Count)
            {
                PlayCurrentLine();
            }
            else
            {
                // 防呆機制：如果整份 CSV 播到最後一行都沒設定結局，就預設播 END1
                Debug.Log("所有的劇本都已經播放完畢！預設顯示 END1 畫面。");
                StartCoroutine(ShowEndScreen("END1"));
            }
        }

    private void PlayCurrentLine()
    {
        DialogData currentLine = dialogList[currentLineIndex];

        if (currentLine.figure == "選項")
        {
            // 讀取 BGM、音效、背景、立繪
            ApplyEnvironmentData(currentLine);

            // 🌟【完美修復】自動往上看一行，把上一行的對話內容顯示在對話框裡！
            if (currentLineIndex > 0)
            {
                DialogData prevLine = dialogList[currentLineIndex - 1];
                // 抓取上一行的角色名與對話，貼到畫面上
                UpdateText(prevLine.figure, prevLine.dialogue);
            }
            else
            {
                // 萬一選項剛好在整個檔案的第 0 行 (防呆)
                UpdateText("", "");
            }

            SetupChoiceBranch();
            return;
        }

        // 判斷這行劇本有沒有寫轉場特效 (vfx)
        if (!string.IsNullOrEmpty(currentLine.vfx))
        {
            // 有特效！啟動轉場協程
            StartCoroutine(HandleTransitionLine(currentLine));
        }
        else
        {
            // 沒有特效，維持原本的瞬間切換
            ApplyLineData(currentLine);
        }
    }

    // 🌟【全新新增】專門處理畫面與聲音的獨立小幫手
    private void ApplyEnvironmentData(DialogData currentLine)
    {
        // 1. 處理背景
        if (!string.IsNullOrEmpty(currentLine.background))
        {
            UpdateBackground(currentLine.background, currentLine.UID);
            background_imgur.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        }
        // 2. 處理立繪
        if (!string.IsNullOrEmpty(currentLine.figure_show))
        {
            if (currentLine.figure_show == "None")
                figure.sprite = null;
            else
                figure.sprite = GetSpriteByName(currentLine.figure_show);
        }
        // 3. 處理音樂與音效
        if (!string.IsNullOrEmpty(currentLine.BGM)) { PlayBGM(currentLine.BGM); }
        if (!string.IsNullOrEmpty(currentLine.sfx)) { PlaySFX(currentLine.sfx); }
    }

    // 🌟 將原本的 ApplyLineData 瘦身，讓它呼叫上面的小幫手
    private void ApplyLineData(DialogData currentLine)
    {
        // 更新對話文字
        UpdateText(currentLine.figure, currentLine.dialogue);

        // 呼叫小幫手更新畫面與聲音
        ApplyEnvironmentData(currentLine);
    }
    // 🌟 全新改良：精確辨識且支援「連續閃爍」的轉場協程
    private System.Collections.IEnumerator HandleTransitionLine(DialogData currentLine)
    {
        isTransitioning = true; // 鎖住玩家點擊

        // 先暫時隱藏對話框 UI
        if (uiPanel != null) uiPanel.SetActive(false);
        transitionPanel.gameObject.SetActive(true);

        if (currentLine.vfx == "白屏閃現")
        {
            float flashSpeed = 0.25f; // 閃爍的速度，數字越小閃越快

            for (int i = 0; i < 3; i++) // 執行 3 次的迴圈
            {
                // 快速變白
                float timer = 0f;
                while (timer < flashSpeed)
                {
                    timer += Time.deltaTime;
                    transitionPanel.color = new Color(1f, 1f, 1f, timer / flashSpeed);
                    yield return null;
                }
                transitionPanel.color = Color.white;

                // 🌟 關鍵：只有在「最後一次(第3次)」閃白的時候，才偷偷換掉背景跟立繪！
                if (i == 2) ApplyLineData(currentLine);

                // 快速變回透明
                timer = 0f;
                while (timer < flashSpeed)
                {
                    timer += Time.deltaTime;
                    transitionPanel.color = new Color(1f, 1f, 1f, 1f - (timer / flashSpeed));
                    yield return null;
                }
                transitionPanel.color = new Color(1f, 1f, 1f, 0f);

                // 每次閃爍中間稍微停頓一極小段時間，讓節奏感更強烈
                if (i < 2) yield return new WaitForSeconds(0.05f);
            }
        }
        else if (currentLine.vfx == "黑屏轉場")
        {
            float fadeSpeed = 1.5f; // 黑屏速度

            // 黑布慢慢拉上
            float timer = 0f;
            while (timer < fadeSpeed)
            {
                timer += Time.deltaTime;
                transitionPanel.color = new Color(0f, 0f, 0f, timer / fadeSpeed);
                yield return null;
            }
            transitionPanel.color = Color.black;

            // 趁全黑時偷偷換素材
            ApplyLineData(currentLine);

            // 停頓一下讓玩家呼吸
            yield return new WaitForSeconds(0.2f);

            // 黑布慢慢拉開
            timer = 0f;
            while (timer < fadeSpeed)
            {
                timer += Time.deltaTime;
                transitionPanel.color = new Color(0f, 0f, 0f, 1f - (timer / fadeSpeed));
                yield return null;
            }
            transitionPanel.color = new Color(0f, 0f, 0f, 0f);
        }
        else
        {
            // 如果 CSV 的 vfx 欄位寫了看不懂的字，就放棄特效，直接瞬間切換
            ApplyLineData(currentLine);
        }

        // 轉場徹底結束，把黑布關掉，重新顯示對話框 UI
        transitionPanel.gameObject.SetActive(false);
        if (uiPanel != null) uiPanel.SetActive(true);

        isTransitioning = false; // 解除點擊鎖定
    }

    /// <summary>
    /// 🌟 新增：掃描連續選項並動態設定 UI 按鈕
    /// </summary>
    private void SetupChoiceBranch()
        {
            isSelectingChoice = true;

            if (choicePanel != null) choicePanel.SetActive(true);
        // 🌟 核心：確保這段閃爍文字被開啟
        if (Divergent_texts != null)
        {
            Divergent_texts.SetActive(true);
        }

        // 先把畫面上所有的預設選項按鈕隱藏
        foreach (var btn in choiceButtons)
            {
                if (btn != null) btn.gameObject.SetActive(false);
            }

            int buttonIndex = 0;

            // 從目前的 currentLineIndex 開始往後掃描，找出所有連續為「選項」的資料列
            for (int i = currentLineIndex; i < dialogList.Count; i++)
            {
                if (dialogList[i].figure == "選項")
                {
                    // 防呆：如果 CSV 寫的選項數量超過了你場景中預設的按鈕上限，就跳出
                    if (buttonIndex >= choiceButtons.Count) break;

                    Button currentBtn = choiceButtons[buttonIndex];
                    if (currentBtn != null)
                    {
                        currentBtn.gameObject.SetActive(true);

                        // 1. 設定按鈕顯示的文字 (抓取 dialogue 欄位)
                        TMP_Text btnText = currentBtn.GetComponentInChildren<TMP_Text>();
                        if (btnText != null)
                        {
                            btnText.text = dialogList[i].dialogue;
                        }
                        else
                        {
                            // 兼容傳統 Text
                            Text legacyBtnText = currentBtn.GetComponentInChildren<Text>();
                            if (legacyBtnText != null) legacyBtnText.text = dialogList[i].dialogue;
                        }

                        // 2. 動態綁定點擊事件
                        string targetRoute = dialogList[i].nextEvent; // 紀錄要跳轉的劇本名 (例如 a07, a08)
                        currentBtn.onClick.RemoveAllListeners();     // 清除舊的監聽
                        currentBtn.onClick.AddListener(() => OnChoiceClicked(targetRoute));

                        buttonIndex++;
                    }
                }
                else
                {
                    // 一旦遇到不是「選項」的行數，代表這一組分歧選項結束了
                    break;
                }
            }
        }

        /// <summary>
        /// 當玩家點擊選項按鈕時觸發 (同檔案內跳轉)
        /// </summary>
        public void OnChoiceClicked(string targetID)
        {
            Debug.Log("玩家選擇了選項，準備跳轉至事件：" + targetID);

            if (choicePanel != null) choicePanel.SetActive(false);
            isSelectingChoice = false;

        // 🌟 關閉閃爍文字
        if (Divergent_texts != null)
        {
            Divergent_texts.SetActive(false);
        }

        isSelectingChoice = false;

        // 🌟 核心修改：在「目前的劇本清單(dialogList)」裡面，尋找對應的 eventID
        for (int i = 0; i < dialogList.Count; i++)
            {
                // 假設你的目標名稱 (例如 a07) 是寫在 CSV 的第 0 欄 (eventID)
                if (dialogList[i].eventID == targetID)
                {
                    currentLineIndex = i; // 把目前的行數切換到找到的那一行
                    PlayCurrentLine();    // 開始播放！
                    return;               // 任務完成，提早結束函式
                }
            }

            // 如果迴圈找完了都沒找到
            Debug.LogError("【跳轉失敗】在目前的劇本裡，找不到 eventID 為 [" + targetID + "] 的台詞！");
        }

        public Sprite GetSpriteByName(string _name)
        {
            if (imageDic.ContainsKey(_name)) return imageDic[_name];
            Debug.LogWarning("找不到圖片：" + _name);
            return null;
        }

    /// <summary>
    /// 🌟 讀取 CSV 指令並播放 BGM
    /// </summary>
    public void PlayBGM(string bgmName)
    {
        // 1. 如果劇本寫了「Stop」或「無」，就乖乖閉嘴
        if (bgmName == "Stop" || bgmName == "無")
        {
            bgmPlayer.Stop();
            bgmPlayer.clip = null;
            return;
        }

        // 2. 檢查我們的音樂庫裡面有沒有這首歌？
        if (bgmDic.ContainsKey(bgmName))
        {
            AudioClip targetClip = bgmDic[bgmName];

            // 🌟 超級關鍵防呆：如果現在正在播的歌，就是劇本要求播的歌，那就「什麼都不要做」！
            // 這樣才能確保玩家按下一句對話時，音樂不會一直從頭開始跳針
            if (bgmPlayer.clip == targetClip && bgmPlayer.isPlaying) return;

            // 換上新唱片，開始播放！
            bgmPlayer.clip = targetClip;
            bgmPlayer.Play();
        }
        else
        {
            Debug.LogWarning("【警告】找不到BGM音樂檔案：" + bgmName + "，請檢查檔案名稱與 CSV 填寫是否一致！");
        }
    }
    /// <summary>
    /// 🌟 讀取 CSV 指令並播放循環音效 (SFX)
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        // 1. 如果劇本寫了「Stop」或「無」，就停止音效
        if (sfxName == "Stop" || sfxName == "無")
        {
            sfxPlayer.Stop();
            sfxPlayer.clip = null;
            return;
        }

        // 2. 檢查音效庫裡面有沒有這個檔案？
        if (sfxDic.ContainsKey(sfxName))
        {
            AudioClip targetClip = sfxDic[sfxName];

            // 🌟 防呆：如果同一種音效正在播，就不重頭開始 (例如雨聲一直下)
            if (sfxPlayer.clip == targetClip && sfxPlayer.isPlaying) return;

            // 換上新音效，開始播放！
            sfxPlayer.clip = targetClip;
            sfxPlayer.Play();
        }
        else
        {
            Debug.LogWarning("【警告】找不到 SFX 音效檔案：" + sfxName + "，請檢查檔案名稱與 CSV 填寫是否一致！");
        }
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
                if (cells.Length < 12) continue;

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
                lineData.Anchor = cells[11];

                dialogList.Add(lineData);
            }
            Debug.Log("劇本檔案 [" + _textAsset.name + "] 讀取完成！行數：" + dialogList.Count);
        }

        private System.Collections.IEnumerator ShowEndScreen(string endImageName)
        {
            isEnding = true;
            canClickToTitle = false;

            background_imgur.sprite = GetSpriteByName(endImageName);
            figure.sprite = null;
            UpdateText("", "");

            if (uiPanel != null) uiPanel.SetActive(false);
            if (clickPrompt != null) clickPrompt.SetActive(false);

            yield return new WaitForSeconds(3f);

            canClickToTitle = true;
            Debug.Log("3秒已過，現在點擊可以回到標題畫面了！");

            if (clickPrompt != null) clickPrompt.SetActive(true);

        // 🌟 進入結局時，永久寫入通關標記到玩家電腦裡！
        PlayerPrefs.SetInt("HasClearedEnding", 1);
        PlayerPrefs.Save();

        yield return new WaitForSeconds(5f);

        canClickToTitle = true;
        Debug.Log("5秒已過，現在點擊可以回到標題畫面了！");

        if (clickPrompt != null) clickPrompt.SetActive(true);
    }
        /// <summary>
        /// 🌟 讓任何指定的文字物件產生呼吸閃爍特效
        /// </summary>
        private void ApplyBlinkEffect(GameObject textObj)
        {
            // 防呆：如果沒放物件，或者物件目前是隱藏狀態，就直接跳出不執行
            if (textObj == null || !textObj.activeSelf) return;

            // 計算 0 到 1 之間的呼吸數值
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));

            // 支援舊版 Text
            Text legacyText = textObj.GetComponent<Text>();
            if (legacyText != null)
            {
                Color c = legacyText.color;
                c.a = alpha;
                legacyText.color = c;
            }

            // 支援 TextMeshPro
            TMP_Text tmpText = textObj.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                Color c = tmpText.color;
                c.a = alpha;
                tmpText.color = c;
            }
        }
    }