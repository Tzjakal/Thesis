using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class Fabrik_Enable : MonoBehaviour
{
    // Start is called before the first frame update
    public FABRIK[] fabrik1;
    private GameObject target;
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("IKtarget");
        fabrik1 = GetComponents<FABRIK>();
        Debug.Log(fabrik1.Length);  
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("target"))
        {
            fabrik1[0].solver.target = GameObject.FindGameObjectWithTag("target").transform;
            if (Vector3.Magnitude(fabrik1[0].solver.target.position - transform.position) < 1.1f)
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[i].enabled = true;
            }
            else
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[i].enabled = false;
            }
        }
        else
        {
            if (Vector3.Magnitude(target.transform.position - transform.position) < 1.1f)
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[i].enabled = true;
            }
            else
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[i].enabled = false;
            }
        }
        
        
    }
}
