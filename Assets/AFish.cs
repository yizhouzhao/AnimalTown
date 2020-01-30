using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFish : APickupObject
{
    // Start is called before the first frame update
    void AWake()
    {
        this.eatable = true;
        this.objectType = EPickupObject.Fish;
    }
}
