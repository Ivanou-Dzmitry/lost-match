using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialElements : MonoBehaviour
{

    private GoalManager goalManagerClass;

    public int hitPoints;

    [Header("Sound")]
    public AudioClip elementSound;

    [Header("Particles")]
    public GameObject destroyParticle;

    //for mylti hits objects
    [Header("Layers")]
    public GameObject objLayer02;
    public GameObject objLayer03;


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

                goalManagerClass.CompareGoal(tagForCompare);

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
        //hide if 1 hitpoint
        if (objLayer02 != null && hitPoints == 1)
        {
            objLayer02.gameObject.SetActive(false);
        }

        //hide if 2 hitpoint
        if (objLayer02 != null && hitPoints == 2)
        {
            objLayer02.gameObject.SetActive(false);
        }
    }
}
