using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision_Test : MonoBehaviour
{
    public GameObject target;
    public GameObject obstacle;
    public Rigidbody rb;
    public Rigidbody targetRb;
    public bool attract;
    private Vector3 currPos;
    private Vector3 lastPos;
    private bool moved = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetRb = target.GetComponent<Rigidbody>();
        lastPos = obstacle.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        currPos = obstacle.transform.position;
        if (currPos != lastPos)
            moved = true;
        else
            moved = false;

        lastPos = currPos;
    }

    private void FixedUpdate()
    {
        float attrForceMagn;
        Vector3 direction;

        if (attract)
            direction = 10 * (transform.position - target.transform.position);
        else
            direction = 10 * (target.transform.position - transform.position);

        if (direction.magnitude > 1)
            attrForceMagn = 0.5f * (targetRb.mass * rb.mass) / (direction.magnitude * direction.magnitude);
        else
            attrForceMagn = 0;
        Vector3 attrForce = direction.normalized * attrForceMagn;
        if (moved)
            targetRb.AddForce(attrForce, ForceMode.Impulse);
        else
            targetRb.velocity = Vector3.zero;

        Debug.Log(direction.magnitude);
    }
}
