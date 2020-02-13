using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RLAOGControl : MonoBehaviour
{
    [Header("character")]
    public AnimalCharacter animalCharacter;

    [Header("example one conponet")]
    public GameObject appleTree;
    private Vector3 appleTreeLocation;

    [Header("Task Sequence")]
    public List<ETaskType> taskList = new List<ETaskType>();
    public List<string> taskDesription = new List<string>();
    public int currentTaskIndex;
    public ETaskType currentTask;


    // Start is called before the first frame update
    void Start()
    {
        animalCharacter = this.GetComponent<AnimalCharacter>();
        appleTreeLocation = appleTree.transform.position;

        animalCharacter.navControl.TravelTo(appleTreeLocation);
        currentTaskIndex = 0;
        currentTask = taskList[currentTaskIndex];
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTaskIndex < taskDesription.Count && animalCharacter.currentActivityCoolDown < 0)
        {
            currentTask = taskList[currentTaskIndex];
            if (currentTask == ETaskType.Walk)
            {
                animalCharacter.navControl.TravelTo(appleTreeLocation);
                if (animalCharacter.navControl.IsDoneTraveling())
                {
                    currentTaskIndex++;
                }
            }
            else if(currentTask == ETaskType.Interact)
            {
                animalCharacter.ActWithSceneTool();
                animalCharacter.ActWithAnimalCharacter();
                currentTaskIndex++;
            }
            else if(currentTask == ETaskType.Use)
            {
                animalCharacter.UseObject();
                currentTaskIndex++;
            }
        }
    }
}
