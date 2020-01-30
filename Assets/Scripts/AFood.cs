using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFood : APickupObject
{
    void Awake()
    {
        this.eatable = true;
    }

    public virtual void Eat(AnimalCharacter animalCharacter)
    {

    }
}
