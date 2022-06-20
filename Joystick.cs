using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Joystick : MonoBehaviour
{
    public Vector2 InputAxis;
    [SerializeField] float radius = 200; //自行調整

    bool actived = false;
    int usingTouchIndex = -1;

    Camera mainCam;
    Transform tran;
    Vector2 myPos;
    Transform stick;

    void Start()
    {
        tran = transform;
        mainCam = Camera.main;
        myPos = tran.position;
        stick = tran.GetChild(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !actived && onRange(Input.GetTouch(Input.touchCount - 1).position)) //在範圍內 & 沒其他手在操控
        {
            actived = true;
            usingTouchIndex = Input.touchCount - 1;
        }

        if (Input.GetMouseButtonUp(0))
        {
            //放掉的是哪一隻手
            for(int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Ended)
                {
                    if (i == usingTouchIndex) //放掉的是按住的那隻
                    {
                        actived = false;
                        stick.localPosition = Vector3.zero;
                        InputAxis = Vector2.zero;
                    }
                    else if (i < usingTouchIndex) //其他
                    {
                        usingTouchIndex--;
                        i--;
                    }
                }
            }
        }

        if(actived) usingStick();
    }

    bool onRange(Vector2 position) //是否按在範圍內
    {
        return Vector2.Distance(position, myPos) < radius;
    }

    void usingStick() //計算InputAxis & Stick最終位置
    {
        Vector2 inputPos = Input.GetTouch(usingTouchIndex).position;
        InputAxis = inputPos - myPos;
        InputAxis /= radius;
        if (InputAxis.magnitude > 1)
        {
            InputAxis = InputAxis.normalized;
            stick.position = InputAxis * radius + myPos;
        }
        else
        {
            stick.position = inputPos;
        }
        
    }
}
