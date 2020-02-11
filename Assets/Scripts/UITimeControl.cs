using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITimeControl : MonoBehaviour
{
    public Text Sec;
    public Text Min;
    public Text Hour;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int second = (int)(Time.time) % 60;
        int minute = (int)(Time.time) % 3600 / 60;
        int hour = (int)(Time.time) / 3600;

        Sec.text = second.ToString("00");
        Min.text = minute.ToString("00");
        Hour.text = hour.ToString("00");
    }
}
