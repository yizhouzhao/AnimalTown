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
        //Set Activity
        animalCharacter.currentActivity = EActivity.CollectFruit;
        animalCharacter.animator.SetInteger("animation", 3);

        //Action
        Debug.Log("ATree: CollectFruit");
        StartCoroutine(SleepNow());
        IEnumerator SleepNow()
        {
            yield return new WaitForSeconds(this.activityDuration);

            animalCharacter.animator.SetInteger("animation", 0);
            
            animalCharacter.bInActivity = false;
        }
    }

    void Update()
    {
        fruitGeneratingTimeRemain -= Time.deltaTime;
        if(fruitGeneratingTimeRemain < 0f)
        {
            //Generate fruit
            GameObject fruit = Instantiate(fruitPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            fruit.GetComponent<APickupObject>().MakeStatic();
            fruit.transform.parent = this.transform;
            Vector3 boxSize = GetComponent<BoxCollider>().size;

            fruit.transform.localPosition = new Vector3(Random.Range(-boxSize.x/4, boxSize.x/4f),
                Random.Range(boxSize.y / 3, boxSize.y), Random.Range(-boxSize.z / 4, boxSize.z / 4f));
            fruitCount++;
           
            fruitGeneratingTimeRemain = fruitGeneratingTime;
        }
        
    }
}
