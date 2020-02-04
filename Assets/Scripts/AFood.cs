using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFood : APickupObject
{
    public float eatTime = 2f; //time to eat this food
    public float fullGain = 0.2f; //fullness gain after eating the food
    
    public bool cookable = true; //able to cook?
    public float cookTime = 1f; //time to cook

    public bool stayFresh;//? whether this food stays fresh? e.g apple on tree or fish in pond
    public float stayFreshTime; //How long this food stays fresh. if pass,


    void Awake()
    {
        this.eatable = true;
    }

    private void Update()
    {
        if (!occupied)
        {
            stayFreshTime -= Time.deltaTime;
            if(stayFreshTime < 0)
            {
                Destroy(this.gameObject);
            }
        }
    }

    //Eat food
    public void Eat(AnimalCharacter animalCharacter)
    {
        //Empty hand
        animalCharacter.bHoldObject = false;
        animalCharacter.holdObject = null;

        animalCharacter.StopMove();

        //Debug.Log("AFood(eat): " + this.objectType.ToString());
        animalCharacter.currentActivity = EActivity.Eat;
        StartCoroutine(EatFood());

        //Eat animation
        animalCharacter.animator.SetInteger("animation", 4);
        IEnumerator EatFood()
        {
            yield return new WaitForSeconds(this.eatTime);

            animalCharacter.SetIdle();

            //Gain fullness
            animalCharacter.fullness = Mathf.Min(animalCharacter.fullness + fullGain, 1f);

            //Distroy this food
            Destroy(this.gameObject);
        }
    }
}
