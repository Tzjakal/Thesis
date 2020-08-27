using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class Fabrik_Enable : MonoBehaviour
{
    // Start is called before the first frame update
    public FABRIK[] fabrik1;
   
   
    void Start()
    {
        fabrik1 = GetComponents<FABRIK>();
        for (int i = 0; i < fabrik1.Length; i++)
            fabrik1[i].enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("HitTarget"))
        {
            fabrik1[0].solver.target = GameObject.FindGameObjectWithTag("HitTarget").transform;
            float distance = Vector3.Magnitude(fabrik1[0].solver.target.position - transform.GetChild(0).GetChild(0).GetChild(0).transform.position);
            
             
            if (distance < 4.0f)
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[0].enabled = true;                                               //αντί για i--->0 αν θέλουμε να απενεργοποιήσουμε τα ΙΚs στα πόδια...
                fabrik1[0].solver.IKPositionWeight = Mathf.Pow(50000.0f, -distance/4.0f);
            }
            else
            {
                for (int i = 0; i < fabrik1.Length; i++)
                    fabrik1[i].enabled = false;
            }
        }
    }
}
