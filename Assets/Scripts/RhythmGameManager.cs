using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class RhythmGameManager : MonoBehaviour
{
    private bool hasMusicStarted = false; // 音乐是否真正开始过
    public GameObject resultPanel; // 【新增】结算面板
    public CameraDirector cameraDirectorScript;

    [Header("Audio Effects")]
    public AudioClip levelUpSound;
    public AudioClip hitSound;
    public AudioSource hitSfxSource; // 用于播放音效的 AudioSource
    public AudioSource levelUpSfxSource; // 用于播放音乐的 AudioSource

    [Header("UI Panels")]
    public GameObject mainMenuUI;
    public GameObject gameplayHUD;

    [Header("UI Components")]
    public GameObject pausePanel;
    public TextMeshProUGUI scoreUIText;
    public TextMeshProUGUI comboUIText;
    public TextMeshProUGUI levelUIText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI maxComboText;
    public TextMeshProUGUI finalScoreValue; // 拖入 Score_Value
    public TextMeshProUGUI finalComboValue; // 拖入 Combo_Value
    public TextMeshProUGUI resultRankText;
    public Slider musicProgressBar;

    [Header("Start Menu UI")]
    public GameObject startMenuPanel;
    public GameObject introPanel;

    [Header("Score")]
    public int currentScore = 0;
    public int comboCount = 0; // 【新增】连击计数
    public int maxComboCount = 0; // 【新增】最大连击记录
    public int baseScore = 5; // 每个音符的基础分数

    [Header("Avatar Components")]
    public Animator avatarAnimator; 

    [Header("Spawner Control")]
    public NoteSpawner[] spawners; // 拖入 4 个 NoteSpawner

    [Header("Gameplay State")]
    public int hitCount = 0; 

    public Text hitText;    
    public Text comboText; // 【新增】连击文本
    private int currentDanceLevel = 0;
    [Header("State Control")]
    public bool isPaused = false;
    public bool isGameFinished = false; // 【新增】游戏结束标志
    public bool isGameStarted = false; // 【新增】游戏是否已经开始
    public float resultDelayTime = 2.0f; // 延迟时间
    public static bool autoPlayAfterLoad = false;
    private AudioSource musicSource; // 【新增】音乐源引用
    
    // 【已改为 AudioSettings.dspTime】不再使用协程方式
    // private Coroutine[] spawnerCoroutines;
    
    // 【新增】判定线反馈映射
    private Dictionary<KeyCode, JudgmentLineFeedback> feedbackMap;

    public void StartGame()
    {
        // --- 1. 核心状态与数据重置 (计科自检：确保上一局的数据不带入) ---
        isGameStarted = true;
        hasMusicStarted = false;
        isGameFinished = false; // 【关键】必须重置结束标记
        Time.timeScale = 1f;    // 恢复游戏时间

        currentScore = 0;       // 分数归零
        comboCount = 0;         // 当前连击归零
        maxComboCount = 0;      // 最大连击纪录归零
        currentDanceLevel = 0;  // 舞蹈等级回 0
        
        // 强制刷新一次 UI，防止界面还显示着上一局的分数
        RefreshUI(); 

        // --- 2. UI 文件夹切换 ---
        if (mainMenuUI != null) mainMenuUI.SetActive(false); 
        if (gameplayHUD != null) gameplayHUD.SetActive(true);
        
        // 如果结算面板开着，也要关掉
        if (resultPanel != null) resultPanel.SetActive(false); 

        // --- 3. 游戏系统初始化 ---
        // 镜头复位逻辑
        if (cameraDirectorScript != null) 
        {
            cameraDirectorScript.canRotate = true; // 确保镜头转起来
        }

        // --- 4. Spawner 激活 (你原本的逻辑) ---
        if (spawners != null && spawners.Length > 0)
        {
            foreach (NoteSpawner spawner in spawners)
            {
                if (spawner != null) spawner.InitSpawner();
            }
        }
        else
        {
            foreach (var spawner in FindObjectsOfType<NoteSpawner>())
            {
                spawner.InitSpawner();
            }
        }

        // --- 5. 音乐启动 ---
        if (musicSource != null)
        {
            musicSource.Stop(); // 先停再播，确保从 0 秒开始
            musicSource.time = 0;
            // 注意：如果你在 Spawner 里用了 PlayDelayed，这里就不需要再调 Play() 了
            // 否则会造成两遍音乐叠加，建议检查 NoteSpawner.InitSpawner() 的内容
            musicSource.Play(); 
            Debug.Log("音乐开始播放指令已发出！");
        }
        isGameStarted = true; // 确保状态正确
        Debug.Log("<color=green>游戏已完全重置并正式开始！</color>");
    }

    // 在 GameManager 类里添加这个新方法
    public void QuitGame()
    {
        Debug.Log("<color=red>退出游戏！</color>"); // 在编辑器里测试时会看到这条 log

        // 真正的退出逻辑
        Application.Quit(); 
    }

    public void ShowIntroduction()
    {
        introPanel.SetActive(true);
        startMenuPanel.SetActive(false);
        Debug.Log("<color=green>显示游戏介绍！</color>");
    }

    public void ReturnToStartMenu()
    {
        introPanel.SetActive(false);
        startMenuPanel.SetActive(true);
        Debug.Log("<color=green>返回开始菜单！</color>");
    }

    public void ShowResult()
    {
        if (resultPanel == null) return;

        // 1. 数据同步：填入这一局的分数和最高连击
        if (finalScoreValue != null) finalScoreValue.text = currentScore.ToString("D6");
        if (finalComboValue != null) finalComboValue.text = maxComboCount.ToString();

        // 2. 调用 Rank 计算逻辑
        CalculateAndSetRank();

        // 3. 处理面板显示（确保你已经加了 Canvas Group）
        CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
        
        resultPanel.SetActive(true);
        
        // 4. 彻底停止游戏物理时间
        Time.timeScale = 0f; 
    }

// 专门计算 Rank 的私有函数，逻辑清晰，方便以后维护
    private void CalculateAndSetRank()
    {
        if (resultRankText == null) return;

        // 按照你设定的 A 案例及其他梯度划分
        if (currentScore >= 30000) // S 级
        {
            resultRankText.text = "S";
            resultRankText.color = new Color(1f, 0.84f, 0f); // 金色 (Gold)
        }
        else if (currentScore >= 10000) // A 级
        {
            resultRankText.text = "A";
            resultRankText.color = new Color(1f, 0f, 1f); // 电光紫 (Magenta)
        }
        else if (currentScore >= 5000) // B 级
        {
            resultRankText.text = "B";
            resultRankText.color = Color.cyan; // 青色
        }
        else // C 级
        {
            resultRankText.text = "C";
            resultRankText.color = Color.white; // 白色
        }
    }
    IEnumerator FadeInResult() 
    {
        // 确保你已经把 Result_Panel 赋值给了 resultPanel 变量
        CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
        
        resultPanel.SetActive(true);
        cg.alpha = 0;

        // 这里用 unscaledDeltaTime 是为了防止你在 ShowResult 里用了 Time.timeScale = 0
        // 否则动画会因为时间停止而卡住
        while (cg.alpha < 1) 
        {
            cg.alpha += Time.unscaledDeltaTime * 2f; // 控制淡入速度
            yield return null; // 告诉 Unity：这一帧跑完了，下一帧再从这里继续
        }
    }

    void Start()
    {
        // 1. 基础组件初始化
        isGameFinished = false; 
        feedbackMap = new Dictionary<KeyCode, JudgmentLineFeedback>();
        musicSource = GetComponent<AudioSource>();

        // 自动查找 Spawner 并建立映射（你原有的逻辑）
        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsOfType<NoteSpawner>();
        }
        foreach (var s in spawners)
        {
            if (s != null && s.feedbackLine != null)
                feedbackMap[s.targetKey] = s.feedbackLine;
        }

        // 初始化进度条
        if (musicProgressBar != null && musicSource != null && musicSource.clip != null)
        {
            musicProgressBar.minValue = 0;
            musicProgressBar.maxValue = musicSource.clip.length;
            musicProgressBar.value = 0;
        }

        // ================== 【关键改动：自动重启逻辑】 ==================
        if (autoPlayAfterLoad)
        {
            // 如果是从 Restart 按钮跳过来的
            autoPlayAfterLoad = false; // 立即重置标记，防止下次干扰
            
            // 隐藏所有菜单，准备直接开跳
            startMenuPanel.SetActive(false);
            introPanel.SetActive(false);
            
            // 直接调用开始游戏函数（确保这个函数里有 Time.timeScale = 1 和数据清零）
            StartGame(); 
            
            Debug.Log("<color=cyan>检测到重启标记，已自动跳过菜单开始游戏</color>");
        }
        else
        {
            // 正常启动：显示主菜单，暂停游戏等待点击
            startMenuPanel.SetActive(true);
            introPanel.SetActive(false);
            Time.timeScale = 0f; 
            
            UpdateUIAndAnimation();
            Debug.Log("<color=green>正常初始化完成，等待玩家点击 Play</color>");
        }
    }

    // 【已改为 AudioSettings.dspTime 在 NoteSpawner.cs 中处理】
    // SpawnerController 协程已不再使用

    public void RefreshUI()
    {
        // 分数：显示为 6 位数，如 000120
        if(scoreUIText != null) scoreUIText.text = currentScore.ToString("D6");

        // 连击：直接显示数字
        if(comboUIText != null) comboUIText.text = comboCount.ToString();
        
        // 等级：
        if(levelUIText != null) levelUIText.text = "LV." + currentDanceLevel;
    }

    void Update()
    {
        // --- 1. 自动激活“开始权限”（补办许可证） ---
        // 只要游戏开始，且音乐动了（哪怕只有 0.001 秒），就激活权限
        if (isGameStarted && musicSource != null && (musicSource.isPlaying || musicSource.time > 0) && !hasMusicStarted)
        {
            hasMusicStarted = true;
            Debug.Log("<color=yellow>音频监控：监测到音乐已经产生位移，已正式开启结算监测。</color>");
        }

        // --- 2. 进度条更新 ---
        if (isGameStarted && musicProgressBar != null && musicSource != null)
        {
            musicProgressBar.value = musicSource.time;
        }

        // --- 3. 【重点】重新设计的结算判定 ---
        // 逻辑：只有当【发了证】且【没在暂停】且【还没标记结束】时，才检查音乐是否停了
        if (isGameStarted && hasMusicStarted && !isPaused && !isGameFinished)
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                // 额外保险：时间必须大于 0.1s，防止在起步那一帧因为延迟被误杀
                if (musicSource.time > 0.1f)
                {
                    Debug.Log("<color=red>！！！音乐正式播放完毕，准备结算！！！</color>");
                    StartEndingSequence();
                }
            }
            
            // 备选保险：如果音乐接近最后 0.1 秒（针对某些 Loop 逻辑或结尾卡顿）
            if (musicSource.clip != null && musicSource.time >= (musicSource.clip.length - 0.1f))
            {
                Debug.Log("<color=red>！！！播放到达时间终点，强制结算！！！</color>");
                StartEndingSequence();
            }
        }

        // --- 4. 暂停检测（只有开始了才能按 ESC） ---
        if (Input.GetKeyDown(KeyCode.Escape) && isGameStarted && !isGameFinished)
        {
            if (!isPaused) PauseGame();
            else ResumeGame();
        }

        if (!isGameStarted || isPaused || isGameFinished) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isGameStarted) return; // 如果还没点 Play，按了也白按

            if (isPaused) ResumeGame();
            else PauseGame();
            return; 
        }

        UpdateAvatarAnimation(); // 每帧检查是否需要切换动画等级
        // 【关键】游戏完全结束后，屏蔽四个按键的所有输入
        UpdateProgressBar();
        if (isGameFinished) 
        {
            // 屏蔽 A、D、LeftArrow、RightArrow 键
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || 
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                Debug.Log("<color=red>游戏已结束，输入已被屏蔽！</color>");
            }
            return;
        }

        // 游戏进行中的按键输入
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            Debug.Log("<color=orange>A 键被按下（原 W）</color>");
            TryHitNote(KeyCode.A);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("<color=orange>D 键被按下（原 A）</color>");
            TryHitNote(KeyCode.D);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("<color=orange>LeftArrow 被按下（原 S）</color>");
            TryHitNote(KeyCode.LeftArrow);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("<color=orange>RightArrow 被按下（原 D）</color>");
            TryHitNote(KeyCode.RightArrow);
        }

        // 强制位置和旋转锁定
        if (avatarAnimator != null)
        {
            Vector3 targetBase = avatarAnimator.transform.parent != null ? avatarAnimator.transform.parent.position : Vector3.zero;
            avatarAnimator.transform.position = targetBase;
            avatarAnimator.transform.rotation = Quaternion.identity;
        }

        // 【调试】每帧打印当前 Spawner 配置（仅在游戏运行时第一次）
        if (spawners != null && spawners.Length > 0 && Time.frameCount == 1)
        {
            Debug.Log("<color=yellow>========== Spawner 配置清单 ==========</color>");
            for (int i = 0; i < spawners.Length; i++)
            {
                Debug.Log($"<color=yellow>Spawner[{i}]: targetKey={spawners[i].targetKey}, notePrefab={spawners[i].notePrefab.name}</color>");
            }
            Debug.Log("<color=yellow>按键映射: W→A(原 W), A→D(原 A), S→LeftArrow(原 S), D→RightArrow(原 D)</color>");
            Debug.Log("<color=yellow>=====================================</color>");
        }
    }

    void UpdateProgressBar()
    {
        if (musicProgressBar != null && musicSource != null && musicSource.isPlaying)
        {
            // 将当前音频播放时间同步到 Slider
            musicProgressBar.value = musicSource.time;
        }
    }

    // 尝试击中按键对应的音符
    void TryHitNote(KeyCode key)
    {
        NoteObject[] allNotes = FindObjectsOfType<NoteObject>();
        Debug.Log($"<color=cyan>TryHitNote({key}): 找到 {allNotes.Length} 个音符</color>");
        
        bool found = false;
        foreach (NoteObject note in allNotes)
        {
            //Debug.Log($"<color=cyan>  检查音符: targetKey={note.targetKey}, canBePressed={note.canBePressed}</color>");
            
            // 找到 targetKey 匹配且在判定范围内的音符
            if (note.targetKey == key && note.canBePressed) 
            {
                Debug.Log($"<color=yellow>找到匹配的音符！调用 TryHit()</color>");
                note.TryHit(); // 调用音符的 TryHit 方法
                found = true;
                break; // 一次按键只击中一个最近的音符
            }
        }
        
        if (!found)
        {
            // 【改动】空按也有惩罚，和 miss 一样
            Debug.Log($"<color=red>未找到 {key} 对应的音符！执行 miss 惩罚</color>");
            OnNotemiss();
        }
    }

    public void OnNoteHit(KeyCode key)
    {
        hitCount++;
        comboCount++; // 【新增】增加连击计数

        if(hitSfxSource != null && hitSound != null)
        {
            hitSfxSource.clip = hitSound; // 重新指定剪辑
            hitSfxSource.Stop();          // 【可选】明确停止上一个（其实调用 Play 也会自动重置）
            hitSfxSource.Play();          // 开始播放
        }

        if(comboCount > maxComboCount)
        {
            maxComboCount = comboCount; // 更新最大连击记录
        }
        
        // 【修改】使用新的分数计算规则：Score = BasePoint × Level × (1 + ComboCount/10)
        float scoreMultiplier = 1f + (comboCount / 10f);
        int scoreGain = Mathf.FloorToInt(baseScore * currentDanceLevel * scoreMultiplier);
        currentScore += scoreGain;
        Debug.Log($"<color=green>Hit! Score +{scoreGain} (Level={currentDanceLevel}, Combo={comboCount})</color>");
        
        // 只有当 comboCount 是 10 的倍数时才触发闪光
        if (comboCount % 10 == 0)
        {
            // 【修改】根据 key 找到对应的判定线反馈
            if (feedbackMap.ContainsKey(key) && feedbackMap[key] != null)
            {
                feedbackMap[key].TriggerLight();
                Debug.Log($"<color=yellow>触发闪光 [{key}]! ComboCount: {comboCount}</color>");
            }
        }
        RefreshUI();
        UpdateUIAndAnimation();
    }

    public void OnNotemiss()
    {
        if(isGameFinished) return; // 【新增】如果游戏已经结束，直接返回，不执行任何操作
        comboCount = 0; // 【新增】连击中断
        hitCount = Mathf.Max(0, hitCount - 5); // 【新增】Miss 时减少 5 分
        // 【改动】不再扣分
        RefreshUI();
        UpdateUIAndAnimation();
        //Debug.Log("<color=yellow>Miss! Combo Reset. Hit Count decreased by 5.</color>");
    }

    public void OnNoteDecrease()
    {
        if (hitCount > 0)
        {
            hitCount = Mathf.Max(0, hitCount - 10);
            UpdateUIAndAnimation();
        }
    }

    // --- 【新增】供 StageEffectManager 调用的重置方法 ---
    public void ResetProgress()
    {
        hitCount = 0;
        comboCount = 0; // 【新增】重置连击计数
        currentDanceLevel = 0; // 强制设为 0
        isGameFinished = true; // 【新增】标记游戏完全结束，禁用所有输入
        
        // 【已改为 AudioSettings.dspTime】Spawner 会自动检查 isGameFinished 并停止生成
        
        if (avatarAnimator != null)
        {
            avatarAnimator.SetInteger("DanceLevel", 0);
        }
        
        UpdateUIAndAnimation();
        Debug.Log("<color=red>RhythmGameManager: 已清零回到 Level 0，禁用所有输入</color>");
    }

    private void UpdateUIAndAnimation()
    {
        // 这里只处理颜色或特效反馈
        if (comboUIText != null) 
        {
            // 霓虹反馈：等级越高，颜色越亮
            comboUIText.color = currentDanceLevel >= 4 ? Color.magenta : Color.cyan;
        }
        UpdateAvatarAnimation(); 
    }

    public bool ShouldTriggerLevel5()
    {
        if(musicSource == null || musicSource.clip == null) return false;
        float framesToSecond = 300f / 60f; // 假设音乐是以 60 FPS 的帧率进行计算的

        float totalDuration = musicSource.clip.length; // 音乐总时长
        float currentTime = musicSource.time; // 当前音乐播放时间

        return (totalDuration - currentTime) <= framesToSecond && musicSource.isPlaying; // 当剩余时间小于等于 300 帧时触发 Level 5
    }

    void UpdateAvatarAnimation()
    {
        if (avatarAnimator == null) return;

        int newDanceLevel = 0;
        
        // 获取挂载在 GameManager 上的 AudioSource (也就是 Billie Jean)
        if (musicSource == null) musicSource = GetComponent<AudioSource>();

        // 逻辑：计算剩余秒数。 288帧 / 60fps = 4.8秒
        if (musicSource != null && musicSource.clip != null && musicSource.isPlaying)
        {
            float remainingTime = musicSource.clip.length - musicSource.time;
            if (remainingTime <= 5f) // 只要还剩 5 秒就强制进入 Level 5
            {
                newDanceLevel = 5;
            }
        }

        // 如果不是 Level 5，再走 hitCount 逻辑
        if (newDanceLevel != 5)
        {
            if (hitCount >= 100) newDanceLevel = 4;
            else if (hitCount >= 50) newDanceLevel = 3;
            else if (hitCount >= 20) newDanceLevel = 2;
            else if (hitCount >= 1) newDanceLevel = 1;
            else newDanceLevel = 0;
        }

        // 执行切换
        if (newDanceLevel != currentDanceLevel)
        {
            currentDanceLevel = newDanceLevel;
            avatarAnimator.SetInteger("DanceLevel", currentDanceLevel);
            
            StageEffectManager effectManager = FindObjectOfType<StageEffectManager>();
            if (effectManager != null) effectManager.UpdateLevel(currentDanceLevel);

            Debug.Log($"<color=magenta>状态切换！当前 Level: {currentDanceLevel}</color>");

            if (newDanceLevel >= currentDanceLevel && levelUpSound != null && levelUpSfxSource != null)
            {
                levelUpSfxSource.PlayOneShot(levelUpSound);
                Debug.Log("<color=cyan>播放升级音效！</color>");
            }

            if (levelUIText != null) 
            {
                levelUIText.text = "LEVEL " + currentDanceLevel;

                // 按照你的设计方案进行配色
                if (currentDanceLevel >= 3)
                {
                    // Level 3-4：使用橙色/火红色
                    levelUIText.color = new Color(1f, 0.3f, 0f); // 橙红色
                }
                else if (currentDanceLevel >= 1)
                {
                    // Level 1-2：使用青色/蓝色
                    levelUIText.color = Color.cyan; 
                }
                else
                {
                    // Level 0：默认灰色或白色
                    levelUIText.color = Color.white;
                }
            }
        }
    }

    public void PauseGame() // 按下 Esc 触发
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // 停止物理和时间逻辑
        if (musicSource != null) musicSource.Pause(); // 停止音乐
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        
        if (musicSource != null)
        {
            musicSource.UnPause(); // 确保音乐立即恢复播放状态
        }
    }

    // 【Restart 键专用】：直接重载场景，利用 Start() 里的逻辑回到初始状态，然后自动触发 StartGame() 的逻辑
    public void RestartGame() 
    {
        Debug.Log("--- 执行 Hard Restart：重载场景 ---");
        
        // 1. 必须恢复时间，否则场景加载后会卡死
        Time.timeScale = 1f;
        isPaused = false;
        isGameFinished = false;

        // 2. 标记：告诉下一生，进来就直接点 Play
        autoPlayAfterLoad = true; 

        // 3. 彻底重载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    // 【Home 键专用】：重置状态并显式确保主菜单 UI 开启
    public void GoToHome()
    {
        isGameStarted = false; // 【新增】标记游戏未开始
        Debug.Log("--- 触发主菜单：重置 UI 状态 ---");
        Time.timeScale = 1f;
        isPaused = false;
        isGameFinished = false;

        // 如果你只有一个场景，直接加载它就会触发 Start() 里的：
        // startMenuPanel.SetActive(true); 
        // introPanel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private IEnumerator DelayedShowResult()
    {
        yield return new WaitForSecondsRealtime(resultDelayTime);

        Debug.Log("正在填入最终数据...");

        // 【核心修复】先填数据，再显示面板！
        if (finalScoreText != null) 
            finalScoreText.text = currentScore.ToString("D6"); // 补足6位显示
        
        if (maxComboText != null) 
            maxComboText.text = maxComboCount.ToString();

        // 运行 Rank 计算逻辑
        CalculateAndSetRank();

        // 最后再激活面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            Debug.Log("结算面板已显示，数据已填入");
        }

        Time.timeScale = 0f; // 彻底停止游戏
    }
    public void StartEndingSequence()
    {
        // 如果已经开始结算了，就不要再进来了（状态锁）
        if (isGameFinished) return; 
        
        isGameFinished = true; // 立即锁定输入
        Debug.Log("<color=magenta>统一结算序列启动！</color>");
        
        // 启动那个你想要的延迟协程
        StartCoroutine(DelayedShowResult());
    }
}