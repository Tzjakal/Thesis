using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repulsive_Force : MonoBehaviour
{
    private GameObject target;
    private float d;
    private float r;
    private float th;
    private float f;
    

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("IKtarget");
        d = Mathf.Sqrt(Vector3.SqrMagnitude(transform.position - target.transform.position));


    }
    // Update is called once per frame
    void Update()
    {
        float x = transform.position.x;
        float y = transform.position.y;
        float z = transform.position.z;

        d = Mathf.Sqrt(Vector3.SqrMagnitude(transform.position - target.transform.position));

        r = Mathf.Sqrt(x * x + y * y + z * z);
        th = Mathf.Atan2(Mathf.Sqrt(x * x + y * y), z);
        f = Mathf.Atan2(y, x);

        target.transform.position = (new Vector3(r * Mathf.Sin(th) * Mathf.Cos(f), r * Mathf.Sin(th) * Mathf.Sin(f), r * Mathf.Cos(th)));
    } 
}
