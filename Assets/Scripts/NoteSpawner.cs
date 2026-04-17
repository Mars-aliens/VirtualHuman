using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Material laneMaterial;
    public KeyCode targetKey;
    public JudgmentLineFeedback feedbackLine; // 拖入这个 Spawner 对应的判定线

    [Header("音乐设置")]
    public AudioSource musicSource; 
    public float bpm = 117f;        // Billie Jean 的 BPM
    public float delayBeforeStart = 2f; // 给玩家 2 秒准备时间
    public int remainingFramesForLevel5 = 300; // 【新增】音乐还剩这么多帧时触发 Level 5

    private float beatInterval;
    private float nextBeatTime;
    private bool hasStarted = false;
    private RhythmGameManager gameManager;
    private bool isInitialized = false;

    public void InitSpawner()
    {
        beatInterval = 60f / bpm;
        gameManager = FindObjectOfType<RhythmGameManager>();

        // 计算第一次生成的绝对音频时间
        nextBeatTime = (float)AudioSettings.dspTime + delayBeforeStart;
        
        // 延迟播放音乐，确保第一批方块有时间飞到判定线
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.PlayDelayed(delayBeforeStart);
            Debug.Log($"<color=green>Spawner [{targetKey}] 开始工作，BPM={bpm}，beat间隔={beatInterval}s，延迟={delayBeforeStart}s</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>Spawner [{targetKey}] 的 musicSource 未设置！</color>");
        }
        hasStarted = true;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || !hasStarted) return;

        // 【新增】检查游戏是否已结束
        if (gameManager != null && gameManager.isGameFinished)
        {
            return;  // 游戏结束，停止生成
        }

        // 使用高精度音频时钟进行判断
        if (AudioSettings.dspTime >= nextBeatTime)
        {
            int currentBeat = Mathf.FloorToInt((float)AudioSettings.dspTime / beatInterval) % 4;
            bool shouldSpawn = false;
            // 根据当前节拍和按键类型决定是否生成方块

            switch (targetKey)
            {
                case KeyCode.A:
                    shouldSpawn = (currentBeat == 0 || currentBeat == 2);
                    break;
                case KeyCode.D:
                    shouldSpawn = (currentBeat == 1 || currentBeat == 3);
                    break;
                case KeyCode.LeftArrow:
                    shouldSpawn = (Random.value > 0.5f);
                    break;
                case KeyCode.RightArrow:
                    shouldSpawn = (Random.value > 0.8f);
                    break;
            }
            if(shouldSpawn)
            {
                SpawnNote();
            }
            nextBeatTime += beatInterval; // 严格累加节拍间隔
            //Debug.Log($"<color=cyan>Spawner [{targetKey}] 生成音符 (dspTime={AudioSettings.dspTime:F3})</color>");
        }
    }

    public void SpawnNote()
    {
        GameObject newNote = Instantiate(notePrefab, transform.position, transform.rotation);
        
        // 修复之前的红线报错：确保使用 noteRenderer
        MeshRenderer noteRenderer = newNote.GetComponentInChildren<MeshRenderer>();
        if (noteRenderer != null && laneMaterial != null)
        {
            noteRenderer.material = laneMaterial;
        }

        // 让生成的方块跟随相机（你之前的逻辑）
        newNote.transform.SetParent(Camera.main.transform);

        // 传递按键
        NoteObject noteScript = newNote.GetComponent<NoteObject>();
        if(noteScript != null) noteScript.targetKey = this.targetKey;
    }

    // 【新增】获取音乐的剩余采样帧数
    public int GetRemainingAudioFrames()
    {
        if (musicSource == null || musicSource.clip == null) 
            return int.MaxValue;
        
        // 当前播放位置（采样）
        int currentSample = musicSource.timeSamples;
        // 总采样数
        int totalSamples = musicSource.clip.samples;
        // 剩余采样数
        int remainingSamples = totalSamples - currentSample;
        
        return remainingSamples;
    }

    // 【新增】检查是否应该触发 Level 5（基于剩余帧数）
    public bool ShouldTriggerLevel5()
    {
        return GetRemainingAudioFrames() <= remainingFramesForLevel5;
    }
}
