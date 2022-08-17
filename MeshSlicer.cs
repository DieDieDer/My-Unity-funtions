using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer : MeshSlicerSimple
{
    protected class PointData
    {
        public Vector3 pos;
        public int triIndex1;//正反面的位置可能不同? 多宣告一個紀錄?
        public int triIndex2;
        public bool isConcave;//是否為凹邊(背景大於圖形)
        public float angle;
        public bool computed;

        public PointData(Vector3 _pos, int _triIndex1, int _triIndex2)
        {
            pos = _pos;
            triIndex1 = _triIndex1;
            triIndex2 = _triIndex2;
            isConcave = false;
            angle = 0;
            computed = false;
        }

        public bool computeAngle(Plane _plane, Vector3 prevPoint, Vector3 nextPoint)
        {
            angle = Vector3.SignedAngle(nextPoint - pos, pos - prevPoint, _plane.normal);

            if (angle < 0)
                angle += 360;

            isConcave = (angle > 180);
            computed = true;

            return isConcave;
        }
    }

    List<List<PointData>> sliceFacePointDatas;

    public override Transform[] sliceMesh(Plane _plane, Transform obj)
    {
        sliceFacePointDatas = new List<List<PointData>>();
        return base.sliceMesh(_plane, obj);
    }


    protected override void addCutFaceTriangles(bool side, Vector3 newPoint1, Vector3 newPoint2, int[] originalIndexs, int newPointIndexA1, int newPointIndexA2, int newPointIndexB1, int newPointIndexB2, params Vector3[] points)
    {
        Vector3 pointA, pointB;
        int indexA1, indexA2, indexB1, indexB2;
        int newIndexA1, newIndexA2, newIndexB1, newIndexB2;
        int[] indexP1 = new int[] { -1, -1 };
        int[] indexP2 = new int[] { -1, -1 };
        bool containP1 = false;
        bool containP2 = false;
        if (side)
        {
            pointA = newPoint2;
            pointB = newPoint1;
            indexA1 = newPointIndexA1;
            indexA2 = newPointIndexA2;
            indexB2 = newPointIndexB2;
            indexB1 = newPointIndexB1;
            newIndexA1 = newPointIndexA1;
            newIndexA2 = newPointIndexA2;
            newIndexB1 = newPointIndexB1;
            newIndexB2 = newPointIndexB2;
        }
        else
        {
            pointA = newPoint1;
            pointB = newPoint2;
            indexA1 = newPointIndexA2;
            indexA2 = newPointIndexA1;
            indexB2 = newPointIndexB1;
            indexB1 = newPointIndexB2;
            newIndexA1 = newPointIndexB1;
            newIndexA2 = newPointIndexB2;
            newIndexB1 = newPointIndexA1;
            newIndexB2 = newPointIndexA2;
        }

        /* 
        1 陣列為0	    直接加
            ~處理陣列
        2 完全找不到	直接加
        3 找到其中一點	插入另一個點
        4 找到兩個點	黏合
        */

        //  ~處理陣列
        for (int i = 0; i < sliceFacePointDatas.Count; i++)
        {
            for (int j = 0; j < sliceFacePointDatas[i].Count; j++)
            {
                if (sliceFacePointDatas[i][j].pos == pointA)
                {
                    containP1 = true;
                    indexP1 = new int[] { i, j };
                    if (containP1 && containP2) break;
                }
                if (sliceFacePointDatas[i][j].pos == pointB)
                {
                    containP2 = true;
                    indexP2 = new int[] { i, j };
                    if (containP1 && containP2) break;
                }
            }
        }

        if (containP1 && containP2)//結果4 黏合
        {
            if (indexP1[0] != indexP2[0])//要把最後一個頭尾相連的排除
            {
                sliceFacePointDatas[indexP1[0]].AddRange(sliceFacePointDatas[indexP2[0]]);
                sliceFacePointDatas.RemoveAt(indexP2[0]);
            }
        }
        else if (containP1)//結果3 插入另一個點
        {
            sliceFacePointDatas[indexP1[0]].Insert(indexP1[1] + 1, new PointData(pointB, indexA2, indexB2));
        }
        else if (containP2)//結果3 插入另一個點
        {
            sliceFacePointDatas[indexP2[0]].Insert(indexP2[1], new PointData(pointA, indexA1, indexB1));
        }
        else// if (!containP1 && !containP2)//結果1 結果2 直接加
        {
            sliceFacePointDatas.Add(new List<PointData>()
            {
                new PointData(pointA, indexA1, indexB1),
                new PointData(pointB, indexA2, indexB2) }
            );
        }

        addExistTriangle(!side, new int[] { originalIndexs[0], originalIndexs[1], -1 }, new int[] { 0, 0, newIndexB1 },           points[0], points[1], newPoint1);
        addExistTriangle(!side, new int[] { originalIndexs[1], -1, -1 },                new int[] { 0, newIndexB2, newIndexB1 },  points[1], newPoint2, newPoint1);
        addExistTriangle(side,  new int[] { -1, -1, originalIndexs[2] },                new int[] { newIndexA1, newIndexA2, 0 },  newPoint1, newPoint2, points[2]);
    }

    protected override void addCutFace()
    {
        subdividePointDatas();
        
        for (int i = 0; i < sliceFacePointDatas.Count; i++)
        {
            if (sliceFacePointDatas[i].Count > 2)
            {
                //add point0
                meshVertices.Add(sliceFacePointDatas[i][0].pos);
                meshUV.Add(meshUV[sliceFacePointDatas[i][0].triIndex1]);
                meshNormal.Add(-plane.normal);
                otherVertices.Add(sliceFacePointDatas[i][0].pos);
                otherUV.Add(otherUV[sliceFacePointDatas[i][0].triIndex2]);
                otherNormal.Add(plane.normal);
                int index0A = meshVertices.Count - 1;
                int index0B = otherVertices.Count - 1;

                //add point1
                int index1A;
                int index1B;

                //add point2
                meshVertices.Add(sliceFacePointDatas[i][1].pos);
                meshUV.Add(meshUV[sliceFacePointDatas[i][1].triIndex1]);
                meshNormal.Add(-plane.normal);
                otherVertices.Add(sliceFacePointDatas[i][1].pos);
                otherUV.Add(otherUV[sliceFacePointDatas[i][1].triIndex2]);
                otherNormal.Add(plane.normal);
                int index2A = meshVertices.Count - 1;
                int index2B = otherVertices.Count - 1;

                for (int j = 1; j < sliceFacePointDatas[i].Count - 1; j++)
                {
                    index1A = index2A;
                    index1B = index2B;
                    meshVertices.Add(sliceFacePointDatas[i][j + 1].pos);
                    meshUV.Add(meshUV[sliceFacePointDatas[i][j + 1].triIndex1]);
                    meshNormal.Add(-plane.normal);
                    otherVertices.Add(sliceFacePointDatas[i][j + 1].pos);
                    otherUV.Add(otherUV[sliceFacePointDatas[i][j + 1].triIndex2]);
                    otherNormal.Add(plane.normal);
                    index2A = meshVertices.Count - 1;
                    index2B = otherVertices.Count - 1;

                    meshTriangles.Add(index0A);
                    meshTriangles.Add(index1A);
                    meshTriangles.Add(index2A);
                    otherTriangles.Add(index2B);
                    otherTriangles.Add(index1B);
                    otherTriangles.Add(index0B);
                }
            }
        }
    }

    void subdividePointDatas()
    {
        Debug.Log(sliceFacePointDatas.Count);
        int orgCount = sliceFacePointDatas.Count;
        for (int i = 0, c = 0; i < orgCount; c++)
        {
            if (c > 5000)//(不應該發生)
            {
                Debug.LogError("Loop over 5000");
                break;
            }

            bool haveConcave = false;
            for (int j = 0; j < sliceFacePointDatas[i].Count; j++)
            {
                PointData point = sliceFacePointDatas[i][j];
                if (!point.computed) computePoint(point, sliceFacePointDatas[i], j);
                
                if (point.isConcave)
                {
                    debugDrawWireCube(sliceFacePointDatas[i][j].pos, 0.5f, Color.red);
                    Debug.Log(point.angle + " " + point.isConcave);
                    haveConcave = true;
                    float accAngle = point.angle - 360;
                    int orgIndex = j;
                    int connectCount = 0;
                    for (int k = j + 1, c2 = 0; ; k++, c2++)
                    {
                        if (c2 > 5000)//(不應該發生)
                        {
                            Debug.LogError("Loop over 5000");
                            haveConcave = false;
                            break;
                        }
                        if (k == orgIndex)//繞回原點(不應該發生)
                        {
                            Debug.LogError("Concave error: loop to org");
                            haveConcave = false;
                            break;
                        }

                        if (k > sliceFacePointDatas[i].Count - 1) k = 0;
                        PointData point2 = sliceFacePointDatas[i][k];
                        if (!point2.computed) computePoint(point2, sliceFacePointDatas[i], k);
                        if(point2.angle != 0) connectCount++;
                        Debug.Log(point2.angle + " " + point2.isConcave + " " + k);

                        if (point2.isConcave)//遇到另一個凸角
                        {
                            debugDrawWireCube(sliceFacePointDatas[i][k].pos, 0.2f, Color.yellow);
                            //int indexDelta = Mathf.Abs(k - j);
                            //if (indexDelta < 2 || indexDelta == sliceFacePointDatas[i].Count - 1)//遇到的是在相鄰格
                            if (connectCount < 2)//遇到的是在相鄰格
                            {
                                //if (k == j + 1 || (j == sliceFacePointDatas[i].Count - 1 && k == 0))//遇到在下一格
                                {
                                    j = k;
                                    point = point2;
                                    connectCount = 0;
                                    Debug.Log("Reset!");
                                    continue;
                                }
                                //else//遇到在前一格(不應該發生)
                                { Debug.LogError("Concave error: Concave behind point " + k + " " + j + " " + sliceFacePointDatas[i].Count); }
                            }
                            else//正常切割
                            {
                                slicePoints(sliceFacePointDatas[i], j, k);
                                Debug.Log("Slide concave");
                            }

                            break;
                        }
                        else
                        {
                            if (point2.angle != 0) debugDrawWireCube(sliceFacePointDatas[i][k].pos, 0.2f, Color.green);
                            else debugDrawWireCube(sliceFacePointDatas[i][k].pos, 0.15f, Color.blue);
                            accAngle += point2.angle;
                            if (accAngle > 180)//累積角度>180
                            {
                                slicePoints(sliceFacePointDatas[i], j, k);
                                Debug.Log("Slide accAngle");
                                break;
                            }
                        }
                    }//for k

                    break;
                }//if (point.isConcave)
            }//for j

            Debug.Log("------------------------------------------------------------");
            if (haveConcave)
            { continue; }
            else
            { i++; }
        }//for i
    }

    void computePoint(PointData point, List<PointData> pointDatas, int pointIndex)
    {
        int index1 = pointIndex - 1;
        if (index1 < 0) index1 = pointDatas.Count - 1;
        int index2 = pointIndex + 1;
        if (index2 > pointDatas.Count - 1) index2 = 0;
        computePoint(point, pointDatas, pointIndex, index1, index2);
    }
    
    void computePoint(PointData point, List<PointData> pointDatas, int pointIndex, int prevIndex, int nextIndex)
    {
        point.computeAngle(plane, pointDatas[prevIndex].pos, pointDatas[nextIndex].pos);
    }

    void slicePoints(List<PointData> pointList, int startIndex, int endIndex)
    {
        Debug.DrawLine(pointList[startIndex].pos, pointList[endIndex].pos, Color.red);
        if (startIndex == endIndex)
        {
            Debug.LogError("startIndex as same as endIndex: " + startIndex);
            return;
        }
        int indexDelta = Mathf.Abs(startIndex - endIndex);
        if (indexDelta < 2 || indexDelta == pointList.Count - 1)
        {
            Debug.LogError("delta between startIndex and endIndex < 2  -->  " + startIndex + " " + endIndex);
            return;
        }

        if (startIndex < endIndex)
        {
            sliceFacePointDatas.Add(new List<PointData>(pointList.GetRange(startIndex, endIndex - startIndex + 1)));

            int startPrevIndex = startIndex - 1;
            if (startPrevIndex < 0) startPrevIndex = pointList.Count - 1;
            int endNextIndex = endIndex + 1;
            if (endNextIndex > pointList.Count - 1) endNextIndex = 0;

            //切完後重新計算頭尾
            computePoint(pointList[startIndex], pointList, startIndex, startPrevIndex, endIndex);
            computePoint(pointList[endIndex], pointList, endIndex, startIndex, endNextIndex);

            pointList.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
        }
        else
        {
            List<PointData> newList = new List<PointData>();
            newList = pointList.GetRange(startIndex, pointList.Count - startIndex);
            newList.AddRange(pointList.GetRange(0, endIndex + 1));
            sliceFacePointDatas.Add(newList);

            //切完後重新計算頭尾
            computePoint(pointList[startIndex], pointList, startIndex, startIndex - 1, endIndex);
            computePoint(pointList[endIndex], pointList, endIndex, startIndex, endIndex + 1);

            pointList.RemoveRange(startIndex + 1, pointList.Count - startIndex - 1);
            pointList.RemoveRange(0, endIndex);
        }
    }

    void debugDrawWireCube(Vector3 center, float size, Color color)
    {
        float s = size / 2;
        Vector3 point1 = new Vector3(s, s, s) + center;
        Vector3 point2 = new Vector3(s, s, -s) + center;
        Vector3 point3 = new Vector3(s, -s, s) + center;
        Vector3 point4 = new Vector3(s, -s, -s) + center;
        Vector3 point5 = new Vector3(-s, s, s) + center;
        Vector3 point6 = new Vector3(-s, s, -s) + center;
        Vector3 point7 = new Vector3(-s, -s, s) + center;
        Vector3 point8 = new Vector3(-s, -s, -s) + center;
        Debug.DrawLine(point1, point2, color);
        Debug.DrawLine(point1, point3, color);
        Debug.DrawLine(point2, point4, color);
        Debug.DrawLine(point3, point4, color);
        Debug.DrawLine(point5, point6, color);
        Debug.DrawLine(point5, point7, color);
        Debug.DrawLine(point6, point8, color);
        Debug.DrawLine(point7, point8, color);
        Debug.DrawLine(point1, point5, color);
        Debug.DrawLine(point2, point6, color);
        Debug.DrawLine(point3, point7, color);
        Debug.DrawLine(point4, point8, color);
    }
}
