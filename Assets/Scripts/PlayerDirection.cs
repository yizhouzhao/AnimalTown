using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PlayerDirection : MonoBehaviour
{

    public Camera player_camera;
    public GameObject effect_click_prefab;
    //int layerMask = 1 << 10;

    //moving control
    private bool isMoving = false;
    public Vector3 targetPosition;
    private PlayerMove playerMove;
    

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = this.transform.position;
        playerMove = GetComponent<PlayerMove>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = player_camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            bool isCollider = Physics.Raycast(ray, out hitInfo, Mathf.Infinity);
            if(isCollider && hitInfo.collider.CompareTag("Ground"))
            {
                //mouse click effect
                isMoving = true;
                
                //Disable click effect
                //ShowClickEffect(hitInfo.point);
                LookAtTarget(hitInfo.point);
                    
            }
            //if(isCollider && hitInfo.collider.CompareTag("Agent"))
            //{
            //    Debug.Log("PlayerDirection: Agent clicked");
            //    AnimalCharacter animal_character = hitInfo.transform.gameObject.GetComponent<AnimalCharacter>();
            //    animal_character.StartConversationWithPlayer(this.transform.position);
            //}
        }

        if (Input.GetMouseButtonUp(0))
        {
            isMoving = false;
        }

        if (isMoving)
        {
            //get destination and move to the direction
            Ray ray = player_camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            bool isCollider = Physics.Raycast(ray, out hitInfo);
            if (isCollider && hitInfo.collider.CompareTag("Ground"))
            {
                LookAtTarget(hitInfo.point);
            }
        }
        else
        {
            if (playerMove.isMoving)
            {
                LookAtTarget(targetPosition);
            }
        }
    }

    void ShowClickEffect(Vector3 hitPoint)
    {
        hitPoint = new Vector3(hitPoint.x, hitPoint.y + 0.1f, hitPoint.z);
        GameObject.Instantiate(effect_click_prefab, hitPoint, Quaternion.identity);
    }


    //set player character target position
    void LookAtTarget(Vector3 hitPoint)
    {
        targetPosition = hitPoint;
        targetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        this.transform.LookAt(targetPosition);
    }
}
