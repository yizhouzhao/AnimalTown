using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAffordance : MonoBehaviour
{
    //Object Lists
    public List<GameObject> affordanceObjectList = new List<GameObject>();

    //prefab
    public GameObject affordanceObjectPrefab;

    //UI
    public RectTransform rectTransform;

    public void RegisterObject(GameObject newGameObject)
    {
        affordanceObjectList.Add(newGameObject);
    }

    public void UnregisterObject(GameObject existGameObject)
    {
        affordanceObjectList.Remove(existGameObject);
    }
    
    public void DrawMap(AnimalCharacter animalCharacter, float visionRange = 50f)
    {
        CleanChild();
        Vector3 characterPosition = animalCharacter.gameObject.transform.position;
        for(int i = 0; i < affordanceObjectList.Count; ++i)
        {
            Vector3 objectPosition = affordanceObjectList[i].transform.position;
            if(Vector3.Distance(characterPosition, objectPosition) < visionRange)
            {
                Debug.Log("UIAffordance: " + affordanceObjectList[i].gameObject.name);
                GameObject affordanceObject = Instantiate(affordanceObjectPrefab, this.transform);

                //Set UI 
                UIAffordanceObject affordanceUI = affordanceObject.GetComponent<UIAffordanceObject>();
                affordanceUI.ChangeBackGroudColor(Color.blue);
                if (affordanceObjectList[i].GetComponent<ASceneTool>())
                {
                    affordanceUI.ChangeText(affordanceObjectList[i].GetComponent<ASceneTool>().toolType.ToString());
                }
                else if (affordanceObjectList[i].GetComponent<APickupObject>())
                {
                    affordanceUI.ChangeText(affordanceObjectList[i].GetComponent<APickupObject>().objectType.ToString());
                }

                RectTransform rt = affordanceUI.gameObject.GetComponent<RectTransform>();

                Vector2 offsets = -rt.offsetMax + rt.offsetMin;

                float offset_x = (objectPosition.x - characterPosition.x) * 8 + rt.rect.width - 25;
                float offset_y = (-objectPosition.z + characterPosition.z) * 3 + rt.rect.height - 25;

                Debug.Log("Affordance: " + offset_x + " " + offset_y);

                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, offset_x, 50);
                rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, offset_y, 50);
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
        AnimalCharacter animalCharacter = player.GetComponent<AnimalCharacter>();
        DrawMap(animalCharacter);

    }

    private void Update()
    {
        
    }
}
