using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterState {
    public float positionX;
    public float positionZ;
    public int sceneCode;
    public int holdObjectCode;
    public int meetCharacterCode;
}

[System.Serializable]
public class Mind
{
    //Belief of objects
    public List<PickupObjectInfo> objectList;
    public List<SceneToolInfo> sceneList;

    //History
    public List<AnimalCharacterInfo> characterInfoList;

    //Mind 
    public List<string> mindNames;
    public List<Mind> otherMinds;
    public List<Mind> commonMinds;

    //meet time cool down
    private float meetTimeCoolDown = 2f;

    public Mind()
    {
        objectList = new List<PickupObjectInfo>();
        sceneList = new List<SceneToolInfo>();

        mindNames = new List<string>();
        otherMinds = new List<Mind>();
        commonMinds = new List<Mind>();

        characterInfoList = new List<AnimalCharacterInfo>();
    }

    public void UpdateObjectInfo(APickupObject pickupObject)
    {
        PickupObjectInfo pickupObjectInfo = pickupObject.GetPickupObjectInfo();
        bool alreadyInList = false;
        for (int i = 0; i < objectList.Count; ++i)
        {
            if (objectList[i].objectName == pickupObjectInfo.objectName)
            {
                alreadyInList = true;
                objectList[i] = pickupObjectInfo;
                break;
            }
        }
        if (!alreadyInList)
        {
            objectList.Add(pickupObjectInfo);
        }
    }

    public void UpdateSceneToolInfo(ASceneTool sceneTool)
    {
        SceneToolInfo sceneToolInfo = sceneTool.GetSceneToolInfo();
        bool alreadyInList = false;
        for (int i = 0; i < sceneList.Count; ++i)
        {
            if (sceneList[i].sceneName == sceneToolInfo.sceneName)
            {
                alreadyInList = true;
                sceneList[i] = sceneToolInfo;
                break;
            }
        }
        if (!alreadyInList)
        {
            sceneList.Add(sceneToolInfo);
        }
    }

    //different input of animal character to update their minds
    public void UpdateCharacterInfo(AnimalCharacter animalCharacter)
    {
        string characterName = animalCharacter.characterName;
        int characterIndex = mindNames.IndexOf(characterName);
        if(characterIndex < 0) //not found
        {
            mindNames.Add(characterName);
            otherMinds.Add(new Mind());
            commonMinds.Add(new Mind());
            characterIndex = mindNames.Count - 1;
        }

        //Add character states in mind
        AnimalCharacterInfo acinfo = animalCharacter.GetAnimalCharacterInfo();
        if (otherMinds[characterIndex].characterInfoList.Count > 0)
        {
            if ((otherMinds[characterIndex].characterInfoList[otherMinds[characterIndex].characterInfoList.Count - 1].recordTime - acinfo.recordTime) < meetTimeCoolDown)
            {
                otherMinds[characterIndex].characterInfoList.Add(acinfo);
            }
        }
        else
        {
            otherMinds[characterIndex].characterInfoList.Add(acinfo);
        }

        if (animalCharacter.holdObject)
            otherMinds[characterIndex].UpdateObjectInfo(animalCharacter.holdObject);

        if (animalCharacter.sceneTool)
            otherMinds[characterIndex].UpdateSceneToolInfo(animalCharacter.sceneTool);


    }
}


[System.Serializable]
public class PGNode
{
    //Location
    public float positionX;
    public float positionZ;

    //Time
    public float pgTime;

    //Activity
    public EActivity activityType;

    //Fluents
    public float energy; //tired or excited
    public float fullness; //hungry or full
    public float money; //money

    //Attributes
    public string attr;

    public PGNode(float x, float z, float t, EActivity activity, float engy, float full, float mny)
    {
        positionX = x;
        positionZ = z;
        pgTime = t;
        activityType = activity;
        energy = engy;
        fullness = full;
        money = mny;
    }
}

public class RLAOGControl : MonoBehaviour
{
    [Header("character")]
    public AnimalCharacter animalCharacter;
    public CharacterState characterState;


    [Header("Task Sequence")]
    public List<ETaskType> taskList = new List<ETaskType>();
    public List<string> taskDesription = new List<string>();
    public int currentTaskIndex;
    public ETaskType currentTask;

