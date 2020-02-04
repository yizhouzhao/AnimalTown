using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFish : AFood
{
    void Start()
    {
        this.objectType = EPickupObject.Fish;
        this.cookTime = 1f; //for test only
        this.fullGain = 0.2f;
        this.stayFreshTime = 10f;
    }
}
