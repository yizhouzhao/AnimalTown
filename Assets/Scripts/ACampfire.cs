﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACampfire : ASceneTool
{
    void Awake()
    {
        base.AWake();
        this.toolName = "Campfire";
        this.toolType = ESceneEventTool.Fire;
        this.activityType = EActivity.Cook;
    }

    public override void Interact(AnimalCharacter animalCharacter)
    {

        //Set Activity
        animalCharacter.currentActivity = EActivity.Cook;
        //NEED COOK ANIMATION !!!!!!!!!!
        animalCharacter.animator.SetInteger("animation", 5);

        animalCharacter.StopMove();

        //Action
        //Debug.Log("ACampfire: Cook");
        StartCoroutine(Cook());
        IEnumerator Cook()
        {
            if (animalCharacter.holdObject != null)
            {
                AFood food = animalCharacter.holdObject as AFood;
                if (food && food.cookable)
                {
                    Debug.Log("ACampfire Cook:" + food.objectType.ToString());
                    yield return new WaitForSeconds(food.cookTime);

                    //After cook
                    food.cookable = false;
                    food.fullGain += 0.3f;

                    //Change material
                    //Color foodColor = food.gameObject.GetComponent<Renderer>().material.color;
                    //food.gameObject.GetComponent<Renderer>().material.color =
                    //    new Color(foodColor.r / 2f, foodColor.g / 2f, foodColor.b / 2, foodColor.a);

                }
            }

            animalCharacter.SetIdle();
        }
    }

}
