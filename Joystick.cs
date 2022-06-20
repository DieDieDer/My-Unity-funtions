using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Joystick : MonoBehaviour
{
    public Vector2 InputAxis;
    [SerializeField] float radius = 200; //�ۦ�վ�

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
        if (Input.GetMouseButtonDown(0) && !actived && onRange(Input.GetTouch(Input.touchCount - 1).position)) //�b�d�� & �S��L��b�ޱ�
        {
            actived = true;
            usingTouchIndex = Input.touchCount - 1;
        }

        if (Input.GetMouseButtonUp(0))
        {
            //�񱼪��O���@����
            for(int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Ended)
                {
                    if (i == usingTouchIndex) //�񱼪��O��������
                    {
                        actived = false;
                        stick.localPosition = Vector3.zero;
                        InputAxis = Vector2.zero;
                    }
                    else if (i < usingTouchIndex) //��L
                    {
                        usingTouchIndex--;
                        i--;
                    }
                }
            }
        }

        if(actived) usingStick();
    }

    bool onRange(Vector2 position) //�O�_���b�d��
    {
        return Vector2.Distance(position, myPos) < radius;
    }

    void usingStick() //�p��InputAxis & Stick�̲צ�m
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
