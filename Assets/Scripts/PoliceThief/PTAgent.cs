using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTAgent : MonoBehaviour
{
    public Rigidbody rigidBody;
    
    public int lastActionIndex;
    public int[,] actions = new int[4, 2] { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };


    public float thrust = 100.0f;

    // Start is called before the first frame update
    void AWake()
    {
        //rigidBody = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
