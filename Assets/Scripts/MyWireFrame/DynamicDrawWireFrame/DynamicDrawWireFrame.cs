using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 绘制模型的网格线框.(直接放在模型身上，初始化时创建)
/// </summary>
public class DynamicDrawWireFrame : MonoBehaviour
{
    private Transform m_Transform;
    private MeshFilter m_MeshFilter;
    private Transform m_drawWireFrameParent;            //描绘物体线框的线 父物体.
    
    [Header("退化四边形资源文件")]
    [SerializeField]
    private DegradedRectangles m_DegradedRectangles;    //退化四边形资源文件.

    [Header("LineRenderer预制体")]
    [SerializeField]
    private GameObject m_Prefab_Line;                   //LineRender预制体.

    void Start()
    {
        if (m_DegradedRectangles == null)
        {
            Debug.LogError("没有赋值退化四边形.");
            return;
        }

        //查找初始化.
        m_Transform = gameObject.GetComponent<Transform>();
        m_MeshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = m_MeshFilter.sharedMesh;
        m_drawWireFrameParent = m_Transform.Find("DrawWireFrameParent");
        if (m_drawWireFrameParent == null) 
        { 
            m_drawWireFrameParent = new GameObject("DrawWireFrameParent").transform;
            m_drawWireFrameParent.SetParent(m_Transform, false); 
        }
            
        //临时变量.
        Vector3 v1;
        Vector3 v2;
        Vector3 v3_1;
        Vector3 v3_2;
        Vector3 vv1;
        Vector3 vv2;
        Vector3 vv3;
        Vector3 face1Normal;
        Vector3 face2Normal;
        float angle;
        List<Vector3> drawList = new List<Vector3>();

        //循环退化四边形，通过计算，得出网格的边缘线.
        for (int i = 0; i < m_DegradedRectangles.degraded_rectangles.Count; i++)
        {
            //获取退化四边形对应的网格顶点坐标.
            v1 = mesh.vertices[m_DegradedRectangles.degraded_rectangles[i].vertex1];    
            v2 = mesh.vertices[m_DegradedRectangles.degraded_rectangles[i].vertex2];
            v3_1 = mesh.vertices[m_DegradedRectangles.degraded_rectangles[i].triangle1_vertex3];

            if (m_DegradedRectangles.degraded_rectangles[i].triangle2_vertex3 > 0)    //如果是边界边缘，该值为-1.
            {
                v3_2 = mesh.vertices[m_DegradedRectangles.degraded_rectangles[i].triangle2_vertex3]; //获取退化四边形对应的网格顶点坐标.

                //计算出两个相邻三角面的法线向量.
                vv1 = v2 - v1;      
                vv2 = v3_1 - v1;
                vv3 = v3_2 - v1;
                face1Normal = Vector3.Cross(vv1, vv2).normalized;
                face2Normal = Vector3.Cross(vv3, vv1).normalized;

                //点积，计算两个三角面是否平行.
                angle = Mathf.Acos(Vector3.Dot(face1Normal, face2Normal)) * Mathf.Rad2Deg;  //两条法线相交的角度.
                //angle = Vector3.Angle(face1Normal, face2Normal);      //只能算到 [0,180] 度.

                if (angle < -2f || angle > 2f)  //小于或大于.    两个面不平行，该线不是中间线.
                {
                    Debug.Log("边缘");
                    drawList.Add(v1);
                    drawList.Add(v2);
                }
                else    //两个面平行.    不画中间的线.
                {

                }
            }
            else    //边界边缘.
            {
                Debug.Log("边界边");
                Debug.Log(m_DegradedRectangles.degraded_rectangles[i]);
                drawList.Add(v1);
                drawList.Add(v2);
            }
        }

        //循环相框集合，每两个点，生成一根线.
        GameObject line;
        LineRenderer line_LineRenderer;
        for (int i = 0; i < drawList.Count; i += 2)
        {
            line = GameObject.Instantiate<GameObject>(m_Prefab_Line, m_drawWireFrameParent);
            line.name = "Line_" + i;
            line_LineRenderer = line.GetComponent<LineRenderer>();
            line_LineRenderer.positionCount = 2;
            line_LineRenderer.SetPosition(0, drawList[i]);
            line_LineRenderer.SetPosition(1, drawList[i + 1]);
        }

    }

}
