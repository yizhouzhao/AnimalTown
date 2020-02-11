﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System;

public class RLAnimalAgent : Agent
{
    [Header("character")]
    public AnimalCharacter animalCharacter;

    [Header("Done Period")]
    public int secondPerPeriod;

    public int NUM_ITEM_TYPES;
    public int NUM_TOOL_TYPES;
    public int NUM_MEET_TYPES;

    public int GetHoldObjectCode()
    {
        if(animalCharacter.holdObject == null)
            return 0;

        switch (animalCharacter.holdObject.objectType)
        {
            case (EPickupObject.Apple):
                return 1;
            case (EPickupObject.Fish):
                return 2;
            default:
                {
                    return 0;
                    throw new NotImplementedException();
                }
        }   
    }

    public int GetSceneToolCode()
    {
        if (animalCharacter.sceneTool == null)
            return 0;

        switch (animalCharacter.sceneTool.toolType)
        {
            case (ESceneEventTool.House):
                return 1;
            case (ESceneEventTool.Tree):
                return 2;
            case (ESceneEventTool.Farm):
                return 3;
            case (ESceneEventTool.Pond):
                return 4;
            case (ESceneEventTool.Fire):
                return 5;

            default:
                {
                    return 0;
                    throw new NotImplementedException();
                }
        }
    }

    public int GetMeetCharacterCode()
    {
        if (animalCharacter.meetAnimalCharacter == null)
            return 0;
        else
            return 1;
    }

    public override void InitializeAgent()
    {
        //initialize location
        animalCharacter = GetComponent<AnimalCharacter>();
        NUM_ITEM_TYPES = 3;
        NUM_TOOL_TYPES = 6;
        NUM_MEET_TYPES = 2;

        secondPerPeriod = 100;

    }

    public override void CollectObservations()
    {
        //location
        AddVectorObs(gameObject.transform.position.x);
        AddVectorObs(gameObject.transform.position.z);

        //Fluents
        AddVectorObs(animalCharacter.energy);
        AddVectorObs(animalCharacter.fullness);

        //Object 
        AddVectorObs(GetHoldObjectCode(), NUM_ITEM_TYPES);

        //Scene
        AddVectorObs(GetSceneToolCode(), NUM_TOOL_TYPES);

        //Meet character
        AddVectorObs(GetMeetCharacterCode(), NUM_MEET_TYPES);
    }

    public override float[] Heuristic()
    {        
        var action = new float[6];
        action[0] = UnityEngine.Random.Range(0f, 1f);
        action[1] = UnityEngine.Random.Range(0f, 1f);
        action[2] = 0.9f; //UnityEngine.Random.Range(0f, 1f);
        action[3] = 0.6f; // UnityEngine.Random.Range(0f, 1f);
        action[4] = 0.3f; // UnityEngine.Random.Range(0f, 1f);
        action[5] = 0.2f; // UnityEngine.Random.Range(0f, 1f);
        return action;
    }

    public override void AgentAction(float[] vectorAction)
    {
        //Reward
        //if (animalCharacter.fullness < 0.2f || animalCharacter.energy < 0.2f)
        {
            AddReward(-1);
        }

        //if (animalCharacter.fullness > 0.8f || animalCharacter.energy > 0.8f)
        //{
        //    AddReward(1);
        //}

        //Done
        if((int)Time.time % secondPerPeriod == secondPerPeriod - 1)
        {
            Done();
        }

        //SetReward(animalCharacter.money * 0.01f);

        //Activity cool down
        if (!animalCharacter.bInActivity)
            animalCharacter.currentActivityCoolDown -= Time.deltaTime;

        if (animalCharacter.currentActivityCoolDown > 0)
            return;

        float position_x = Mathf.Clamp(vectorAction[0], 0f, 1f);
        float position_z = Mathf.Clamp(vectorAction[1], 0f, 1f);

        float scene_tool_prob = Mathf.Clamp(vectorAction[2], 0f, 1f);
        float pickup_drop_prob = Mathf.Clamp(vectorAction[3], 0f, 1f);
        float use_prob =  Mathf.Clamp(vectorAction[4], 0f, 1f);
        float communicate_prob = Mathf.Clamp(vectorAction[5], 0f, 1f);

        if (UnityEngine.Random.Range(0f, 1f) < scene_tool_prob) 
        {
            animalCharacter.ActWithSceneTool();
        }

        if (UnityEngine.Random.Range(0f, 1f) < communicate_prob) 
        {
            animalCharacter.ActWithAnimalCharacter();
        }

        if (UnityEngine.Random.Range(0f, 1f) < pickup_drop_prob)
        {
            animalCharacter.PickupDropObject();
        }

        if (UnityEngine.Random.Range(0f, 1f) < use_prob)
        {
            animalCharacter.UseObject();
        }

        if (animalCharacter.navControl.IsDoneTraveling())
        {
            animalCharacter.Walk2(position_x, position_z);
        }
    }

    public override void AgentReset()
    {
        animalCharacter.ResetAnimalCharacter();
    }
}
