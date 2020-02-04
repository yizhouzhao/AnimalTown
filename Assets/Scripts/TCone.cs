using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCone : MonoBehaviour
{
    //Who owns the vision cone
    public AnimalCharacter owner;

    // Start is called before the first frame update
    void Start()
    {
        owner = this.transform.parent.GetComponent<AnimalCharacter>();    
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //double direction
            Debug.Log("Tcone Animal Character meet another: " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            //both sides
            if (this.owner.meetAnimalCharacter == null && animalCharacter.meetAnimalCharacter == null)
            {
                this.owner.meetAnimalCharacter = animalCharacter;
                animalCharacter.meetAnimalCharacter = this.owner;
            }

            //one side
            //if (this.owner.meetAnimalCharacter == null)
            //{
            //    this.owner.meetAnimalCharacter = animalCharacter;
            //}
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if (owner.bInActivity || animalCharacter.bInActivity)
            {
                return;
            }
            //double direction
            Debug.Log("Tcone Animal Character exit another : " + other.gameObject.name);
            

            //both sides
            if (this.owner.meetAnimalCharacter && animalCharacter.meetAnimalCharacter)
            {
                if (ReferenceEquals(this.owner.meetAnimalCharacter, animalCharacter))
                {
                    owner.meetAnimalCharacter = null;
                    animalCharacter.meetAnimalCharacter = null;
                }
            }

            //one side
            //if (this.owner.meetAnimalCharacter)
            //{
            //    if (ReferenceEquals(this.owner.meetAnimalCharacter, animalCharacter))
            //    {
            //        owner.meetAnimalCharacter = null;
            //    }
            //}
        }
    }
}
