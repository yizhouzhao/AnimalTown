using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class RLAgentTest : Agent
{
    Rigidbody rBody;
    public override void InitializeAgent()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void CollectObservations()
    {
        AddVectorObs(gameObject.transform.position);
        AddVectorObs(gameObject.transform.rotation.z);
        AddVectorObs(gameObject.transform.rotation.x);
    }

    public override float[] Heuristic()
    {
        var action = new float[2];
        action[0] = Input.GetAxis("Horizontal");
        action[1] = Input.GetAxis("Vertical");
        return action;
    }

    public float speed = 10;
    public override void AgentAction(float[] vectorAction)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = vectorAction[0];
        controlSignal.z = vectorAction[1];
        rBody.AddForce(controlSignal * speed);

        if (gameObject.transform.position.x - 140 < -10f || gameObject.transform.position.x - 140 > 10f ||
            gameObject.transform.position.z - 150< -10f || gameObject.transform.position.z - 150f > 10f)
        {
            Done();
            AddReward(-1f);
        }
        else
        {
            AddReward(0.1f);
        }
    }

    public override void AgentReset()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.position = new Vector3(140, 5, 150);
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
    }
}