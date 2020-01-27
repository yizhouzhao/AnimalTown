using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPlayerState { Idle, Moving };

public class PlayerMove : MonoBehaviour
{
    //Direction and speed
    public float speed = 4.0f;
    private PlayerDirection dir;
    public bool isMoving = false;

    //Animation
    EPlayerState _state;
    Animator _animator;

    private CharacterController controller;
    // Start is called before the first frame update
    void Start()
    {
        dir = GetComponent<PlayerDirection>();
        controller = GetComponent<CharacterController>();
        _state = EPlayerState.Idle;
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(dir.targetPosition, transform.position);
        if (distance > 0.3f)
        {
            isMoving = true;
            _state = EPlayerState.Moving;
            controller.SimpleMove(transform.forward * speed);
        }
        else
        {
            isMoving = false;
            _state = EPlayerState.Idle;
        }
    }

    private void LateUpdate()
    {
        if(_state == EPlayerState.Idle)
        {
            _animator.SetInteger("animation", 0);
        }
        else
        {
            //Moving
            _animator.SetInteger("animation", 1);
        }
    }
}
