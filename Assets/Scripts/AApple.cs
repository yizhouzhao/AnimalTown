using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : AFood
{
    // Start is called before the first frame update
    void Start()
    {
        this.objectType = EPickupObject.Apple;
        eatTime = 2f;
        fullGain = 0.2f;
    }
}
