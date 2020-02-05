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


public class EAnimalIslandDefinitions : MonoBehaviour
{
    //Terrianian
    public static float terrainHeight = 200f;
    public static float terrainWidth = 200f;

    //ActivityTime
    public static float sleepTime = 16f;
    
    public static float collectAppleTime = 2f;
    
    public static float collectFishTime = 4f;
    public static int pondCapacity = 4; //allow how many animals at pond together at one time

    //Seed:apple
    public static float appleGrowTime = 12f; //how long it takes to grow an apple
    public static float appleFullGain = 0.2f;
    public static float appleCookTime = 2f;
    public static float appleEatTime = 2f;
    public static float appleSeedGrowTime = 48f; //how long it takes to grow an apple tree
    public static float applePrice = 1f;
    public static float appleStayFreshTime = 100f;

    //Food:fish
    public static float fishGrowTime = 8f;
}
