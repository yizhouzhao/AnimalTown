using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AApple : AFood
{
    public float eatTime;
    public float eneryGain;
    // Start is called before the first frame update
    void AWake()
    {
        this.objectType = EPickupObject.Apple;
        eatTime = 2f;
        eneryGain = 0.2f;
    }

    //Eat food
    public override void Eat(AnimalCharacter animalCharacter)
    {
        //Empty hand
        animalCharacter.bHoldObject = false;
        animalCharacter.holdObject = null;

        Debug.Log("ATree: CollectFruit");
        StartCoroutine(EatApple());

        IEnumerator EatApple()
        {
            yield return new WaitForSeconds(this.eatTime);

            animalCharacter.animator.SetInteger("animation", 0);

        }
    }
}
