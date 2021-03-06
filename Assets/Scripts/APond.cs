﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APond : ASceneTool
{
    //Fruit to generate over a period
    [Header("Fruit information")]
    public GameObject fruitPrefab;
    public float fruitGeneratingTime;
    private float fruitGeneratingTimeRemain;
    public int fruitCount;
    public int maxFruitCount = 10;

    void Awake()
    {
        base.AWake();
        //Tool type
        this.toolName = "Pond";
        this.toolType = ESceneEventTool.Pond;
        this.activityType = EActivity.CollectFruit;
        this.activityDuration = EAnimalIslandDefinitions.collectFishTime;
        this.maxCapacity = EAnimalIslandDefinitions.pondCapacity;
    }

    void Start()
    {
        //Tree info
        this.fruitCount = 0;
        this.fruitGeneratingTime = EAnimalIslandDefinitions.fishGrowTime;
        this.fruitGeneratingTimeRemain = this.fruitGeneratingTime;
    }

    public override void Interact(AnimalCharacter animalCharacter)
    {
        animalCharacter.StopMove();
        //Set Activity
        animalCharacter.currentActivity = EActivity.CollectFruit;
        animalCharacter.animator.SetInteger("animation", 3);

        //Action
        //Debug.Log("APond: Collect fish");
        StartCoroutine(CollectFish());
        IEnumerator CollectFish()
        {
            yield return new WaitForSeconds(this.activityDuration);

            animalCharacter.SetIdle();

            //Collect Fruit
            if (fruitCount > 0) //still have fruit
            {
                foreach (Transform childTransform in transform)
                {
                    APickupObject pickupObject = childTransform.GetComponent<APickupObject>();
                    if (pickupObject)
                    {
                        fruitCount--;
                        //if character do not have object in hand
                        if (!animalCharacter.bHoldObject)
                        {
                            pickupObject.Pickup(animalCharacter);
                        }
                        else //hold an object already, nothing happens
                        {
                            fruitCount++;
                            //pickupObject.MakeDynamic();
                            //pickupObject.occupied = false;
                        }
                        break;
                    }

                }
            }
        }
    }

    void Update()
    {
        fruitGeneratingTimeRemain -= Time.deltaTime;
        if (fruitGeneratingTimeRemain < 0f)
        {
            if (fruitCount <= maxFruitCount)
            {
                //Generate fruit
                GameObject fruit = Instantiate(fruitPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                APickupObject pickupObject = fruit.GetComponent<APickupObject>();
                pickupObject.MakeStatic();
                pickupObject.occupied = true;
                fruit.transform.parent = this.transform;
                Vector3 boxSize = GetComponent<BoxCollider>().size;

                fruit.transform.localPosition = new Vector3(Random.Range(-boxSize.x / 6, boxSize.x / 6f),
                    Random.Range(-boxSize.y/4, 0), Random.Range(-boxSize.z / 6, boxSize.z / 6));
                fruitCount++;
            }
            fruitGeneratingTimeRemain = fruitGeneratingTime;
        }

    }
}
