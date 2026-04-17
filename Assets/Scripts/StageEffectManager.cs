using UnityEngine;

public class StageEffectManager : MonoBehaviour
{
    public Animator avatarAnimator;
    private Light mainLight;
    public CameraDirector cameraDirectorScript; 

    private int currentDanceLevel = 0;
    private bool isFinished = false;
    private bool hasReachedLevel5 = false;
    private float level5AnimationDuration = 0f;
    private float level5AnimationStartTime = 0f;
    private bool cameraAtTarget = false; // 【新增】记录相机是否已到达目标位置

    void Start()
    {
        // 自动寻找灯光
        GameObject lightObj = GameObject.FindWithTag("CyanLight");
        if (lightObj != null) mainLight = lightObj.GetComponent<Light>();
    }

    void Update()
    {
        // 1. 核心判断：首次到达 Level 5 时记录动画时长
        if (currentDanceLevel >= 5 && hasReachedLevel5 && !isFinished)
            {
                float elapsedTime = Time.time - level5AnimationStartTime;

                // 【新增逻辑】在动画快结束（比如最后 0.5 秒）或者结束时，启动复位
                if (elapsedTime >= level5AnimationDuration)
                {
                    isFinished = true;
                    
                    // 1. 恢复镜头脚本的控制权，让它能再次转动
                    if (cameraDirectorScript != null) 
                    {
                        cameraDirectorScript.canRotate = true; 
                    }

                    // 2. 告诉相机：不要再执行锁定逻辑了
                    cameraAtTarget = false; 

                    // 3. 触发延迟结算
                    RhythmGameManager manager = FindObjectOfType<RhythmGameManager>();
                    if (manager != null) manager.StartEndingSequence();
                }
            }

        // 2. 等待 Level 5 动画播放完成后再重置
        if (hasReachedLevel5 && !isFinished)
        {
            float elapsedTime = Time.time - level5AnimationStartTime;
            
            // 当动画播放完成后，触发重置
            if (elapsedTime >= level5AnimationDuration)
            {
                isFinished = true;
                
                // 不要直接 ResetProgress，而是通过 Manager 触发带延迟的结算
                RhythmGameManager gameManager = FindObjectOfType<RhythmGameManager>();
                if (gameManager != null && !gameManager.isGameFinished)
                {
                    // 调用我们新改的那个带协程的逻辑入口
                    gameManager.StartEndingSequence(); 
                }
            }
        }

        // 3. 如果已进入结束状态，持续执行相机定格逻辑
        if (isFinished)
        {
            PerformFinishLock();
            
            // 既然是游戏结束，灯光也慢慢回到基础亮度
            if (mainLight != null)
                mainLight.intensity = Mathf.Lerp(mainLight.intensity, 1.5f, Time.deltaTime);
                
            return; // 结束状态下不执行后续逻辑
        }

        // 灯光平时自动回落
        if (mainLight != null)
            mainLight.intensity = Mathf.Lerp(mainLight.intensity, 1.5f, 5f * Time.deltaTime);

        // 锁定角色位置，防止由于动画位移导致的穿模
        if (avatarAnimator != null)
        {
            avatarAnimator.transform.position = Vector3.zero;
            avatarAnimator.transform.rotation = Quaternion.identity;
        }
    }

    // 执行最终定格：相机平滑回到正前方，到达后停止转动
    void PerformFinishLock()
    {
        Vector3 targetPos = new Vector3(0, 1.6f, 5.0f);
        
        // 如果相机还没到达目标位置，就继续移动
        if (!cameraAtTarget)
        {
            Vector3 currentPos = Camera.main.transform.position;
            Vector3 newPos = Vector3.Lerp(currentPos, targetPos, 2f * Time.deltaTime);
            Camera.main.transform.position = newPos;
            Camera.main.transform.LookAt(new Vector3(0, 1.0f, 0));
            
            // 检查是否已接近目标位置（距离小于 0.1）
            float distance = Vector3.Distance(newPos, targetPos);
            if (distance < 0.1f)
            {
                cameraAtTarget = true;
                Camera.main.transform.position = targetPos; // 精确锁定到目标位置
                Camera.main.transform.LookAt(new Vector3(0, 1.0f, 0));
                Debug.Log("<color=green>相机已到达初始位置，已锁定</color>");
            }
        }
        // 相机已到达目标位置后，就不再执行任何操作，保持固定
    }

    public void UpdateLevel(int level) 
    { 
        currentDanceLevel = level; 
    }

    // 【新增】重置游戏状态方法，供 GameManager 调用
    public void ResetGameState()
    {
        isFinished = false;
        hasReachedLevel5 = false;
        cameraAtTarget = false;
        level5AnimationDuration = 0f;
        level5AnimationStartTime = 0f;
        
        // 恢复相机旋转
        if (cameraDirectorScript != null) 
            cameraDirectorScript.canRotate = true;
        
        Debug.Log("<color=magenta>StageEffectManager: 已重置所有状态，相机可旋转</color>");
    }
}