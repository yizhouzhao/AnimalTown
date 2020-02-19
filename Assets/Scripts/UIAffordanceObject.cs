using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAffordanceObject : MonoBehaviour
{
    public Image backgroudImage;
    public Text objectName;
    public Text conditionText;
    public Text affordanceText;

    public void ChangeBackGroudColor(Color newColor)
    {
        backgroudImage.color = newColor;
    }

    public void ChangeNameText(string textString)
    {
        objectName.text = textString;
    }

    public void ChangeConditionText(string textString)
    {
        conditionText.text = textString;
    }

    public void ChangeAffordanceText(string textString)
    {
        affordanceText.text = textString;
    }

}
