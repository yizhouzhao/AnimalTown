﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : APickupObject
{
    // Start is called before the first frame update
    void AWake()
    {
        this.objectType = EPickupObject.Apple;
        this.eatable = true;
    }
}
