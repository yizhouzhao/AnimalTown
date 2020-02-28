using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pedestrain : PTAgent
{
    // Start is called before the first frame update
    void Start()
    {
        lastActionIndex = UnityEngine.Random.Range(0, 4);
    }

    void Act()
    {
        float prob = UnityEngine.Random.Range(0f, 1f);
        int offset = (prob < 0.3f) ? -1 : (prob < 0.7f) ? 0 : 1;
        this.lastActionIndex = (this.lastActionIndex + offset) % (actions.Length / 2 );
        this.lastActionIndex = this.lastActionIndex < 0 ? this.lastActionIndex + (actions.Length / 2) : this.lastActionIndex;
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
