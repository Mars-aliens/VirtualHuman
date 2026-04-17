using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgmentLineFeedback : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Color originalEmissionColor;
    
    // 标识这条判定线对应的按键
    public KeyCode associatedKey = KeyCode.A;
    
    // 设为 HDR 颜色，在 Inspector 里把强度调高
    [ColorUsage(true, true)] 
    public Color highlightEmissionColor = Color.white; 
    public float fadeSpeed = 8f;               // 加快恢复速度，突显瞬间感

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        // 确保材质启用了 Emission 关键字
        if (meshRenderer != null)
        {
            meshRenderer.material.EnableKeyword("_EMISSION");
            // 获取初始的 Emission 颜色（通常是黑色，即不发光）
            originalEmissionColor = meshRenderer.material.GetColor("_EmissionColor");
        }
    }

    void Update()
    {
        // 每一帧都将 Emission 颜色插值回到初始状态
        if (meshRenderer != null)
        {
            Color currentEmission = meshRenderer.material.GetColor("_EmissionColor");
            Color nextEmission = Color.Lerp(currentEmission, originalEmissionColor, Time.deltaTime * fadeSpeed);
            meshRenderer.material.SetColor("_EmissionColor", nextEmission);
        }
    }

    // 点亮判定线（设置高强度的自发光）
    public void TriggerLight()
    {
        if (meshRenderer != null)
        {
            // 如果你想让它极其亮，甚至带有辉光（Bloom）效果，可以在这里乘一个大于 1 的系数
            meshRenderer.material.SetColor("_EmissionColor", highlightEmissionColor * 2f); 
        }
    }
}