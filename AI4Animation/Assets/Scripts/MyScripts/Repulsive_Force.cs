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
    private Vector3 force;
    private Vector3 a;
    private float targetMass = 0.001f;
    

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("IKtarget");
        d = Mathf.Sqrt(Vector3.SqrMagnitude(transform.position - target.transform.position));
    }
    // Update is called once per frame
    void Update()
    {
        float x = target.transform.position.x;
        float y = target.transform.position.y;
        float z = target.transform.position.z;

        d = Mathf.Sqrt(Vector3.SqrMagnitude(transform.position - target.transform.position));

        r = Mathf.Sqrt(x * x + y * y + z * z);
        th = Mathf.Atan2(Mathf.Sqrt(x * x + y * y), z);
        f = Mathf.Atan2(y, x);

        target.transform.position = (new Vector3(r * Mathf.Sin(th) * Mathf.Cos(f), r * Mathf.Sin(th) * Mathf.Sin(f), r * Mathf.Cos(th)));
    }
    //private void FixedUpdate()
    //{
    //    force = (1 / (d * d)) * new Vector3(1, 1, 1);
    //    a = Vector3.Scale(force, new Vector3(1 / targetMass, 1 / targetMass, 1 / targetMass));
    //    target.transform.position = 0.5f * a;

    //}
}
