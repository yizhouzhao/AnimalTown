using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFish : AFood
{
    void Start()
    {
        this.objectType = EPickupObject.Fish;
        this.cookTime = EAnimalIslandDefinitions.fishCookTime; //for test only
        this.fullGain = EAnimalIslandDefinitions.fishFullGain;
        this.stayFreshTime = EAnimalIslandDefinitions.fishStayFreshTime;
    }
}
