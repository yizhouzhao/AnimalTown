using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ESceneEventTool
{
    House, Tree, Pond, Farm, Fire
}

public enum EActivity
{
    Idle, Sleep, Eat, Play, Travel, CollectFruit, Fishing, Cook, Plant, Trade
}

public enum EPickupObject
{
    Apple, Fish, Bamboo
}

public enum EAnimalType
{
    Panda, Tiger, Goose, Pig
}


public class AnimalIslandProfile
{
    public static float terrainHeight = 200;
    public static float terrainWidth = 200;

}

public class EAnimalIslandDefinitions : MonoBehaviour
{
}
