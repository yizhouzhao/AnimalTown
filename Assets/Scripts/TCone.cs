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
}
