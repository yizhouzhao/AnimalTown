using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : ASeed
{
    // Start is called before the first frame update
    void Start()
    {
        this.objectType = EPickupObject.Apple;
        this.cookTime = EAnimalIslandDefinitions.appleCookTime; //for test only
        this.fullGain = EAnimalIslandDefinitions.appleFullGain;
        this.eatTime = EAnimalIslandDefinitions.appleEatTime;
        this.growTime = EAnimalIslandDefinitions.appleSeedGrowTime;
        this.price = EAnimalIslandDefinitions.applePrice;
        this.stayFreshTime = EAnimalIslandDefinitions.appleStayFreshTime;
    }
}
