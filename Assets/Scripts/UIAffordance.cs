using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAffordance : MonoBehaviour
{
    //Object Lists
    public static List<GameObject> affordanceObjectList = new List<GameObject>();

    //Color dict
    public Dictionary<string, Color> AffordanceColor = new Dictionary<string, Color>() {
        { "Farm", new Color(0,1f,0.2f,0.3f)},
        { "Pond", new Color(0.2f,0.7f,0.3f)},
        { "House", new Color(0.6f,0.1f,0.3f)},
        { "Fire", new Color(0.6f,0.3f,0.8f)},
        { "Tree", new Color(0.6f,0.5f,0.4f)},

        { "Apple", new Color(0.7f,0.2f,0.2f)},
        { "Fish", new Color(0.7f,0.6f,0.1f)},

    };

    public Dictionary<string, Vector2> AffordanceSize = new Dictionary<string, Vector2>() {
        { "Farm", new Vector2(100f, 40f)},
        { "Pond",  new Vector2(200f, 200f)},
        { "House", new Vector2(80f, 80f)},
        { "Fire", new Vector2(50f, 50f)},
        { "Tree", new Vector2(50f, 50f)},

        { "Apple", new Vector2(30f, 30f)},
        { "Fish", new Vector2(40f, 40f)}
    };


    //prefab
    public GameObject affordanceObjectPrefab;
    public GameObject lineImage;

    AnimalCharacter playerCharacter;

    public static void RegisterObject(GameObject newGameObject)
    {
        affordanceObjectList.Add(newGameObject);
    }

    public static void UnregisterObject(GameObject existGameObject)
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
                string conditionText;
                string affordanceText;
                
             
                if (affordanceObjectList[i].GetComponent<ASceneTool>())
                {
                    objectName = affordanceObjectList[i].GetComponent<ASceneTool>().toolType.ToString();
                    ASceneTool sceneTool = affordanceObjectList[i].GetComponent<ASceneTool>();
                    conditionText = sceneTool.animalCharacterNames.Count < sceneTool.maxCapacity ? "(Available)" 
                        : ReferenceEquals(animalCharacter.sceneTool,sceneTool) ? "(In)" : "(Occupied)";
                    affordanceText = sceneTool.animalCharacterNames.Count < sceneTool.maxCapacity ? "[" + sceneTool.activityType.ToString() + "]" : "[None]";

                    affordanceUI.ChangeNameText(objectName);
                    affordanceUI.ChangeConditionText(conditionText);
                    affordanceUI.ChangeAffordanceText(affordanceText);
                }
                else //if (affordanceObjectList[i].GetComponent<APickupObject>())
                {
                    APickupObject pickupObject = affordanceObjectList[i].GetComponent<APickupObject>();
                    objectName = pickupObject.objectType.ToString();
                    affordanceUI.ChangeNameText(objectName);

                    conditionText = pickupObject.occupied ? "(Occupied)"
                        : ReferenceEquals(animalCharacter.holdObject, pickupObject) ? "(owned)" : "(Available)";
                    string descriptionText = "";

                    if (pickupObject as AFood)
                    {
                        descriptionText += " Eatable";
                    }

                    if(pickupObject as ASeed)
                    {
                        descriptionText += " Plantable";
                    }

                    affordanceUI.ChangeConditionText(conditionText);
                    affordanceUI.ChangeAffordanceText("[" + descriptionText + "]");
                }

                affordanceUI.ChangeBackGroudColor(AffordanceColor[objectName]);

                //Draw rectangle
                RectTransform rt = affordanceUI.gameObject.GetComponent<RectTransform>();

                Vector2 offsets = -rt.offsetMax + rt.offsetMin;

                float offset_x = (objectPosition.x - characterPosition.x) * 4 + rt.rect.width;
                float offset_y = (-objectPosition.z + characterPosition.z) * 2 + rt.rect.height;

                Debug.Log("Affordance rt size: " + rt.rect.width + " " + rt.rect.height);

                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, offset_x, AffordanceSize[objectName].x);
                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, offset_y, AffordanceSize[objectName].y);

                ////Draw line
                //GameObject LineImage = Instantiate(lineImage, this.transform);
                //RectTransform imageRectTransform = LineImage.GetComponent<RectTransform>();

                //Vector2 differenceVector = new Vector2(offset_x, offset_y);
                ////imageRectTransform.sizeDelta = new Vector2(differenceVector.magnitude, 10f);

                //imageRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, offset_x, differenceVector.magnitude);
                //imageRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, offset_y, 10f);

                //imageRectTransform.pivot = new Vector2(0, 0.5f);
                //imageRectTransform.position = new Vector2(300f, 300f);
                //float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
                //imageRectTransform.rotation = Quaternion.Euler(0, 0, angle);


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
