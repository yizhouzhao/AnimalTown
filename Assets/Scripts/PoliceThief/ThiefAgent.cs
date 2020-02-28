using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThiefAgent : PTAgent
{
    public float[,] belief;
    public float initialProb = 0.5f;

    public int enemyIndex;
    public EPoliceThiefEnvConfig envConfig;


    // Start is called before the first frame update
    void Start()
    {
        //higher speed than pedestrian and police
        this.thrust *= 2;
        belief = new float[envConfig.worldAgentNum, 2];
        //init belief
        ResetBelief();
    }

    private void ResetBelief()
    {
        for(int i = 0; i < belief.Length / 2; ++i)
        {
            belief[i, 0] = initialProb;
            belief[i, 1] = 1 - belief[i, 0];
        }   
    }

    public void Act()
    {
        float max_0_belief = -1;
        int max_0_index = -1;
        for (int i = 0; i < belief.Length / 2; ++i)
        {
            if(belief[i, 0] >= max_0_belief)
            {
                max_0_belief = belief[i, 0];
                max_0_index = i;
            }
        }

        if(max_0_belief <= 0f)
        {
            lastActionIndex = UnityEngine.Random.Range(0, this.actions.Length / 2);
        }
        else
        {
            enemyIndex = max_0_index;
            MonoBehaviour enemy = envConfig.GetAgent(enemyIndex);

            float max_dist = 0f;
            int bestActionIndex = -1;



            for(int i = 0; i < this.actions.Length / 2; ++i)
            {
                
                Vector3 forceDirection = new Vector3(actions[i, 0], 0, actions[i, 1]);
                Vector3 newVelocity = this.rigidBody.velocity + (forceDirection * thrust) / rigidBody.mass;

                Vector3 nextPosition = this.transform.position + newVelocity * 0.02f; //speed * time + position
                float distance = Vector3.Distance(nextPosition, enemy.transform.position);
                if (distance > max_dist)
                {
                    max_dist = distance;
                    bestActionIndex = i;
                }
            }

        }

    }
    void FixedUpdate()
    {
        Act();
        //print(lastActionIndex);
        Vector3 forceDirection = new Vector3(actions[lastActionIndex, 0], 0, actions[lastActionIndex, 1]);
        rigidBody.AddForce(forceDirection * thrust);

        Vector3 newPosition = Vector3.zero;
        newPosition.x = this.transform.position.x > EPoliceThiefEnv.landSize ? -100f : this.transform.position.x < 0 ? 100f : 0f;
        newPosition.z = this.transform.position.z > EPoliceThiefEnv.landSize ? -100f : this.transform.position.z < 0 ? 100f : 0f;

        this.transform.position += newPosition;
    }
}
