using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASceneTool : MonoBehaviour
{
    //Define tool type
    public ESceneEventTool toolType;
    public string toolName;
    public EActivity activityType;
    public float activityDuration;

    //Occupied
    public int maxCapacity = 1;
    //public bool occupied = false;

    //Occupied Animal Characters
    public List<string> animalCharacterNames; //hold animal character names

    protected void AWake()
    {
        UIAffordance.RegisterObject(this.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {  
        if(other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //Debug.Log("Ascenetool: " + this.toolName + " enters: " + other.gameObject.name);
            //Still have place
            if (animalCharacterNames.Count < maxCapacity)
            {
                AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
                animalCharacterNames.Add(animalCharacter.characterName);
                animalCharacter.sceneTool = this;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {  
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //Debug.Log("Ascenetool: " + this.toolName + " exits: " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            animalCharacterNames.Remove(animalCharacter.characterName);

            if (ReferenceEquals(animalCharacter.sceneTool, this))
            {
                animalCharacter.sceneTool = null;
            }
        }
    }

    //Virtual Act Method: realize interaction between this scene tool and agent
    public virtual void Interact(AnimalCharacter animalCharacter)
    {
        throw new NotImplementedException();
    }
}
