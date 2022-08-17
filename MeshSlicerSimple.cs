using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlicerSimple
{
    protected List<int> meshTriangles = new List<int>();
    protected List<Vector3> meshVertices = new List<Vector3>();
    protected List<Vector3> meshNormal = new List<Vector3>();
    protected List<Vector2> meshUV = new List<Vector2>();
    protected List<int> meshVerticesIndex = new List<int>();

    protected List<int> otherTriangles = new List<int>();
    protected List<Vector3> otherVertices = new List<Vector3>();
    protected List<Vector3> otherNormal = new List<Vector3>();
    protected List<Vector2> otherUV = new List<Vector2>();
    protected List<int> otherVerticesIndex = new List<int>();


    protected List<int> SelectTriangles(bool index) { return index ? meshTriangles : otherTriangles; }
    protected List<Vector3> SelectVertices(bool index) { return index ? meshVertices : otherVertices; }
    protected List<Vector3> SelectNormal(bool index) { return index ? meshNormal : otherNormal; }
    protected List<Vector2> SelectUV(bool index) { return index ? meshUV : otherUV; }
    protected List<int> SelectVerticesIndex(bool index) { return index ? meshVerticesIndex : otherVerticesIndex; }

    protected List<int> sliceFaceEdges1 = new List<int>();
    protected List<int> sliceFaceEdges2 = new List<int>();

    protected Mesh mesh;
    protected Plane plane;

    public virtual Transform[] sliceMesh(Plane _plane, Transform obj)
    {
        //宣告&取得
        meshTriangles = new List<int>();
        meshVertices = new List<Vector3>();
        meshNormal = new List<Vector3>();
        meshUV = new List<Vector2>();

        otherTriangles = new List<int>();
        otherVertices = new List<Vector3>();
        otherNormal = new List<Vector3>();
        otherUV = new List<Vector2>();

        mesh = obj.GetComponent<MeshFilter>().mesh;
        plane = _plane;

        //偵測
        int meshLength = mesh.triangles.Length;

        for (int vertIndex = 0; vertIndex < meshLength; vertIndex += 3)
        {
            int vert1Index = mesh.triangles[vertIndex];
            int vert2Index = mesh.triangles[vertIndex + 1];
            int vert3Index = mesh.triangles[vertIndex + 2];
            Vector3 vert1 = mesh.vertices[vert1Index];
            Vector3 vert2 = mesh.vertices[vert2Index];
            Vector3 vert3 = mesh.vertices[vert3Index];

            bool sideResult1 = plane.GetSide(vert1);
            bool sideResult2 = plane.GetSide(vert2);
            bool sideResult3 = plane.GetSide(vert3);

            if (sideResult1 == sideResult2 && sideResult2 == sideResult3)//全同邊
            {
                int[] orgIndexs = new int[] { vert1Index, vert2Index, vert3Index };
                if (sideResult1)//在正面
                {
                    addExistTriangle(meshTriangles, meshVertices, meshNormal, meshUV, meshVerticesIndex, orgIndexs, new int[] { 0, 0, 0 }, vert1, vert2, vert3);
                }
                else//在反面
                {
                    addExistTriangle(otherTriangles, otherVertices, otherNormal, otherUV, otherVerticesIndex, orgIndexs, new int[] { 0, 0, 0 }, vert1, vert2, vert3);
                }
            }
            else //偵測不同邊，直接切了
            {
                //刪除一個tri，插入兩個vert，插入三個tri
                if (sideResult1 == sideResult2)
                {
                    Vector3 newPoint1 = sliceEdge(plane, vert1, vert3, vert1Index, vert3Index, out int newPointIndexA1, out int newPointIndexB1);
                    Vector3 newPoint2 = sliceEdge(plane, vert2, vert3, vert2Index, vert3Index, out int newPointIndexA2, out int newPointIndexB2);

                    addCutFaceTriangles(sideResult3, newPoint1, newPoint2, new int[] { vert1Index, vert2Index, vert3Index }, newPointIndexA1, newPointIndexA2, newPointIndexB1, newPointIndexB2, vert1, vert2, vert3);
                }
                else if (sideResult2 == sideResult3)
                {
                    Vector3 newPoint1 = sliceEdge(plane, vert2, vert1, vert2Index, vert1Index, out int newPointIndexA1, out int newPointIndexB1);
                    Vector3 newPoint2 = sliceEdge(plane, vert3, vert1, vert3Index, vert1Index, out int newPointIndexA2, out int newPointIndexB2);

                    addCutFaceTriangles(sideResult1, newPoint1, newPoint2, new int[] { vert2Index, vert3Index, vert1Index }, newPointIndexA1, newPointIndexA2, newPointIndexB1, newPointIndexB2, vert2, vert3, vert1);
                }
                else //  (sideResult1 == sideResult3)
                {
                    Vector3 newPoint1 = sliceEdge(plane, vert3, vert2, vert3Index, vert2Index, out int newPointIndexA1, out int newPointIndexB1);
                    Vector3 newPoint2 = sliceEdge(plane, vert1, vert2, vert1Index, vert2Index, out int newPointIndexA2, out int newPointIndexB2);

                    addCutFaceTriangles(sideResult2, newPoint1, newPoint2, new int[] { vert3Index, vert1Index, vert2Index }, newPointIndexA1, newPointIndexA2, newPointIndexB1, newPointIndexB2, vert3, vert1, vert2);
                }
            }
        }

        addCutFace();

        Mesh tempMesh1 = new Mesh();
        Mesh tempMesh2 = new Mesh();

        Transform otherObj = null;
        if (meshTriangles.Count > 0 && otherTriangles.Count > 0)
        {
            //貼上數值
            meshNormal.ForEach(x => { x = -x; });
            tempMesh1.vertices = meshVertices.ToArray();
            tempMesh1.triangles = meshTriangles.ToArray();
            tempMesh1.normals = meshNormal.ToArray();
            tempMesh1.uv = meshUV.ToArray();

            otherNormal.ForEach(x => { x = -x; });
            tempMesh2.vertices = otherVertices.ToArray();
            tempMesh2.triangles = otherTriangles.ToArray();
            tempMesh2.normals = otherNormal.ToArray();
            tempMesh2.uv = otherUV.ToArray();

            //創造第二物件
            otherObj = GameObject.Instantiate(obj.gameObject).transform;
            obj.GetComponent<MeshFilter>().mesh = tempMesh1;
            otherObj.GetComponent<MeshFilter>().mesh = tempMesh2;

            //改變碰撞
            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            meshCollider.sharedMesh = tempMesh1;
            MeshCollider otherCollider = otherObj.GetComponent<MeshCollider>();
            otherCollider.sharedMesh = tempMesh2;
        }

        return new Transform[] { obj, otherObj };
    }

    protected virtual void addCutFaceTriangles(bool side, Vector3 newPoint1, Vector3 newPoint2, int[] originalIndexs, int newPointIndexA1, int newPointIndexA2, int newPointIndexB1, int newPointIndexB2, params Vector3[] points)
    {
        int indexA1, indexA2, indexB1, indexB2;
        int newIndexA1, newIndexA2, newIndexB1, newIndexB2;
        if (side)
        {
            indexA1 = newPointIndexA2;
            indexA2 = newPointIndexA1;
            indexB1 = newPointIndexB1;
            indexB2 = newPointIndexB2;
            newIndexA1 = newPointIndexA1;
            newIndexA2 = newPointIndexA2;
            newIndexB1 = newPointIndexB1;
            newIndexB2 = newPointIndexB2;
        }
        else
        {
            indexA1 = newPointIndexA1;
            indexA2 = newPointIndexA2;
            indexB1 = newPointIndexB2;
            indexB2 = newPointIndexB1;
            newIndexA1 = newPointIndexB1;
            newIndexA2 = newPointIndexB2;
            newIndexB1 = newPointIndexA1;
            newIndexB2 = newPointIndexA2;
        }
        sliceFaceEdges1.Add(indexA1);
        sliceFaceEdges1.Add(indexA2);
        sliceFaceEdges2.Add(indexB1);
        sliceFaceEdges2.Add(indexB2);

        addExistTriangle(!side, new int[] { originalIndexs[0], originalIndexs[1], -1 }, new int[] { 0, 0, newIndexB1 }, points[0], points[1], newPoint1);
        addExistTriangle(!side, new int[] { originalIndexs[1], -1, -1 }, new int[] { 0, newIndexB2, newIndexB1 }, points[1], newPoint2, newPoint1);
        addExistTriangle(side, new int[] { -1, -1, originalIndexs[2] }, new int[] { newIndexA1, newIndexA2, 0 }, newPoint1, newPoint2, points[2]);
    }

    protected void addExistTriangle(List<int> _triangles, List<Vector3> _vertices, List<Vector3> _normals, List<Vector2> _uvs, List<int> _orgIndex, int[] originalIndexs, int[] newPointIndexs, params Vector3[] points)
    {
        detectTriangleVert(_triangles, _vertices, _normals, _uvs, _orgIndex, originalIndexs[0], newPointIndexs[0], points[0]);
        detectTriangleVert(_triangles, _vertices, _normals, _uvs, _orgIndex, originalIndexs[1], newPointIndexs[1], points[1]);
        detectTriangleVert(_triangles, _vertices, _normals, _uvs, _orgIndex, originalIndexs[2], newPointIndexs[2], points[2]);
    }

    protected void addExistTriangle(bool side, int[] originalIndex, int[] newPointIndexs, params Vector3[] points)
    {
        detectTriangleVert(SelectTriangles(side), SelectVertices(side), SelectNormal(side), SelectUV(side), SelectVerticesIndex(side), originalIndex[0], newPointIndexs[0], points[0]);
        detectTriangleVert(SelectTriangles(side), SelectVertices(side), SelectNormal(side), SelectUV(side), SelectVerticesIndex(side), originalIndex[1], newPointIndexs[1], points[1]);
        detectTriangleVert(SelectTriangles(side), SelectVertices(side), SelectNormal(side), SelectUV(side), SelectVerticesIndex(side), originalIndex[2], newPointIndexs[2], points[2]);
    }

    protected void detectTriangleVert(List<int> _triangles, List<Vector3> _vertices, List<Vector3> _normals, List<Vector2> _uvs, List<int> _orgIndex, int originalIndex, int newPointIndex, Vector3 point)
    {
        //int index = listIndexOf(_vertices, point);
        int index = _orgIndex.IndexOf(originalIndex);
        if (index == -1)//找不到點
        {
            if (originalIndex == -1)//newPoint1 2
            {
                _triangles.Add(newPointIndex);
            }
            else
            {
                _triangles.Add(_vertices.Count);
                _vertices.Add(point);

                _orgIndex.Add(originalIndex);

                //int vertIndex = arrayIndexOf(mesh.vertices, point);
                _normals.Add(mesh.normals[originalIndex]);
                _uvs.Add(mesh.uv[originalIndex]);
            }
        }
        else//點存在
        {
            _triangles.Add(index);
        }
    }

    protected Vector3 sliceEdge(Plane plane, Vector3 point1, Vector3 point2, int point1Index, int point2Index, out int newPointIndex1, out int newPointIndex2)
    {
        Ray ray = new Ray(point1, (point2 - point1));
        plane.Raycast(ray, out float distance);
        Vector3 slicePoint = ray.GetPoint(distance);

        addNewVertice(point1, point2, point1Index, point2Index, slicePoint, distance / (point2 - point1).magnitude, out newPointIndex1, out newPointIndex2);
        return slicePoint;
    }

    protected void addNewVertice(Vector3 point1, Vector3 point2, int point1Index, int point2Index, Vector3 newPoint, float _distance, out int newPointIndex1, out int newPointIndex2)
    {
        //製造新點
        Vector3 newNormal = (mesh.normals[point1Index] + mesh.normals[point2Index]) / 2;
        Vector2 newUV = Vector2.Lerp(mesh.uv[point1Index], mesh.uv[point2Index], _distance);
        meshVertices.Add(newPoint);
        otherVertices.Add(newPoint);
        meshNormal.Add(newNormal);
        otherNormal.Add(newNormal);
        meshUV.Add(newUV);
        otherUV.Add(newUV);
        newPointIndex1 = meshVertices.Count - 1;
        newPointIndex2 = otherVertices.Count - 1;

        meshVerticesIndex.Add(-2);
        otherVerticesIndex.Add(-2);
    }

    protected virtual void addCutFace()
    {
        meshNormal.Add(-plane.normal);
        otherNormal.Add(plane.normal);
        meshUV.Add(Vector2.zero);
        otherUV.Add(Vector2.zero);

        Vector3 centerPoint = Vector3.zero;
        int mvc = meshVertices.Count;
        int ovc = otherVertices.Count;
        for (int i = 0; i < sliceFaceEdges1.Count; i += 2)
        {
            centerPoint += meshVertices[sliceFaceEdges1[i]];
            centerPoint += meshVertices[sliceFaceEdges1[i + 1]];
            meshTriangles.Add(sliceFaceEdges1[i]);
            meshTriangles.Add(sliceFaceEdges1[i + 1]);
            meshTriangles.Add(mvc);
        }

        for (int i = 0; i < sliceFaceEdges2.Count; i += 2)
        {
            otherTriangles.Add(sliceFaceEdges2[i]);
            otherTriangles.Add(sliceFaceEdges2[i + 1]);
            otherTriangles.Add(ovc);
        }

        centerPoint /= sliceFaceEdges1.Count;

        meshVertices.Add(centerPoint);
        otherVertices.Add(centerPoint);
    }
}
