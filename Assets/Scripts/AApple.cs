using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : ASeed
{
    // Start is called before the first frame update
    void Start()
    {
        this.objectType = EPickupObject.Apple;
        this.cookTime = 1f; //for test only
        this.fullGain = 0.2f;
        this.eatTime = 2f;
        this.growTime = 1f;
        this.price = 1f;
        this.stayFreshTime = 100f;
    }
}
