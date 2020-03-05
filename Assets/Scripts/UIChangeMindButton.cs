using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChangeMindButton : MonoBehaviour
{
    public Mind mind;
    public string nestedcharacterName;
    public Text nameText;

    public KeyCode keyCode;

    public void SetButtonInfo()
    {
        nameText.text = nestedcharacterName;
        keyCode = KeyCode.Z;
    }

    public void ShowNestedMind()
    {
        //Debug.Log("UIChangeMindButton clicked" + "show");
        //UIObjectMind oUIMind = this.transform.parent.GetComponent<UIMindSelectionControl>().otherUIObjectMind;
        //int characterIndex = mind.mindNames.IndexOf(nestedcharacterName);
        //oUIMind.DrawMindObjectInfo(mind.otherMinds[characterIndex]);
    }

    private void Update()
    {
        //Debug.Log("UIChangeMindButton update");
        //ShowNestedMind();
        //if (Input.GetKeyDown(keyCode))
        //{
        //    ShowNestedMind();
        //}
    }
}
