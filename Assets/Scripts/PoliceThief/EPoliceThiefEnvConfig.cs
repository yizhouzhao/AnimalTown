using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


public class EPoliceThiefEnv
{
    public static float landSize = 100f;
}

public class EPoliceThiefEnvConfig : MonoBehaviour
{
    public List<Pedestrain> pedestrains = new List<Pedestrain>();
    public List<RLPolice> polices = new List<RLPolice>();
    public List<ThiefAgent> thieves = new List<ThiefAgent>();

    public int worldAgentNum;

    public MonoBehaviour GetAgent(int idx)
    {
        Assert.IsTrue(idx >= 0 && idx < pedestrains.Count + polices.Count + thieves.Count);
        if (idx < pedestrains.Count)
            return pedestrains[idx];
        else if (idx < (polices.Count + pedestrains.Count))
            return polices[idx - pedestrains.Count];

        return thieves[idx - polices.Count + pedestrains.Count];
    }

    // Start is called before the first frame update
    void Start()
    {
        worldAgentNum = pedestrains.Count + polices.Count + thieves.Count;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
