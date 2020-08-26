using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowCatch : MonoBehaviour
{
    public GameObject targetObj;
    public GameObject proptarget;
    public GameObject propPad;

    private GameObject ball;


    void Start()
    {
        proptarget = GameObject.FindGameObjectWithTag("proptarget");
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameObject.FindGameObjectWithTag("target"))
        {
            if (Input.GetButtonDown("Fire1"))
                NewBall();
            propPad = GameObject.FindGameObjectWithTag("propPad");
        }
    }
    void NewBall()
    {
        ball = Instantiate(targetObj, new Vector3(propPad.transform.position.x, propPad.transform.position.y + 0.2f, propPad.transform.position.z), Quaternion.Euler(0, 0, 45));
        proptarget.transform.position = new Vector3(Random.Range(propPad.transform.position.x - 10.0f, propPad.transform.position.x + 10.0f), 2.72f, Random.Range(propPad.transform.position.z + 10.0f, propPad.transform.position.z + 10.0f));



    }
    private void OnCollisionEnter(Collision collision)
    {
        if(ball)
        {
            if (collision.gameObject == ball.gameObject)
                Destroy(ball.gameObject);
        }
    }
}
