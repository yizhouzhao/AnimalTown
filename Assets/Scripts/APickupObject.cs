using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APickupObject : MonoBehaviour
{
    //object type
    public EPickupObject objectType;

    //occupied
    public bool occupied = false;

    //Eatable?
    public bool eatable = false;

    private void OnTriggerEnter(Collider other)
    {
        //if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        if(other.gameObject.tag == "Vision")
        {
            Debug.Log("APickupObject enters");
            //A character/an agent sees a pickup object
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<TCone>().owner;
            if(animalCharacter.meetPickupObject == null && !occupied)
            {
                animalCharacter.meetPickupObject = this;
                occupied = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        if (other.gameObject.tag == "Vision")
        {
            Debug.Log("APickupObject exits");
            //A character/an agent leaves a pickup object
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<TCone>().owner;
            if (ReferenceEquals(animalCharacter.meetPickupObject, this))
            {
                animalCharacter.meetPickupObject = null;
                occupied = false;
            }  
        }
    }

    //make the object static without collision and rigidbody
    public void MakeStatic()
    {
        Rigidbody rBody = GetComponent<Rigidbody>();
        GetComponent<BoxCollider>().enabled = false;
        GetComponent<MeshCollider>().enabled = false;
        rBody.useGravity = false;
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
    }

    //make the object with collision and rigidbody
    public void MakeDynamic()
    {
        transform.parent = null;
        GetComponent<BoxCollider>().enabled = true;
        GetComponent<MeshCollider>().enabled = true;
        GetComponent<Rigidbody>().useGravity = true;
    }

    //Pick up object
    public void Pickup(AnimalCharacter animalCharacter)
    {
        Debug.Log("APickupObject Pickup");
        MakeStatic();
        this.transform.position = animalCharacter.holdTransform.position;
        this.transform.parent = animalCharacter.holdTransform;
        animalCharacter.bHoldObject = true;
        animalCharacter.meetPickupObject = null;
        animalCharacter.holdObject = this;

    }

    //Drop object
    public void Drop(AnimalCharacter animalCharacter)
    {
        MakeDynamic();
        GetComponent<Rigidbody>().AddForce(animalCharacter.transform.forward * 10f);
        this.occupied = false;
        animalCharacter.bHoldObject = false;
        animalCharacter.holdObject = null;
    }


}
