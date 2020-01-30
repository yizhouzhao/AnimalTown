using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APickupObject : MonoBehaviour
{
    //object type
    public EPickupObject objectType;

    //occupied
    public bool occupied = false;

    //
    public bool eatable;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            Debug.Log("APickupObject enters");
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if(animalCharacter.meetPickupObject == null && !occupied)
            {
                animalCharacter.meetPickupObject = this;
                occupied = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            Debug.Log("APickupObject exits");
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if(ReferenceEquals(animalCharacter.meetPickupObject, this))
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

    public void Drop(AnimalCharacter animalCharacter)
    {
        MakeDynamic();
        GetComponent<Rigidbody>().AddForce(animalCharacter.transform.forward * 10f);
        this.occupied = false;
        animalCharacter.bHoldObject = false;
        animalCharacter.holdObject = null;
    }
}
