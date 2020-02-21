using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPGPanelControl : MonoBehaviour
{
    //prefab
    public GameObject PGPanelPrefab;

    RLAOGControl playerAOGControl;

    //
    public Dictionary<string, Color> ActivityColor = new Dictionary<string, Color>() {
        { "Farm", new Color(0,1f,0.2f,0.3f)},
        { "Pond", new Color(0.2f,0.7f,0.3f)},
        { "House", new Color(0.6f,0.1f,0.3f)},
        { "Fire", new Color(0.6f,0.3f,0.8f)},
        { "Tree", new Color(0.6f,0.5f,0.4f)},

        { "Apple", new Color(0.7f,0.2f,0.2f)},
        { "Fish", new Color(0.7f,0.6f,0.1f)},

    };

    public void DrawPG(RLAOGControl aogControl)
    {
        //clean child transform
        foreach (Transform childTransform in this.transform)
        {
            Destroy(childTransform.gameObject);
        }

        //Draw
        for(int i = aogControl.historyActivity.Count - 1; i >= 0; --i)
        {
            GameObject pgObject = Instantiate(PGPanelPrefab, this.transform);
            RectTransform rt = pgObject.GetComponent<RectTransform>();

            Debug.Log("UIPanelControl: " + "pgInit");
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20 + 50 * i, 40);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 30, 40);

            UIPGPanel uiPanel = pgObject.GetComponent<UIPGPanel>();

            uiPanel.SetContentFromPGNode(aogControl.historyActivity[i]);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerAOGControl = player.GetComponent<RLAOGControl>();
    }

    // Update is called once per frame
    void Update()
    {
        DrawPG(playerAOGControl);
    }
}
