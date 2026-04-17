using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteObject : MonoBehaviour
{
    public float scrollSpeed = 8f;   // 方块下落速度
    public bool canBePressed = false; // 是否在判定区内

    public float perfectThreshold = 0.2f;  // 完美判定的距离阈值
    public float goodThreshold = 0.5f;     // 普通判定的距离阈值
    public KeyCode targetKey = KeyCode.A;  // 这个方块对应的按键（A/D/LeftArrow/RightArrow）

    void Update()
    {
        // 1. 实时更新下落位置
        transform.localPosition += Vector3.down * scrollSpeed * Time.deltaTime;

        // 2. 实时检查是否在判定范围内
        float distance = Mathf.Abs(transform.localPosition.y - (-1f));
        canBePressed = (distance <= goodThreshold); // 判定范围内才能按

        // 3. 自动销毁（漏接的情况）
        // 按键检测现在由 RhythmGameManager 统一处理
        if (transform.localPosition.y < -2f) 
        {
            RhythmGameManager manager = FindObjectOfType<RhythmGameManager>();
            if (manager != null)
            {
                manager.OnNotemiss();
            }
            Destroy(gameObject);
        }
    }

    // 供 RhythmGameManager 调用，当按键符合 targetKey 时触发正式判定
    public void TryHit()
    {
        float distance = Mathf.Abs(transform.localPosition.y - (-1f));

        if (distance <= perfectThreshold)
        {
            Debug.Log("Perfect!");
            HitSuccess();
        }
        else if (distance <= goodThreshold)
        {
            Debug.Log("Good!");
            HitSuccess();
        }
        // 如果距离太远，按下也没反应
    }

    void HitSuccess()
    {
        // 1. 调用游戏管理器增加分数，并传递这个音符的按键
        RhythmGameManager gameManager = FindObjectOfType<RhythmGameManager>();
        if (gameManager != null)
        {
            gameManager.OnNoteHit(targetKey);
        }

        // 2. 销毁方块
        Destroy(gameObject); 
    }
}