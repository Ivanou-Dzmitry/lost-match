using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Element : MonoBehaviour
{
    public int hitPoints;
    private GoalManager goalManagerClass;

    // Start is called before the first frame update
    void Start()
    {
        //classes
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }

}
