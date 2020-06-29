using System.Collections;
using System.Collections.Generic;
using SIGGRAPH_2018;
using UnityEngine;

public class Collision_Resolve : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {

        BioAnimation_Wolf parentscript = transform.root.GetComponent<BioAnimation_Wolf>();
        if (collision.collider.name == "targetObj(Clone)")
        {
            parentscript.colTime = Time.time;
            parentscript.collisionFlag = false;

        }
        //Debug.Log(collision.collider.name);
        //Debug.Log(transform.root.name);
    }
}
 