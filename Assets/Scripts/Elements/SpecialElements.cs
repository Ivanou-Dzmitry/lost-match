using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpecialElements : MonoBehaviour
{

    private GoalManager goalManagerClass;

    public int hitPoints;

    [Header("Sound")]
    public AudioClip[] elementSounds;

    [Header("Particles")]
    public GameObject[] elementParticles;

    //for mylti hits objects
    [Header("Layers")]
    public GameObject[] elementLayers;


    // Start is called before the first frame update
    void Start()
    {
        //classes
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (hitPoints <= 0)
        {
            //for goals for breakable
            if (goalManagerClass != null)
            {
                string tagForCompare = this.gameObject.tag;

                int Column = (int)this.gameObject.transform.position.x;
                int Row = (int)this.gameObject.transform.position.y;

                //hack for various breakable
                if (this.gameObject.tag == "breakable_02" || this.gameObject.tag == "breakable_03" && this.gameObject.tag != null)
                {
                    tagForCompare = "breakable_01";
                }

                //hack for various blockers
                if (this.gameObject.tag == "blocker_02" || this.gameObject.tag == "blocker_03" && this.gameObject.tag != null)
                {
                    tagForCompare = "blocker_01";
                }

                goalManagerClass.CompareGoal(tagForCompare, Column, Row);

                goalManagerClass.UpdateGoals();
            }

            Destroy(this.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;

        LayerManager();
    }

    void LayerManager()
    {
        //hide layers
        for (int i = 0; i < elementLayers.Length; i++)
        {
            if (elementLayers[i] != null && hitPoints == i + 1)
            {
                elementLayers[i].gameObject.SetActive(false);
            }
        }

    }
}
