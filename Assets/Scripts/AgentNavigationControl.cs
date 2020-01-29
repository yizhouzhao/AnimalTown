﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentNavigationControl : MonoBehaviour
{
    public NavMeshAgent agent;
    public string testTravelTo = "";
    private bool _calculatingPath;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (testTravelTo != "")
        {
            TravelTo(GameObject.FindWithTag("Player").transform.position);
        }
    }

    //Travel to player and start conversation
    public void TravelTo(Vector3 location)
    {
        _calculatingPath = true;
        agent.SetDestination(location);
        _calculatingPath = false;
    }

    public bool IsDoneTraveling()
    {
        return _calculatingPath || (agent.remainingDistance <= agent.stoppingDistance);
    }

    private void LateUpdate()
    {
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            // Debug.Log(agent.velocity.sqrMagnitude);
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
        }

    }
}