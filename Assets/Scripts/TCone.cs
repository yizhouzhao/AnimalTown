using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCone : MonoBehaviour
{
    //Who owns the vision cone
    public AnimalCharacter owner;

    private float meetCharacterStayTime;

    // Start is called before the first frame update
    void Start()
    {
        owner = this.transform.parent.GetComponent<AnimalCharacter>();
        Debug.Log("Tcone: owner");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //double direction
            
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            //both sides
            if (this.owner.meetAnimalCharacter == null && animalCharacter.meetAnimalCharacter == null)
            {
                if ((!this.owner.bInActivity) && (!animalCharacter.bInActivity))
                {
                    //Debug.Log("Tcone "+ owner.name + "Animal Character meet another: " + other.gameObject.name);
                    this.owner.meetAnimalCharacter = animalCharacter;
                    animalCharacter.meetAnimalCharacter = this.owner;

                    meetCharacterStayTime = 2 * owner.tradeWaitTime;
                }
            }

            //one side
            //if (this.owner.meetAnimalCharacter == null)
            //{
            //    this.owner.meetAnimalCharacter = animalCharacter;
            //}
        }
    }

    private void Update()
    {
        //if (owner.meetAnimalCharacter != null)
        //{
        //    meetCharacterStayTime -= Time.time;
        //    if (meetCharacterStayTime < 0f)
        //    {
        //        //Clean
        //        owner.meetAnimalCharacter = null;
        //        meetCharacterStayTime = meetCharacterStayTime = 2 * owner.tradeWaitTime;
        //    }
        //}
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            //Debug.Log("Player Agent Tcone " + name + " Animal Character exit another : " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            RLAOGControl aogControl = animalCharacter.gameObject.GetComponent<RLAOGControl>();
            aogControl.mind.UpdateCharacterInfo(animalCharacter);

            if ((owner.agreeCommunication || animalCharacter.agreeCommunication) && ReferenceEquals(this.owner.meetAnimalCharacter, animalCharacter))
            {
                //if (ReferenceEquals(owner, owner.meetAnimalCharacter.meetAnimalCharacter))
                {
                    return;
                }
            }

            //both sides
            if (this.owner.meetAnimalCharacter && animalCharacter.meetAnimalCharacter)
            {
                //Debug.Log("Player Agent Tcone 2222" + name + " Animal Character exit another : " + other.gameObject.name);
                if (ReferenceEquals(this.owner.meetAnimalCharacter, animalCharacter) && ReferenceEquals(animalCharacter.meetAnimalCharacter, this.owner))
                {
                    //Debug.Log("Tcone " + owner.name + " Animal Character exit another : " + other.gameObject.name);
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
