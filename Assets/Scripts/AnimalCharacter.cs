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

    public float fullLossPerSecond = 0.01f;
    public float energyLossPerSecond = 0.01f;

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
    public KeyCode communicationKey;

    [Header("Communication")]
    public AnimalCharacter meetAnimalCharacter; //meet another agent
    public bool agreeCommunication; //whether the two character want to communicate
    public TCone visionCone;
    
    [HideInInspector]
    public AgentNavigationControl navControl;

    [Header("Nevigation")]
    public GameObject signPrefab;
    public Vector3 originalLocation;

    [Header("Survival Time")]
    public float runningTime;

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

        //Activity cool down time
        activityCoolDown = EAnimalIslandDefinitions.characterActivityCoolDown;
        currentActivityCoolDown = activityCoolDown;

        //Set up navigation control for agents only
        if (this.gameObject.tag == "Agent")
        {
            navControl = GetComponent<AgentNavigationControl>();
        }
        originalLocation = this.transform.position;


        //Setup vision
        Transform visionConeTransform = transform.Find("cone");
        if (visionConeTransform == null)
        {
            Debug.LogError("No vision transform for player/agent");
        }
        visionCone = visionConeTransform.GetComponent<TCone>();
    
        ResetAnimalCharacter();
    }

    //Reset fluents for characters
    public void ResetAnimalCharacter()
    {
        //energy and full
        energy = 0.5f;
        fullness = 0.5f;
        //Set up start money
        money = 10f;

        this.transform.position = originalLocation;
        this.transform.rotation = Quaternion.identity;

        //Reset activity, animation and nevigation
        SetIdle();

        meetAnimalCharacter = null;
    }

    // Update is called once per frame
    void Update()
    {
        //Activity cool down
        if(!bInActivity)
            currentActivityCoolDown -= Time.deltaTime;
        
        if (currentActivityCoolDown > 0)
            return;

        if (gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(interactKey))
            {
                ActWithSceneTool();
            }
            if (Input.GetKeyDown(interactKey))
            {
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
        if(gameObject.tag == "NOLONGERAgent")
        {
            if (UnityEngine.Random.Range(0f, 1f) < 0.6) //Input.GetKeyDown(interactKey))
            {
                ActWithSceneTool();
            }

            if (UnityEngine.Random.Range(0f, 1f) < 0.9f) //Input.GetKeyDown(communicationKey))
            {
                //meetAnimalCharacter.visionCone.enabled = false;
                ActWithAnimalCharacter();
            }

            if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
            {
                PickupDropObject();
            }

            if (UnityEngine.Random.Range(0f, 1f) < 0.2f)
            {
                UseObject();
            }

            if (navControl.IsDoneTraveling())
            {
                //RandomWalk1(UnityEngine.Random.Range(100f, 200f));
                currentActivityCoolDown = activityCoolDown;
                RandomWalk2();
            }
        }

    }

    //Fix update for 
    private void FixedUpdate()
    {
        RegularGainAndLoss();

    }

    private void RegularGainAndLoss()
    {
        //energy and full loss
        energy = Mathf.Max(energy - energyLossPerSecond * Time.fixedDeltaTime, 0f);
        fullness = Mathf.Max(fullness - fullLossPerSecond * Time.fixedDeltaTime, 0f);
        //money += 0.0001f * money;
    }

    //Act with animal character event
    public void ActWithAnimalCharacter()
    {
        if ((meetAnimalCharacter != null) && (!bInActivity))//&& (!meetAnimalCharacter.bInActivity))
        {
            //Debug.Log("Animal Character Interact with another character");
            currentActivityCoolDown = activityCoolDown;
            Trade();
        }
    }

    //Act with scene tool event
    public void ActWithSceneTool()
    {
        if ((sceneTool != null) && (!bInActivity))
        {
            //cool down
            currentActivityCoolDown = activityCoolDown;
            //Debug.Log("Animal Character Interact with scene tool"); 
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
                //Debug.Log("Animal Charcte Pickup " + meetPickupObject.objectType.ToString());
                meetPickupObject.Pickup(this);
                currentActivityCoolDown = activityCoolDown;
            }
        }
        else
        {
            
            //Debug.Log("Animal Charcte Drop " + holdObject.objectType.ToString());
            holdObject.Drop(this);
            currentActivityCoolDown = activityCoolDown;
        }
    }

    //Use object event
    public void UseObject()
    {
        if (holdObject != null && (!bInActivity))
        {
            //Debug.Log("Animal Character Use " + holdObject.objectType.ToString());
            AFood food = holdObject as AFood;

            //If it is food and eatable
            if (food && food.eatable)
            {
                currentActivityCoolDown = activityCoolDown;
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
            //this.navControl.agent.speed = 0;
            //this.navControl.agent.angularSpeed = 0;
            //navControl.recordVelocity = navControl.agent.velocity;
            //navControl.agent.velocity = Vector3.zero;
            navControl.agent.isStopped = true;
        }
        else //tag == "Player"
        {
            GetComponent<CharacterController>().enabled = false;
        }

        //Test stop move and immediatly stop moving
    }

    //Set this agent to idle status
    public void SetIdle()
    {
        this.animator.SetInteger("animation", 0);
        this.currentActivity = EActivity.Idle;
        this.bInActivity = false;

        if (navControl)
        {
            //this.navControl.agent.speed = this.navControl.originalSpeed;
            //this.navControl.agent.angularSpeed = this.navControl.originalAngularSpeed;
            navControl.agent.isStopped = false;
            //navControl.agent.velocity = navControl.recordVelocity;
        }
        else
        {
            GetComponent<CharacterController>().enabled = true;
        }

        //Test stop move and immediatly resume moving
        currentActivityCoolDown = activityCoolDown;
    }

    //Group activity: trade
    public void Trade()
    {
        //stop
        StopMove();
        meetAnimalCharacter.StopMove();

        this.agreeCommunication = true;
        this.bInActivity = true;
        //meetAnimalCharacter.bInActivity = true;

        this.animator.SetInteger("animation", 0);
        this.currentActivity = EActivity.Trade;



        //if already in trade event
        if (meetAnimalCharacter.agreeCommunication)
        {
            return;
        }

        //Debug.LogError("Trade: " + name + " look at " + meetAnimalCharacter.name);

        //Wait another animalcharacter's response
        StartCoroutine(WaitTradeRequest(3f));
        IEnumerator WaitTradeRequest(float waitTime)
        {
            float accumulatedWaitTime = 0f;
            while (accumulatedWaitTime < waitTime)
            {
                accumulatedWaitTime += Time.deltaTime;

                Debug.Log("Trade look at movement " + this.name + " : " + meetAnimalCharacter.name);
                this.transform.LookAt(meetAnimalCharacter.transform.position);
                meetAnimalCharacter.transform.LookAt(this.transform.position);

                if (meetAnimalCharacter.agreeCommunication)
                {
                    break;
                }
                yield return null;
            }
            
            //yield return new WaitForSeconds(.1f); //just for delay

            //if both two agents agree to trade
            if (this.agreeCommunication && meetAnimalCharacter.agreeCommunication)
            {
                Debug.Log("Animal Character !!!Trade!!!: " + this.name + " with " + meetAnimalCharacter.name);
                APickupObject myObject = this.holdObject;
                APickupObject hisObject = meetAnimalCharacter.holdObject;

                //Trade event
                if (myObject && hisObject) //case 1: exchange goods
                {
                    //Debug.Log("Animal Character Trade case 1");
                    myObject.Drop(this);
                    myObject.occupied = true;
                    hisObject.Drop(meetAnimalCharacter);
                    hisObject.occupied = true;

                    myObject.Pickup(meetAnimalCharacter);
                    hisObject.Pickup(this);
                }
                else if (myObject == null && hisObject == null) //case 2: nothing happens
                {
                    //Debug.Log("Animal Character Trade case 2");
                }

                else if (myObject != null && hisObject == null) //case 3: sell
                {
                    //Debug.Log("Animal Character Trade case 3");
                    if (meetAnimalCharacter.money > myObject.price)
                    {
                        myObject.Drop(this);
                        myObject.occupied = true;
                        myObject.Pickup(meetAnimalCharacter);
                        this.money += myObject.price;
                        meetAnimalCharacter.money -= myObject.price;
                    }
                }


                else if (myObject == null && hisObject != null) //case 4: buy
                {
                    //Debug.Log("Animal Character Trade case 4");
                    if (this.money > hisObject.price)
                    {
                        hisObject.Drop(meetAnimalCharacter);
                        hisObject.occupied = true;
                        hisObject.Pickup(this);
                        this.money -= hisObject.price;
                        meetAnimalCharacter.money += hisObject.price;
                    }
                }

            }

            else
            {
                //Debug.LogError("Animal Character No trade");
            }

            if (meetAnimalCharacter) 
            {
                meetAnimalCharacter.SetIdle();
                meetAnimalCharacter.agreeCommunication = false;
                //Debug.LogError("Trade Complete 1");
                meetAnimalCharacter.meetAnimalCharacter = null;
                meetAnimalCharacter = null;
                //Debug.LogError("Trade Complete 2");
            }


            this.SetIdle();
            this.agreeCommunication = false;
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
        float x = UnityEngine.Random.Range(0.1f, 0.9f) * EAnimalIslandDefinitions.terrainHeight;
        float z = UnityEngine.Random.Range(0.1f, 0.9f) * EAnimalIslandDefinitions.terrainWidth;

        Vector3 targetPosition = new Vector3(x, this.transform.position.y, z);
        NavMeshHit hit;
        NavMesh.SamplePosition(targetPosition, out hit, 50, NavMesh.AllAreas);

        navControl.TravelTo(hit.position);
        //Debug.Log("Animal Character RandomWalk2: " + hit.position);
        GameObject pNewObject = (GameObject)GameObject.Instantiate(signPrefab, hit.position, Quaternion.identity);
    }

    //Navigate to 
    public void Walk2(float x, float z)
    {
        currentActivityCoolDown = activityCoolDown;
        Vector3 targetPosition = new Vector3(x * EAnimalIslandDefinitions.terrainHeight, this.transform.position.y, z * EAnimalIslandDefinitions.terrainWidth);
        NavMeshHit hit;
        NavMesh.SamplePosition(targetPosition, out hit, 50, NavMesh.AllAreas);

        navControl.TravelTo(hit.position);
        //Debug.Log("Animal Character RandomWalk2: " + hit.position);
        GameObject pNewObject = (GameObject)GameObject.Instantiate(signPrefab, hit.position, Quaternion.identity);
    }

    //Navigate to 
    public void Walk3 (float x, float z, float scale = 20f)
    {
        currentActivityCoolDown = activityCoolDown;

        Vector3 targetPosition = this.transform.position + this.transform.forward * x * scale;
        targetPosition += this.transform.right * z * scale;

        NavMeshHit hit;
        NavMesh.SamplePosition(targetPosition, out hit, 2 *　scale, NavMesh.AllAreas);

        navControl.TravelTo(hit.position);
        //Debug.Log("Animal Character RandomWalk2: " + hit.position);
        GameObject pNewObject = (GameObject)GameObject.Instantiate(signPrefab, hit.position, Quaternion.identity);
    }
}
