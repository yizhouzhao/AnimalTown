using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EAnimalIslandGameModeType {Supervise, Wander};

public class PlayerGameMode : MonoBehaviour
{
    public EAnimalIslandGameModeType AnimalIslandGameModeType = EAnimalIslandGameModeType.Supervise;
    // Start is called before the first frame update
    void Start()
    {
        if(AnimalIslandGameModeType == EAnimalIslandGameModeType.Supervise)
        {
            Transform player = GameObject.Find("Player").transform;
            Destroy(player);

            Transform playerCamera = GameObject.Find("PlayerCamera").transform;
            playerCamera.gameObject.SetActive(false);

            Transform mainCamera = GameObject.Find("MainCamera").transform;
            mainCamera.gameObject.SetActive(true);

        }
        else if (AnimalIslandGameModeType == EAnimalIslandGameModeType.Wander)
        {
            Transform playerCamera = GameObject.Find("PlayerCamera").transform;
            playerCamera.gameObject.SetActive(true);

            Transform mainCamera = GameObject.Find("MainCamera").transform;
            mainCamera.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
