using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : APickupObject
{
    // Start is called before the first frame update
    void Start()
    {
        this.objectType = EPickupObject.Apple;
    }
}
