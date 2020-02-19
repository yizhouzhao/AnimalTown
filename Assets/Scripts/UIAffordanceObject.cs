using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAffordanceObject : MonoBehaviour
{
    public Image backgroudImage;
    public Text objectName;

    public void ChangeBackGroudColor(Color newColor)
    {
        backgroudImage.color = newColor;
    }

    public void ChangeText(string textString)
    {
        objectName.text = textString;
    }
}
