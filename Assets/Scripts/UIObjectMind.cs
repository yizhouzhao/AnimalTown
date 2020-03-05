using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIObjectMind : MonoBehaviour
{

    //prefab
    public GameObject ObjectInfoPrefab;

    RLAOGControl playerAOGControl;

    //will this object update mind all the time?
    
    public bool dynamicFlag = true;
    public bool isCommon = false;


    // Start is called before the first frame update
    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerAOGControl = player.GetComponent<RLAOGControl>();
    }


    public void DrawMindObjectInfo(Mind mind)
    {
        //clean child transform
        foreach (Transform childTransform in this.transform)
        {
            Destroy(childTransform.gameObject);
        }

        for(int i = mind.objectList.Count - 1; i >= 0; i--)
        {
            GameObject oiObject = Instantiate(ObjectInfoPrefab, this.transform);
            RectTransform rt = oiObject.GetComponent<RectTransform>();

            //Debug.Log("UIPanelControl: " + "pgInit");
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20 + 50 * i, 40);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10, 40);

            UIMindInfoPanel uiPanel = oiObject.GetComponent<UIMindInfoPanel>();
            uiPanel.SetInfoFromMind(mind.objectList[i]);
        }

        int offsetCount = mind.objectList.Count;
        for (int i = mind.sceneList.Count - 1; i >= 0; i--)
        {
            GameObject oiObject = Instantiate(ObjectInfoPrefab, this.transform);
            RectTransform rt = oiObject.GetComponent<RectTransform>();

            //Debug.Log("UIPanelControl: " + "pgInit");
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20 + 50 * (i + offsetCount), 40);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10, 40);

            UIMindInfoPanel uiPanel = oiObject.GetComponent<UIMindInfoPanel>();
            uiPanel.SetInfoFromMind(mind.sceneList[i]);
        }

        offsetCount += mind.sceneList.Count;
        for (int i = mind.characterInfoList.Count - 1; i >= 0; i--)
        {
            GameObject oiObject = Instantiate(ObjectInfoPrefab, this.transform);
            RectTransform rt = oiObject.GetComponent<RectTransform>();

            //Debug.Log("UIPanelControl: " + "pgInit");
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20 + 50 * (i + offsetCount), 40);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10, 40);

            UIMindInfoPanel uiPanel = oiObject.GetComponent<UIMindInfoPanel>();
            uiPanel.SetInfoFromMind(mind.characterInfoList[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (dynamicFlag)
        {
            DrawMindObjectInfo(playerAOGControl.mind);
        }
        else
        {
            if (isCommon)
            {
                if (playerAOGControl.mind.commonMinds.Count > 0)
                {
                    //Debug.Log("UIObjectMind update");
                    DrawMindObjectInfo(playerAOGControl.mind.commonMinds[0]);
                }
            }
            else
            {
                if (playerAOGControl.mind.otherMinds.Count > 0)
                {
                    //Debug.Log("UIObjectMind update");
                    DrawMindObjectInfo(playerAOGControl.mind.otherMinds[0]);
                }
            }
        }
    }
}
