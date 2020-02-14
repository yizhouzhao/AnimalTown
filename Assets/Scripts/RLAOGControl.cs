using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RLAOGControl : MonoBehaviour
{
    [Header("character")]
    public AnimalCharacter animalCharacter;


    [Header("Task Sequence")]
    public List<ETaskType> taskList = new List<ETaskType>();
    public List<string> taskDesription = new List<string>();
    public int currentTaskIndex;
    public ETaskType currentTask;

    private bool canTakeAction = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ExecuteAfterTime(1f));
        IEnumerator ExecuteAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            animalCharacter = this.GetComponent<AnimalCharacter>();

            //animalCharacter.navControl.TravelTo(appleTreeLocation);
            currentTaskIndex = 0;
            currentTask = taskList[currentTaskIndex];

            // Code to execute after the delay
            canTakeAction = true;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (canTakeAction)
        {
            if (currentTaskIndex < taskList.Count && animalCharacter.currentActivityCoolDown < 0)
            {
                //Debug.Log("AOG Control:" + animalCharacter.navControl.IsDoneTraveling().ToString());
                currentTask = taskList[currentTaskIndex];
                if (currentTask == ETaskType.Walk)
                {
                    GameObject targetObject = GameObject.Find(taskDesription[currentTaskIndex]);
                    animalCharacter.navControl.TravelTo(targetObject.transform.position);

                    if (Vector3.Distance(targetObject.transform.position, transform.position) < 2f) //done traveling
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
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
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
}
