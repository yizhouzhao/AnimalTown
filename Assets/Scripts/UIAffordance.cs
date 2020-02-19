using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAffordance : MonoBehaviour
{
    //Object Lists
    public List<GameObject> affordanceObjectList = new List<GameObject>();

    //Color dict
    public Dictionary<string, Color> AffordanceColor = new Dictionary<string, Color>() {
        { "Farm", new Color(0,1f,0.2f,0.3f)},
        { "Pond", new Color(0.2f,0.7f,0.3f)},
        { "House", new Color(0.6f,0.1f,0.3f)},
        { "Fire", new Color(0.6f,0.3f,0.8f)},
        { "Tree", new Color(0.6f,0.5f,0.4f)}
    };

    public Dictionary<string, Vector2> AffordanceSize = new Dictionary<string, Vector2>() {
        { "Farm", new Vector2(200f, 100f)},
        { "Pond",  new Vector2(200f, 200f)},
        { "House", new Vector2(80f, 80f)},
        { "Fire", new Vector2(50f, 50f)},
        { "Tree", new Vector2(50f, 50f)}
    };


    //prefab
    public GameObject affordanceObjectPrefab;

    //UI
    public RectTransform rectTransform;

    AnimalCharacter playerCharacter;

    public void RegisterObject(GameObject newGameObject)
    {
        affordanceObjectList.Add(newGameObject);
    }

    public void UnregisterObject(GameObject existGameObject)
    {
        affordanceObjectList.Remove(existGameObject);
    }
    
    public void DrawMap(AnimalCharacter animalCharacter, float visionRange = 40f)
    {
        CleanChild();
        Vector3 characterPosition = animalCharacter.gameObject.transform.position;
        characterPosition.y = 0;
        for (int i = 0; i < affordanceObjectList.Count; ++i)
        {
            Vector3 objectPosition = affordanceObjectList[i].transform.position;
            objectPosition.y = 0;
            if (Vector3.Distance(characterPosition, objectPosition) < visionRange)
            {
                //Debug.Log("UIAffordance: " + affordanceObjectList[i].gameObject.name);
                GameObject affordanceObject = Instantiate(affordanceObjectPrefab, this.transform);

                //Set UI 
                UIAffordanceObject affordanceUI = affordanceObject.GetComponent<UIAffordanceObject>();
                string objectName;
             
                if (affordanceObjectList[i].GetComponent<ASceneTool>())
                {
                    objectName = affordanceObjectList[i].GetComponent<ASceneTool>().toolType.ToString();
                    affordanceUI.ChangeText(objectName);
                }
                else //if (affordanceObjectList[i].GetComponent<APickupObject>())
                {
                    objectName = affordanceObjectList[i].GetComponent<APickupObject>().objectType.ToString();
                    affordanceUI.ChangeText(objectName);
                }

                affordanceUI.ChangeBackGroudColor(AffordanceColor[objectName]);

                RectTransform rt = affordanceUI.gameObject.GetComponent<RectTransform>();

                Vector2 offsets = -rt.offsetMax + rt.offsetMin;

                float offset_x = (objectPosition.x - characterPosition.x) * 8 + rt.rect.width - 25;
                float offset_y = (-objectPosition.z + characterPosition.z) * 2 + rt.rect.height - 25;

                //Debug.Log("Affordance: " + offset_x + " " + offset_y);

                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, offset_x, AffordanceSize[objectName].x);
                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, offset_y, AffordanceSize[objectName].y);
            }
        }
    }

    private void CleanChild()
    {
        foreach(Transform childTransform in this.transform)
        {
            Destroy(childTransform.gameObject);
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerCharacter = player.GetComponent<AnimalCharacter>();
    }

    private void Update()
    {
        DrawMap(playerCharacter);
    }
}
