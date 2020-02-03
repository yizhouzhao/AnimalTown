using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFish : AFood
{
    void AWake()
    {
        this.objectType = EPickupObject.Fish;
        this.cookTime = 1f; //for test only
        this.fullGain = 0.2f;
    }
}
