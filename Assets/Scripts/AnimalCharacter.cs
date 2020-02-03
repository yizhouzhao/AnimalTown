using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public float activityCoolDown;
    public float currentActivityCoolDown;

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

    
    [HideInInspector]
    public AgentNavigationControl navControl;

    [Header("Nevigation")]
    public GameObject signPrefab;

    // Start is called before the first frame update
    void Start()
    {
        //Set up animation
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.SetInteger("animation", 0);

        //Set hold object position
        holdTransform = this.transform.Find("HoldTransform");
        if(holdTransform == null)
        {
            Debug.LogError("No hold transform for player/agent");
        }

        //Set up start money
        money = 10f;

        //Activity cool down time
        activityCoolDown = 3f;
        currentActivityCoolDown = activityCoolDown;

        //Set up navigation control for agents only
        if (this.gameObject.tag == "Agent")
        {
            navControl = GetComponent<AgentNavigationControl>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.tag == "Player")
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

        //Test random walk for agent
        if(gameObject.tag == "Agent")
        {
            currentActivityCoolDown -= Time.deltaTime;
            if (currentActivityCoolDown > 0)
                return;

            
            if (Input.GetKeyDown(interactKey))//UnityEngine.Random.Range(0f, 1f) < 0.9)
            {
                ActWithSceneTool();
                ActWithAnimalCharacter();
                //navControl.agent.speed = navControl.originalSpeed;
                currentActivityCoolDown = activityCoolDown;
            }
            //if (UnityEngine.Random.Range(0f, 1f) < 0.6)
            //{
            //    //avControl.agent.speed = 0;
            //    PickupDropObject();
            //    //navControl.agent.speed = navControl.originalSpeed;
            //}

            //if (UnityEngine.Random.Range(0f, 1f) < 0.6)
            //{
            //    //navControl.agent.speed = 0;
            //    UseObject();
            //    //navControl.agent.speed = navControl.originalSpeed;
            //}

            //if (navControl.IsDoneTraveling())
            //{
            //    //RandomWalk1(UnityEngine.Random.Range(100f, 200f));
            //    RandomWalk2();
            //}
        }

    }

    //Act with animal character event
    private void ActWithAnimalCharacter()
    {
        if (this.meetAnimalCharacter)
        {
            Debug.Log("Animal Character Interact with another character");
            Trade();
        }
    }

    //Act with scene tool event
    public void ActWithSceneTool()
    {
        if (sceneTool != null)
        {
            Debug.Log("Animal Character Interact with scene tool"); 
            this.bInActivity = true;
            sceneTool.Interact(this);
        }
    }

    //Pick up drop event
    public void PickupDropObject()
    {
        if(holdObject == null)
        {
            if (meetPickupObject != null)
            {
                print("Pickup!!!");
                meetPickupObject.Pickup(this);
            }
        }
        else
        {
            print("Drop!!!");
            holdObject.Drop(this);
        }
    }

    //Use object event
    public void UseObject()
    {
        if (holdObject != null)
        {
            //If it is food and eatable
            print("Animal Character Use!!!");
            AFood food = holdObject as AFood;
            if (food && food.eatable)
            {
                this.bInActivity = true;
                food.Eat(this);
            }
        }
    }

    //Stop movement
    public void StopMove()
    {
        if (tag == "Agent") //agent
        {
            this.navControl.agent.speed = 0;
            this.navControl.agent.angularSpeed = 0;
        }
        else //tag == "Player"
        {
            GetComponent<CharacterController>().enabled = false;
        }
    }

    //Set this agent to idle status
    public void SetIdle()
    {
        this.animator.SetInteger("animation", 0);
        this.currentActivity = EActivity.Idle;
        this.bInActivity = false;

        if (navControl)
        {
            this.navControl.agent.speed = this.navControl.originalSpeed;
            this.navControl.agent.angularSpeed = this.navControl.originalAngularSpeed;
        }
        else
        {
            GetComponent<CharacterController>().enabled = true;
        }
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
        StartCoroutine(WaitTradeRequest(meetAnimalCharacter, 3f));
        IEnumerator WaitTradeRequest(AnimalCharacter anotherCharacter, float waitTime)
        {
            //stop
            StopMove();

            float accumulatedWaitTime = 0f;
            while (accumulatedWaitTime < waitTime)
            {
                accumulatedWaitTime += Time.deltaTime;
                if (anotherCharacter.agreeCommunication)
                {
                    break;
                }
                yield return null;
            }
            
            //yield return new WaitForSeconds(1f); //just for delay

            if (this.agreeCommunication && meetAnimalCharacter.agreeCommunication)
            {
                APickupObject myObject = this.holdObject;
                APickupObject hisObject = meetAnimalCharacter.holdObject;

                //Trade event
                if (myObject && hisObject) //case 1: exchange goods
                {
                    Debug.Log("Animal Character Trade case 1");
                    myObject.Drop(this);
                    hisObject.Drop(meetAnimalCharacter);
                    myObject.Pickup(meetAnimalCharacter);
                    hisObject.Pickup(this);
                }
                else if (myObject == null && hisObject == null) //case 2: nothing happens
                {
                    Debug.Log("Animal Character Trade case 2");
                }

                else if (myObject != null && hisObject == null) //case 3: sell
                {
                    Debug.Log("Animal Character Trade case 3");
                    if (meetAnimalCharacter.money > myObject.price)
                    {
                        myObject.Drop(this);
                        myObject.Pickup(meetAnimalCharacter);
                        this.money += myObject.price;
                        meetAnimalCharacter.money -= myObject.price;
                    }
                }


                else if (myObject == null && hisObject != null) //case 4: buy
                {
                    Debug.Log("Animal Character Trade case 4");
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

            //meetAnimalCharacter.meetAnimalCharacter = null;
            //this.meetAnimalCharacter = null;
        }
    }

    //Walk event: random walk version 1 get a random point from radius
    public void RandomWalk1(float walkRadius)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);
        Vector3 finalPosition = hit.position;

        navControl.TravelTo(finalPosition);
    }

    //Walk event: random walk version 2 get a random point on map
    public void RandomWalk2()
    {
        float x = UnityEngine.Random.Range(0.1f, 0.9f) * AnimalIslandProfile.terrainHeight;
        float z = UnityEngine.Random.Range(0.1f, 0.9f) * AnimalIslandProfile.terrainWidth;

        Vector3 targetPosition = new Vector3(x, this.transform.position.y, z);
        NavMeshHit hit;
        NavMesh.SamplePosition(targetPosition, out hit, 50, NavMesh.AllAreas);

        navControl.TravelTo(hit.position);
        Debug.Log("Animal Character RandomWalk2: " + hit.position);
        GameObject pNewObject = (GameObject)GameObject.Instantiate(signPrefab, hit.position, Quaternion.identity);
    }
}
