using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASoil : ASceneTool
{
    public bool hasPlants;
    // Start is called before the first frame update
    void Start()
    {
        hasPlants = false;
    }

    //Its own enter trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            Debug.Log("Ascenetool: " + this.toolName + " enters: " + other.gameObject.name);
            //Still have place
            if (animalCharacterNames.Count < maxCapacity)
            {
                AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
                animalCharacterNames.Add(animalCharacter.characterName);
                animalCharacter.sceneTool = this;
            }
        }

        else if (other.gameObject.tag == "PickupObject")
        {
            ASeed seed = other.gameObject.GetComponent<APickupObject>() as ASeed;
            if (seed && !hasPlants)
            {
                //Plant
                Plant(seed);
            }
        }
    }

    //Plant seed on it
    void Plant(ASeed seed)
    {
        hasPlants = true;
        seed.occupied = true;
        seed.MakeStatic();
        seed.transform.parent = this.transform;
        seed.transform.localPosition = Vector3.zero;

        GetComponent<BoxCollider>().enabled = false;

      
        StartCoroutine(GrowSeed(seed));

        //Grow seed to a tree
        IEnumerator GrowSeed(ASeed aSeed)
        {
            Debug.Log("Soil Plant: " + seed.objectType.ToString());

            yield return new WaitForSeconds(aSeed.growTime);

            //A seed becomes a tree;
            GameObject tree = Instantiate(seed.plantPrefab, this.transform.position, Quaternion.identity);

            tree.transform.parent = this.transform;

            //calculate size
            Vector3 boxSize = tree.GetComponent<BoxCollider>().size;
            Vector3 soilSize = this.GetComponent<BoxCollider>().size;

            float treeSize = soilSize.x / (boxSize.x + 0.1f);
            tree.transform.localScale = new Vector3(treeSize, treeSize, treeSize);

            //Distroy seed and soil
            Destroy(aSeed.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "Agent")
        {
            Debug.Log("Ascenetool: " + this.toolName + " exits: " + other.gameObject.name);
            AnimalCharacter animalCharacter = other.gameObject.GetComponent<AnimalCharacter>();
            animalCharacterNames.Remove(animalCharacter.characterName);

            if (ReferenceEquals(animalCharacter.sceneTool, this))
            {
                animalCharacter.sceneTool = null;
            }
        }
    }


    public override void Interact(AnimalCharacter animalCharacter)
    {
        //if have seed in hand plant seed
        if (animalCharacter.holdObject != null)
        {
            ASeed seed = animalCharacter.holdObject.GetComponent<APickupObject>() as ASeed;
            if (seed)
            {
                seed.Drop(animalCharacter);
                Plant(seed);
            }

        }
        animalCharacter.SetIdle();
    }
}
