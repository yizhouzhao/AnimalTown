using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    private Transform player;
    private Vector3 offsetPosition;
    private bool isRotating = false;


    public float distance = 0;
    public float scrollSpeed = 10;
    public float rotateSpeed = 2;

    // Use this for initialization
    void Start()
    {
        player = GameObject.Find("Player").transform;
        transform.LookAt(player.position);
        offsetPosition = transform.position - player.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = offsetPosition + player.position;
        RotateView();
        ScrollView();
    }

    void ScrollView()
    {
        //print(Input.GetAxis("Mouse ScrollWheel"));
        distance = offsetPosition.magnitude;
        distance += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        distance = Mathf.Clamp(distance, 2, 18);
        offsetPosition = offsetPosition.normalized * distance;
    }

    void RotateView()
    {
        //Input.GetAxis("Mouse X");
        //Input.GetAxis("Mouse Y");
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            transform.RotateAround(player.position, player.up, rotateSpeed * Input.GetAxis("Mouse X"));

            Vector3 originalPos = transform.position;
            Quaternion originalRotation = transform.rotation;

            transform.RotateAround(player.position, transform.right, -rotateSpeed * Input.GetAxis("Mouse Y"));
            float x = transform.eulerAngles.x;
            if (x < 10 || x > 80)
            {
                transform.position = originalPos;
                transform.rotation = originalRotation;
            }

        }

        offsetPosition = transform.position - player.position;
    }
}
