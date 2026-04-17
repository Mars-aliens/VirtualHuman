using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public float rotateSpeed = 10f; 
    // 【关键】必须加上这行，否则另一个脚本找不到它
    public bool canRotate = true; 

    void Update()
    {
        // 如果开关被关掉，就不再旋转
        if (!canRotate) return; 

        transform.RotateAround(Vector3.zero, Vector3.up, 15f * Time.deltaTime);
        transform.LookAt(new Vector3(0, 1.0f, 0));
    }
}
