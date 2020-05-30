using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 放在摄像机物体上.
/// </summary>
public class GLWireFrame : MonoBehaviour
{
    void OnPreRender()
    {
        GL.wireframe = true;
    }


    void OnPostRender()
    {
        GL.wireframe = false;
    }
}
