using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTAgent : MonoBehaviour
{
    public Rigidbody rigidBody;
    // Start is called before the first frame update
    void AWake()
    {
        rigidBody = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
