using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalCharacter : MonoBehaviour
{
    [Header("Basic information")]
    public string characterName;
    public int age;
    public string chararcterDescription;

    [Header("Skills")]
    public float structured;
    public float motorSkill;
    public float artCraftSkill;

    [Header("Animation")]
    public Animator animator;

    [Header("Status(Fluents)")]
    public float energy; //tired or excited
    public float fullness; //hungry or full

    //Activity
    public ASceneTool sceneTool;//Scenetool Reference: to record what scene tool the character meets
    public float currentActivityRemainTime;
    public EActivity currentActivity;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
