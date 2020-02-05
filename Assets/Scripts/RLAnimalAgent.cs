using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class RLAnimalAgent : Agent
{
    public AnimalCharacter animalCharacter;

    public override void InitializeAgent()
    {
        animalCharacter = GetComponent<AnimalCharacter>();
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

    public override void AgentAction(float[] vectorAction)
    {

    }

    public override void AgentReset()
    {

    }
}
