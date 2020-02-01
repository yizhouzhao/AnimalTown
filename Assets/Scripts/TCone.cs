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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //double direction
            Debug.Log("Animal Character meet another: " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if (this.owner.meetAnimalCharacter == null && animalCharacter.meetAnimalCharacter == null)
            {
                this.owner.meetAnimalCharacter = animalCharacter;
                animalCharacter.meetAnimalCharacter = this.owner;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //double direction
            Debug.Log("Animal Character exit another: " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            if (this.owner.meetAnimalCharacter && animalCharacter.meetAnimalCharacter)
            {
                if(ReferenceEquals(this.owner.meetAnimalCharacter, animalCharacter))
                {
                    owner.meetAnimalCharacter = null;
                    animalCharacter.meetAnimalCharacter = null;
                }
            }
        }
    }
}
