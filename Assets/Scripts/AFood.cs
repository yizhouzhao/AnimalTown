using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFood : APickupObject
{
    public float eatTime = 2f; //time to eat this food
    public float fullGain = 0.2f; //fullness gain after eating the food
    
    public bool cookable = true; //able to cook?
    public float cookTime = 1f; //time to cook

    void Awake()
    {
        this.eatable = true;
    }

    //Eat food
    public void Eat(AnimalCharacter animalCharacter)
    {
        //Empty hand
        animalCharacter.bHoldObject = false;
        animalCharacter.holdObject = null;

        Debug.Log("AFood(eat): " + this.objectType.ToString());
        animalCharacter.currentActivity = EActivity.Eat;
        StartCoroutine(EatApple());

        //Eat animation
        animalCharacter.animator.SetInteger("animation", 4);
        IEnumerator EatApple()
        {
            yield return new WaitForSeconds(this.eatTime);

            animalCharacter.bInActivity = false;
            animalCharacter.currentActivity = EActivity.Idle;
            animalCharacter.animator.SetInteger("animation", 0);

            //Gain fullness
            animalCharacter.fullness = Mathf.Min(animalCharacter.fullness + fullGain, 1f);

            //Distroy this food
            Destroy(this.gameObject);
        }
    }
}
