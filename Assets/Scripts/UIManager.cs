using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 在 Inspector 面板把对应的“文件夹”拖进来
    public GameObject mainMenuUI;
    public GameObject gameplayHUD;

    // 这个方法给 StartButton 调用
    public void StartGame()
    {
        // 1. 关掉主菜单
        mainMenuUI.SetActive(false);
        
        // 2. 开启游戏内的 UI (分数、进度条)
        gameplayHUD.SetActive(true);
        
        // 3. (可选) 这里可以调用你的音乐播放脚本开始放歌
        Debug.Log("游戏开始！");
    }
}
