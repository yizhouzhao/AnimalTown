using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentNavigationControl : MonoBehaviour
{
    public NavMeshAgent agent;
    public string testTravelTo = "";
    private bool _calculatingPath;
    public float originalSpeed;
    public float originalAngularSpeed;

    // Start is called before the first frame update
    void Start()
    {
        //Test navigation
        if (testTravelTo != "")
        {
            TravelTo(GameObject.FindWithTag("Player").transform.position);
        }

        originalAngularSpeed = agent.speed;
        originalAngularSpeed = agent.angularSpeed;

    }

    // Update is called once per frame
    void Update()
    {
        //if (testTravelTo != "")
        //{
        //    TravelTo(GameObject.FindWithTag("Player").transform.position);
        //}

        //Debug.Log("AgentNavCtrl: " + IsDoneTraveling().ToString());
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
