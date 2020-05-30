using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建建筑线框.
/// </summary>
public class CreateWireFrameEditor : EditorWindow
{
    private static CreateWireFrameEditor s_CWFE;                    //编辑器窗口.

    private static GameObject s_ActiveObj;                          //选中的物体.
    private static GameObject s_Prefab_Line = null;                 //线框预制体. (就是一个带LineRenderer组件的空物体，其材质Shader为MK/Glow/Selective/Sprites/Default)
    private static DegradedRectangles s_DegradedRectangles = null;  //退化四边形资源文件.

    [MenuItem("MyTool/建筑外边框/添加建筑外边框 &A")]
    private static void OpenCreateWireFrameWindow()
    {
        try
        {
            s_ActiveObj = Selection.activeObject as GameObject;
            if (s_ActiveObj.GetComponent<MeshFilter>() == null)
            {
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("请先选中带网格的物体...");
            //throw e;
        }

        s_CWFE = EditorWindow.GetWindow<CreateWireFrameEditor>("创建线框参数");
        s_CWFE.Show();
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("设置初始参数，点击按钮创建", style, GUILayout.Width(base.position.width), GUILayout.ExpandWidth(true), GUILayout.Height(25));

        s_ActiveObj = EditorGUILayout.ObjectField("选中的物体:", s_ActiveObj, typeof(GameObject)) as GameObject;
        s_Prefab_Line = EditorGUILayout.ObjectField("线框,线预制体:", s_Prefab_Line, typeof(GameObject)) as GameObject;
        s_DegradedRectangles = (DegradedRectangles)EditorGUILayout.ObjectField("对应退化四边形资源:", s_DegradedRectangles, typeof(DegradedRectangles));

        if (GUI.Button(new Rect(0, 150, base.position.width, 18), "创建外边框"))
        {
            CreateWireFrameMethod();

            s_CWFE.Close();
        }
    }

    /// <summary>
    /// 创建线框方法.
    /// </summary>
    private void CreateWireFrameMethod()
    {
        //检验是否赋值退化四边形.
        if (s_DegradedRectangles == null || s_Prefab_Line == null)
        {
            if (s_DegradedRectangles == null)
                Debug.LogError("没有赋值退化四边形.");
            else
                Debug.LogError("没有赋值线预制体.");

            return;
        }

        //检验是否已存在线框.
        Transform drawWireFrameParent = null;
        drawWireFrameParent = s_ActiveObj.transform.Find("DrawWireFrameParent");
        if (drawWireFrameParent != null)
        {
            Debug.Log(s_ActiveObj.name + ", 已存在线框...");
            return;
        }

        //获取物体的网格组件. 新建线框父物体.
        Mesh mesh = s_ActiveObj.GetComponent<MeshFilter>().sharedMesh;
        drawWireFrameParent = new GameObject("DrawWireFrameParent").transform;
        drawWireFrameParent.SetParent(s_ActiveObj.transform, false);

        //临时变量.
        Vector3 v1;     //退化四边形对应的网格顶点坐标.
        Vector3 v2;
        Vector3 v3_1;
        Vector3 v3_2;
        Vector3 vv1;    //法线向量.
        Vector3 vv2;
        Vector3 vv3;
        Vector3 face1Normal;
        Vector3 face2Normal;
        float angle;
        List<Vector3> drawList = new List<Vector3>();   //线框集合.

        //循环退化四边形，通过计算，得出网格的边缘线.
        for (int i = 0; i < s_DegradedRectangles.degraded_rectangles.Count; i++)
        {
            //获取退化四边形对应的网格顶点坐标.
            v1 = mesh.vertices[s_DegradedRectangles.degraded_rectangles[i].vertex1];
            v2 = mesh.vertices[s_DegradedRectangles.degraded_rectangles[i].vertex2];
            v3_1 = mesh.vertices[s_DegradedRectangles.degraded_rectangles[i].triangle1_vertex3];

            if (s_DegradedRectangles.degraded_rectangles[i].triangle2_vertex3 > 0)    //如果是边界边缘，该值为-1.
            {
                v3_2 = mesh.vertices[s_DegradedRectangles.degraded_rectangles[i].triangle2_vertex3]; //获取退化四边形对应的网格顶点坐标.

                //计算出两个相邻三角面的法线向量.
                vv1 = v2 - v1;
                vv2 = v3_1 - v1;
                vv3 = v3_2 - v1;
                face1Normal = Vector3.Cross(vv1, vv2).normalized;
                face2Normal = Vector3.Cross(vv3, vv1).normalized;

                //点积，计算两个三角面是否平行.
                angle = Mathf.Acos(Vector3.Dot(face1Normal, face2Normal)) * Mathf.Rad2Deg;  //两条法线相交的角度.
                //angle = Vector3.Angle(face1Normal, face2Normal);      //只能算到 [0,180] 度.

                //if (angle > 2f)  //大于.
                if (angle < -2f || angle > 2f)  //小于或大于.    两个面不平行，该线不是中间线.
                {
                    //Debug.Log("边缘");
                    drawList.Add(v1);
                    drawList.Add(v2);
                }
                else    //两个面平行.    不画中间的线.
                {

                }
            }
            else    //边界边缘.
            {
                //Debug.Log("边界边");
                //Debug.Log(degradedRectangles.degraded_rectangles[i]);
                drawList.Add(v1);
                drawList.Add(v2);
            }
        }

        //循环线框集合，每两个点，生成一根线.
        GameObject line;
        LineRenderer line_LineRenderer;
        for (int i = 0; i < drawList.Count; i += 2)
        {
            line = GameObject.Instantiate<GameObject>(s_Prefab_Line, drawWireFrameParent);
            line.name = "Line_" + i;
            line_LineRenderer = line.GetComponent<LineRenderer>();
            line_LineRenderer.positionCount = 2;
            line_LineRenderer.SetPosition(0, drawList[i]);
            line_LineRenderer.SetPosition(1, drawList[i + 1]);
        }

        Debug.Log("生成线框成功");
    }
}
