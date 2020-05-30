using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 退化四边形.
/// </summary>
[System.Serializable]
public struct DegradedRectangle
{
    public int vertex1;             //构成边的顶点1的索引
    public int vertex2;             //构成边的顶点2的索引
    public int triangle1_vertex3;   //边所在三角面1的顶点3索引
    public int triangle2_vertex3;   //边所在三角面2的顶点3索引 
}

/// <summary>
/// 创建退化四边形资源文件.
/// </summary>
[CreateAssetMenu(fileName = "DegradedRectanglesData", menuName = "Degraded Rectangles(创建退化四边形资源文件)")]
public class DegradedRectangles : ScriptableObject
{
    [Header("点击右上方齿轮，生成退化四边形顶点索引")]             

    [Header("需要创建退化四边形的网格")]
    public Mesh mesh;                                       //需要创建退化四边形的网格.

    /// <summary>
    /// 退化四边形的顶点索引.
    /// </summary>
    [Header("退化四边形的顶点索引")]
    public List<DegradedRectangle> degraded_rectangles;     //(v1、v2、v1_3)(v1、v2、v2_3)为相同一条边的两个三角面，两个三角面即为一个退化四边形.   v2_3为-1，即该四边形相同边是“边界边缘”.

    /// <summary>
    /// 创建退化四边形.
    /// </summary>
    [ContextMenu("Generate Degraded Rectangle(生成退化四边形顶点索引)")]
    private void GenerateDegradedRectangle()
    {
        if (mesh == null)
        {
            Debug.LogError("mesh is null");
            return;
        }

        int[] triangles = mesh.triangles;   //所有三角面的顶点索引(对应网格顶点坐标的索引).
        Vector3[] vertices = mesh.vertices; //网格顶点坐标数组.

        // 遍历Mesh.triangles来找到所有退化四边形，要求无重复
        List<MeshLine> custom_lines = new List<MeshLine>();
        int length = triangles.Length / 3;
        for (int i = 0; i < length; i++)
        {
            //获得三角面的顶点索引.
            int vertex1_index = triangles[i * 3];
            int vertex2_index = triangles[i * 3 + 1];
            int vertex3_index = triangles[i * 3 + 2];

            //添加三角面进custom_lines中，相同边即构成退化四边形. (网格的两个顶点连成的边，有多少条边，即有多少个退化四边形) (eg:Cube网格就有18个退化四边形)
            AddCustomLine(vertex1_index, vertex2_index, vertex3_index, vertices, custom_lines); //添加三角图元vertex1和vertex2构成的退化四边形（或叫边）
            AddCustomLine(vertex2_index, vertex3_index, vertex1_index, vertices, custom_lines); //添加三角图元vertex2和vertex3构成的退化四边形（或叫边）
            AddCustomLine(vertex3_index, vertex1_index, vertex2_index, vertices, custom_lines); //添加三角图元vertex3和vertex1构成的退化四边形（或叫边）
        }

        //把customLines中的退化四边形顶点索引，存储到degraded_rectangles中.
        degraded_rectangles = new List<DegradedRectangle>(custom_lines.Count);
        for (int i = 0; i < custom_lines.Count; i++)
        {
            degraded_rectangles.Add(custom_lines[i].degraded_rectangle);
        }

        Debug.Log("成功生成退化四边形");
    }

    /// <summary>
    /// 添加三角面v1、v2构成退化四边形，存储到临时的customLines中.
    /// </summary>
    /// <param name="vertex1Index">相同边顶点1</param>
    /// <param name="vertex2Index">相同边顶点2</param>
    /// <param name="vertex3Index">三角面顶点3</param>
    /// <param name="meshVertices">网格顶点坐标数组</param>
    /// <param name="customLines">退化四边形集合</param>
    private void AddCustomLine(int vertex1Index, int vertex2Index, int vertex3Index, Vector3[] meshVertices, List<MeshLine> customLines) 
    {
        Vector3 point1 = meshVertices[vertex1Index];
        Vector3 point2 = meshVertices[vertex2Index];
        MeshLine customLine = new MeshLine(point1, point2);

        if (!customLines.Contains(customLine))  //集合中没有对应的相同边.
        {
            customLine.degraded_rectangle = new DegradedRectangle();
            customLine.degraded_rectangle.vertex1 = vertex1Index;
            customLine.degraded_rectangle.vertex2 = vertex2Index;
            customLine.degraded_rectangle.triangle1_vertex3 = vertex3Index;
            customLine.degraded_rectangle.triangle2_vertex3 = -1;           //没有相同边的退化四边形v2_3值就是-1.(该边叫做 边界边缘)
            customLines.Add(customLine);
        }
        else    //集合中有相同边. 
        {
            int i = customLines.IndexOf(customLine);
            DegradedRectangle rectangle = customLines[i].degraded_rectangle;

            //退化四边形的第二个三角面 顶点3 存储到triangle2_vertex3.
            if (rectangle.triangle2_vertex3 == -1)
            {
                rectangle.triangle2_vertex3 = vertex3Index;
                customLines[i].degraded_rectangle = rectangle;
            }
        }
    }

    /// <summary>
    /// 用于判断、生成退化四边形的类.
    /// </summary>
    private class MeshLine 
    {
        public Vector3 point1;
        public Vector3 point2;
        public DegradedRectangle degraded_rectangle;

        public MeshLine(Vector3 point1, Vector3 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }

        public static bool operator ==(MeshLine line1, MeshLine line2)
        {
            return line1.Equals(line2);
        }

        public static bool operator !=(MeshLine line1, MeshLine line2) 
        {
            return !line1.Equals(line2);
        }

        public override bool Equals(object obj) 
        {
            if (obj == null || GetType() != obj.GetType()) 
            {
                return false;
            }

            MeshLine line2 = (MeshLine)obj;

            if (point1 == line2.point1 && point2 == line2.point2) 
            {
                return true;
            }

            if (point1 == line2.point2 && point2 == line2.point1)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("point1: {0}\npoint2: {1}\nindex.point1: {2}\nindex.point2: {3}\nindex.face_point1: {4}\nindex.face_point2: {5}", point1, point2, degraded_rectangle.vertex1, degraded_rectangle.vertex2, degraded_rectangle.triangle1_vertex3, degraded_rectangle.triangle2_vertex3);
        }
    }

}

