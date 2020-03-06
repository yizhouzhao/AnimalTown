using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCone : MonoBehaviour
{
    //Who owns the vision cone
    public AnimalCharacter owner;

    public AnimalCharacter lastMeetCharacter;

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
            
            AnimalCharacter otherAnimalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            lastMeetCharacter = otherAnimalCharacter;


            //mind control
            RLAOGControl aogControl = owner.gameObject.GetComponent<RLAOGControl>();
            aogControl.mind.UpdateCharacterInfo(otherAnimalCharacter);
            
            //common mind
            if(ReferenceEquals(otherAnimalCharacter.meetAnimalCharacter, this.owner))
            {
                aogControl.mind.UpdateCommonMind(otherAnimalCharacter, this.owner);
            }


            //both sides
            if (this.owner.meetAnimalCharacter == null && otherAnimalCharacter.meetAnimalCharacter == null)
            {
                if ((!this.owner.bInActivity) && (!otherAnimalCharacter.bInActivity))
                {
                    //Debug.Log("Tcone "+ owner.name + "Animal Character meet another: " + other.gameObject.name);
                    this.owner.meetAnimalCharacter = otherAnimalCharacter;
                    otherAnimalCharacter.meetAnimalCharacter = this.owner;

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
