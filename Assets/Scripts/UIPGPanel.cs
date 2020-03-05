using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPGPanel : MonoBehaviour
{
    public Text timeText;
    public Text activitText;

    public Slider healthSlide;
    public Slider energySlide;
    public Slider moneySlide;

    public void SetContentFromPGNode(PGNode pgNode)
    {
        float pgTime = pgNode.pgTime;
        int hour = (int)pgTime / 3600;
        int min = (int)pgTime % 3600 / 60;
        int sec = (int)pgTime % 60;

        timeText.text =  min.ToString("00") + ":" + sec.ToString("00");
        activitText.text = pgNode.activityType.ToString();
        healthSlide.value = pgNode.fullness;
        energySlide.value = pgNode.energy;
        moneySlide.value = pgNode.money;
    }
}
