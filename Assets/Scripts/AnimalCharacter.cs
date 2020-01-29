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

    //Activity
    public ASceneTool sceneTool;//Scenetool Reference: to record what scene tool the character meets
    public float currentActivityRemainTime;
    public EActivity currentActivity;
    public bool bInActivity;

    //Object in hand
    public bool holdObject;
    public EObject objectType;


    // Start is called before the first frame update
    void Start()
    {
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.SetInteger("animation", 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            print("Interact!!!");
            ActWithSceneTool();
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
}
