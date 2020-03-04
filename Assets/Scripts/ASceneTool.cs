using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneToolInfo : mindInfo
{
    //object type
    public ESceneEventTool sceneType;
    public string sceneName;

    //occupied
    public bool occupied = false;

    public SceneToolInfo(ESceneEventTool otype, string oname, Vector3 position, float timeT)
    {
        sceneName = oname;
        this.sceneType = otype;
        recordPosition = position;
        recordTime = timeT;
    }
}

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


    public SceneToolInfo GetSceneToolInfo()
    {
        return new SceneToolInfo(toolType, this.gameObject.name, this.transform.position, Time.time);
    }


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
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if (animalCharacterNames.Count < maxCapacity)
            {
                animalCharacterNames.Add(animalCharacter.characterName);
                animalCharacter.sceneTool = this;
            }
            RLAOGControl aogControl = animalCharacter.gameObject.GetComponent<RLAOGControl>();
            aogControl.mind.UpdateSceneToolInfo(this);
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
