using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATree : ASceneTool
{
    //Fruit to generate over a period
    [Header("Fruit information")]
    public GameObject fruitPrefab;
    public float fruitGeneratingTime = 1f;
    private float fruitGeneratingTimeRemain;
    public int fruitCount;
    public int maxFruitCount = 10;
    
    void Awake()
    {   
        //Tool type
        this.toolName = "Tree";
        this.toolType = ESceneEventTool.Tree;
        this.activityType = EActivity.CollectFruit;
        this.activityDuration = 2f;
    }

    void Start()
    {
        //Tree info
        this.fruitCount = 0;
        this.fruitGeneratingTimeRemain = this.fruitGeneratingTime;
    }

    public override void Interact(AnimalCharacter animalCharacter)
    {
        if (animalCharacter.navControl)
        {
            animalCharacter.navControl.agent.speed = 0;
            animalCharacter.navControl.agent.angularSpeed = 0;
        }
        //Set Activity
        animalCharacter.currentActivity = EActivity.CollectFruit;
        animalCharacter.animator.SetInteger("animation", 3);

        //Action
        Debug.Log("ATree: CollectFruit");
        StartCoroutine(CollectFruit());
        IEnumerator CollectFruit()
        {
            yield return new WaitForSeconds(this.activityDuration);

            animalCharacter.SetIdle();

            //Collect Fruit
            if(fruitCount > 0) //still have fruit
            {
                foreach(Transform childTransform in transform)
                {
                    APickupObject pickupObject = childTransform.GetComponent<APickupObject>();
                    if(pickupObject)
                    {
                        fruitCount--;
                        //if character do not have object in hand
                        if(!animalCharacter.bHoldObject)
                        {
                            pickupObject.Pickup(animalCharacter);
                        }
                        else //hold an object already
                        {
                            pickupObject.MakeDynamic();
                            pickupObject.occupied = false;
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
        if(fruitGeneratingTimeRemain < 0f)
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

                fruit.transform.localPosition = new Vector3(Random.Range(-boxSize.x / 4, boxSize.x / 4f),
                    Random.Range(boxSize.y / 3, boxSize.y), Random.Range(-boxSize.z / 4, boxSize.z / 4f));
                
                fruitCount++;
            }
            fruitGeneratingTimeRemain = fruitGeneratingTime;
        }
        
    }
}
