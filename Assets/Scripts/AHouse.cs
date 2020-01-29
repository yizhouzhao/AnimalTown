using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AHouse : ASceneTool
{
    void Awake()
    {
        this.toolName = "House";
        this.toolType = ESceneEventTool.House;
    }

    public override void Interact(AnimalCharacter animalCharacter)
    {
        
    }
}