    [Header("History")]
    public int memoryCapcity = 0;
    [SerializeField]
    public List<PGNode> historyActivity = new List<PGNode>();

    private bool canTakeAction = false;

    [Header("Task Sequence")]
    public List<float> beliefList;

    [Header("Mind")]
    public Mind mind;

    // Start is called before the first frame update
    void Start()
    {
        if (this.gameObject.tag == "Agent")
        {
            StartCoroutine(ExecuteAfterTime(1f));
        }
        
        //init belief
        beliefList = new List<float>();
        for(int i = 0; i < 3; i++)
        {
            beliefList.Add(UnityEngine.Random.Range(0f, 1f));
        }

        IEnumerator ExecuteAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            animalCharacter = this.GetComponent<AnimalCharacter>();

            //animalCharacter.navControl.TravelTo(appleTreeLocation);
            currentTaskIndex = 0;
            currentTask = taskList[currentTaskIndex];

            // Code to execute after the delay
            canTakeAction = true;

            //init 
            characterState = GetCharacterState();
        }

        mind = new Mind();

    }

    public CharacterState GetCharacterState()
    {
        CharacterState character_state = new CharacterState();
        character_state.positionX = this.transform.position.x;
        character_state.positionZ = this.transform.position.z;
        character_state.sceneCode = GetSceneToolCode();
        character_state.holdObjectCode = GetHoldObjectCode();
        character_state.meetCharacterCode = GetMeetCharacterCode();

        return character_state;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.gameObject.tag == "Agent" == canTakeAction)
        {
            if (currentTaskIndex < taskList.Count && animalCharacter.currentActivityCoolDown < 0)
            {
                //Debug.Log("AOG Control:" + animalCharacter.navControl.IsDoneTraveling().ToString());
                currentTask = taskList[currentTaskIndex];
                if (currentTask == ETaskType.Walk)
                {
                    GameObject targetObject = GameObject.Find(taskDesription[currentTaskIndex]);
                    Vector3 targetLocation = new Vector3(targetObject.transform.position.x, this.transform.position.y, targetObject.transform.position.z);
                    animalCharacter.navControl.TravelTo(targetLocation);

                    if (Vector3.Distance(targetLocation, transform.position) < 2f || 
                        (animalCharacter.sceneTool != null && animalCharacter.sceneTool.gameObject.name == taskDesription[currentTaskIndex])
                        || animalCharacter.meetPickupObject != null && animalCharacter.meetPickupObject.gameObject.name == taskDesription[currentTaskIndex]) //done traveling
                    {
                     
                        currentTaskIndex++;
                    }
                }
                else if (currentTask == ETaskType.Interact)
                {
                    animalCharacter.ActWithSceneTool();
                    animalCharacter.ActWithAnimalCharacter();
                    currentTaskIndex++;
                }
                else if(currentTask == ETaskType.PickupDrop)
                {
                    animalCharacter.PickupDropObject();
                    currentTaskIndex++;
                }
                else if (currentTask == ETaskType.Use)
                {
                    animalCharacter.UseObject();
                    currentTaskIndex++;
                }
            }
        }
    }

    public int GetHoldObjectCode()
    {
        if (animalCharacter.holdObject == null)
            return 0;

        switch (animalCharacter.holdObject.objectType)
        {
            case (EPickupObject.Apple):
                return 1;
            case (EPickupObject.Fish):
                return 2;
            default:
                {
                    return 0;
                }
        }
    }

    public int GetSceneToolCode()
    {
        if (animalCharacter.sceneTool == null)
            return 0;

        switch (animalCharacter.sceneTool.toolType)
        {
            case (ESceneEventTool.House):
                return 1;
            case (ESceneEventTool.Tree):
                return 2;
            case (ESceneEventTool.Farm):
                return 3;
            case (ESceneEventTool.Pond):
                return 4;
            case (ESceneEventTool.Fire):
                return 5;

            default:
                {
                    return 0;
                }
        }
    }

    public int GetMeetCharacterCode()
    {
        if (animalCharacter.meetAnimalCharacter == null)
            return 0;
        else
            return 1;
    }

    public void Explore(int state_action_code, ETaskType taskType, string TaskDescription)
    {
        if(state_action_code == 0) //Scene
        {
            
        }
    }
}
