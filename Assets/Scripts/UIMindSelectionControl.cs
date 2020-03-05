using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMindSelectionControl : MonoBehaviour
{
    //prefab
    public GameObject MindSelectionButtonPrefab;

    RLAOGControl playerAOGControl;

    //other mind and common mind
    public UIObjectMind otherUIObjectMind;
    public UIObjectMind commonUIObjectMind;

    // Start is called before the first frame update
    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerAOGControl = player.GetComponent<RLAOGControl>();
    }

    public void DrawSelectionButton(Mind mind)
    {
        //clean child transform
        foreach (Transform childTransform in this.transform)
        {
            Destroy(childTransform.gameObject);
        }

        for (int i = mind.mindNames.Count - 1; i >= 0; i--)
        {
            string mindName = mind.mindNames[i];
            GameObject buttonObject = Instantiate(MindSelectionButtonPrefab, this.transform);
            RectTransform rt = buttonObject.GetComponent<RectTransform>();

            //Debug.Log("UIPanelControl: " + "pgInit");
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20 + 120 * i, 100);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10, 30);

            UIChangeMindButton uiMindButton = buttonObject.GetComponent<UIChangeMindButton>();
            uiMindButton.mind = mind;
            uiMindButton.nestedcharacterName = mindName;
            uiMindButton.SetButtonInfo();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Mind mind = playerAOGControl.mind;

        DrawSelectionButton(mind);
    }
}
