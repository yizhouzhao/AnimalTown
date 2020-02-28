using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;


public class RLPolice : Agent
{
    public EPoliceThiefEnvConfig envConfig;

    public Rigidbody rigidBody;

    // Start is called before the first frame update
    public override void InitializeAgent()
    {
        rigidBody = this.GetComponent<Rigidbody>();
    }

    public override void CollectObservations()
    {
        foreach(Pedestrain pedestrain in envConfig.pedestrains)
        {
            AddVectorObs(pedestrain.gameObject.transform.position.x / EPoliceThiefEnv.landSize);
            AddVectorObs(pedestrain.gameObject.transform.position.z / EPoliceThiefEnv.landSize);
            AddVectorObs(pedestrain.rigidBody.velocity.x);
            AddVectorObs(pedestrain.rigidBody.velocity.z);
        }

        AddVectorObs(gameObject.transform.position.x / EPoliceThiefEnv.landSize);
        AddVectorObs(gameObject.transform.position.z / EPoliceThiefEnv.landSize);
        AddVectorObs(rigidBody.velocity.x);
        AddVectorObs(rigidBody.velocity.z);

        foreach (ThiefAgent thiefAgent in envConfig.thieves)
        {
            AddVectorObs(thiefAgent.gameObject.transform.position.x / EPoliceThiefEnv.landSize);
            AddVectorObs(thiefAgent.gameObject.transform.position.z / EPoliceThiefEnv.landSize);
            AddVectorObs(thiefAgent.rigidBody.velocity.x);
            AddVectorObs(thiefAgent.rigidBody.velocity.z);
        }

    }


    public override float[] Heuristic()
    {
        var action = new float[2];
        action[0] = Input.GetAxis("Horizontal");
        action[1] = Input.GetAxis("Vertical");
        return action;
    }

    public override void AgentAction(float[] vectorAction)
    {


    }

    public override void AgentReset()
    {
    }
}
