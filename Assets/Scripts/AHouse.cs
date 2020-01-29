using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AHouse : ASceneTool
{
    void Awake()
    {
        this.toolName = "House";
        this.toolType = ESceneEventTool.House;
        this.activityType = EActivity.Sleep;
        this.activityDuration = 4f;
    }

    public override void Interact(AnimalCharacter animalCharacter)
    {
        animalCharacter.currentActivity = EActivity.Sleep;
        animalCharacter.animator.SetInteger("animation", 5);

        Debug.Log("AHouse: sleep");
        StartCoroutine(SleepNow());
        IEnumerator SleepNow()
        {
            yield return new WaitForSeconds(4f);

            animalCharacter.animator.SetInteger("animation", 0);
            animalCharacter.energy = Mathf.Min(1f, animalCharacter.energy + 0.5f);
            animalCharacter.bInActivity = false;
        }
    }
}
