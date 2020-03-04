using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMindInfoPanel : MonoBehaviour
{
    public Text timeText;
    public Text LocationText;
    public Text infoNameText;

    public void SetInfoFromMind(MindInfo mindInfo)
    {
        PickupObjectInfo poInfo = mindInfo as PickupObjectInfo;
        if (poInfo != null)
        {
            infoNameText.text = poInfo.objectType.ToString();
        }

        SceneToolInfo scInfo = mindInfo as SceneToolInfo;
        if (scInfo != null)
        {
            infoNameText.text = scInfo.sceneType.ToString();
        }

        LocationText.text = mindInfo.recordPosition.x.ToString("000") + "," + mindInfo.recordPosition.z.ToString("000");

        float infoTime = mindInfo.recordTime;
        int min = (int)infoTime % 3600 / 60;
        int sec = (int)infoTime % 60;

        timeText.text = min.ToString("00") + ":" + sec.ToString("00");
    }


}
