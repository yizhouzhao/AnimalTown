using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIObjectMind : MonoBehaviour
{

    //prefab
    public GameObject ObjectInfoPrefab;

    RLAOGControl playerAOGControl;

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
    }

    // Update is called once per frame
    void Update()
    {
        DrawMindObjectInfo(playerAOGControl.mind);
    }
}
