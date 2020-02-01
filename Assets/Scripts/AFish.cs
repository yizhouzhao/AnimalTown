using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFish : APickupObject
{
    void AWake()
    {
        this.objectType = EPickupObject.Fish;
    }
}
