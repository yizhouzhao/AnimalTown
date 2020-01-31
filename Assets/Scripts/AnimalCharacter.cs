using System;
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
    [HideInInspector]
    public Animator animator;

    [Header("Status(Fluents)")]
    public float energy; //tired or excited
    public float fullness; //hungry or full
    public float money; //money

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

    [Header("Keycode for control")]
    public KeyCode interactKey;
    public KeyCode pickupDropKey;
    public KeyCode useKey;

    [Header("Communication")]
    public AnimalCharacter meetAnimalCharacter; //meet another agent
    public bool agreeCommunication; //whether the two character want to communicate


    // Start is called before the first frame update
    void Start()
    {
        //Set up animation
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.SetInteger("animation", 0);

        holdTransform = this.transform.Find("HoldTransform");

        if(holdTransform == null)
        {
            Debug.LogError("No hold transform for player/agent");
        }

        money = 10f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            ActWithSceneTool();
            ActWithAnimalCharacter();
        }
        if (Input.GetKeyDown(pickupDropKey))
        {
            print("Pickup!!!");
            PickupDropObject();
        }

        if (Input.GetKeyDown(useKey))
        {
            print("Use!!!");
            UseObject();
        }
    }

    private void ActWithAnimalCharacter()
    {
        if (this.meetAnimalCharacter)
        {
            Debug.Log("Animal Character Interact with another character");
            Trade();
        }
    }

    //Act with Scene tool
    public void ActWithSceneTool()
    {
        if (sceneTool != null)
        {
            Debug.Log("Animal Character Interact with scene tool"); 
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

    //Set this agent to idle status
    public void SetIdle()
    {
        this.animator.SetInteger("animation", 0);
        this.currentActivity = EActivity.Idle;
        this.bInActivity = false;
    }

    //Group activity: trade
    public void Trade()
    {
        this.agreeCommunication = true;

        this.animator.SetInteger("animation", 0);
        this.bInActivity = true;
        this.currentActivity = EActivity.Trade;

        //if already in trade event
        if (meetAnimalCharacter.agreeCommunication)
        {
            return;
        }

        //Wait another animalcharacter's response
        StartCoroutine(WaitTradeRequest(meetAnimalCharacter, 1f));
        IEnumerator WaitTradeRequest(AnimalCharacter anotherCharacter, float waitTime)
        {
            float accumulatedWaitTime = 0f;
            while (accumulatedWaitTime < waitTime)
            {
                accumulatedWaitTime += Time.deltaTime;
                if (anotherCharacter.agreeCommunication)
                {
                    break;
                }
            }
            yield return new WaitForSeconds(1f); //just for delay
        }

        if(this.agreeCommunication && meetAnimalCharacter.agreeCommunication)
        {
            APickupObject myObject = this.holdObject;
            APickupObject hisObject = meetAnimalCharacter.holdObject;

            //Trade event
            if(myObject && hisObject) //case 1: exchange goods
            {
                myObject.Drop(this);
                hisObject.Drop(meetAnimalCharacter);
                myObject.Pickup(meetAnimalCharacter);
                hisObject.Pickup(this);
            }
            else if (myObject == null && hisObject == null) //case 2: nothing happens
            {

            }

            else if (myObject != null && hisObject == null) //case 2: sell
            {
                if(meetAnimalCharacter.money > myObject.price)
                {
                    myObject.Drop(this);
                    myObject.Pickup(meetAnimalCharacter);
                    this.money += myObject.price;
                    meetAnimalCharacter.money -= myObject.price;
                }
            }


            else if (myObject == null && hisObject != null) //case 4: buy
            {
                if (this.money > hisObject.price)
                {
                    hisObject.Drop(meetAnimalCharacter);
                    hisObject.Pickup(this);
                    this.money -= hisObject.price;
                    meetAnimalCharacter.money += hisObject.price;
                }
            }



        }

        this.SetIdle();
        this.agreeCommunication = false;

        meetAnimalCharacter.SetIdle();
        meetAnimalCharacter.agreeCommunication = false;

        meetAnimalCharacter.meetAnimalCharacter = null;
        this.meetAnimalCharacter = null;
    }
}
