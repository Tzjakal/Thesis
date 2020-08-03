using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowCatch : MonoBehaviour
{
    public GameObject proptarget;
    public GameObject propPad;
    // Start is called before the first frame update
    public GameObject targetObj;

    private GameObject ball;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            NewBall();
    }
    void NewBall()
    {
        ball = Instantiate(targetObj, new Vector3(propPad.transform.position.x, propPad.transform.position.y + 0.2f, propPad.transform.position.z), Quaternion.Euler(0, 0, 45));
        proptarget.transform.position = transform.position - propPad.transform.position;

    }
    private void OnCollisionEnter(Collision collision)
    {

    }
}
