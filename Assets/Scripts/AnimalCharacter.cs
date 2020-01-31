using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalCharacter : MonoBehaviour
{
    [Header("Basic information")]
    public EAnimalType animalType;
    public string characterName;
    public int age;
    public string chararcterDescription;

    [Header("Skills")]
    public float structured;
    public float motorSkill;
    public float artCraftSkill;

    //Animation and nevigation*
    public Animator animator;

    [Header("Status(Fluents)")]
    public float energy; //tired or excited
    public float fullness; //hungry or full

    [Header("Activity")]
    public ASceneTool sceneTool;//Scenetool Reference: to record what scene tool the character meets
    public float currentActivityRemainTime;
    public EActivity currentActivity;
    public bool bInActivity;

    [Header("Object")]
    public bool bHoldObject;
    public APickupObject meetPickupObject;
    public Transform holdTransform; //position to hold this object
    public APickupObject holdObject;


    // Start is called before the first frame update
    void Start()
    {
        //Set up animation
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.SetInteger("animation", 0);

        //Set up hold transform to hold the pick up object
        foreach (Transform eachChild in transform)
        {
            if (eachChild.name == "HoldTransform")
            {
                holdTransform = eachChild;
                break;
            }
        }

        if(holdTransform == null)
        {
            Debug.LogError("No hold transform for player/agent");
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            print("Interact!!!");
            ActWithSceneTool();
        }
        if (Input.GetKeyDown("e"))
        {
            print("Pickup!!!");
            PickupDropObject();
        }

        if (Input.GetKeyDown("q"))
        {
            print("Use!!!");
            UseObject();
        }
    }

    //Act with Scene tool
    public void ActWithSceneTool()
    {
        if (sceneTool != null)
        {
            this.bInActivity = true;
            sceneTool.Interact(this);
        }
    }

    public void PickupDropObject()
    {
        if(holdObject == null)
        {
            if (meetPickupObject != null)
            {
                meetPickupObject.Pickup(this);
            }
        }
        else
        {
            holdObject.Drop(this);
        }
    }

    public void UseObject()
    {
        if (holdObject != null)
        {
            //If it is food and eatable
            AFood food = holdObject as AFood;
            if (food && food.eatable)
            {
                this.bInActivity = true;
                food.Eat(this);
            }
        }
    }
}
