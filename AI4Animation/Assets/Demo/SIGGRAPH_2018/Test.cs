using System.Collections;
using System.Collections.Generic;
using SIGGRAPH_2018;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Test : MonoBehaviour
{
    public GameObject targetObj;
    public float force = 100.0f;
    //public GameObject proptarget;
    //public GameObject propPad;
 

    private GameObject rock;
    private Vector3 Adam;
    private Vector3 Wolf;
    private Vector3 dir;
    private LineRenderer lineRenderer;

   
    // Start is called before the first frame update
    void Start()
    {
      

    }
    void Update()
    {
        if (Input.GetButtonDown("CreateRandom"))
        {
            Adam = GameObject.FindGameObjectWithTag("adam").transform.position;
            Wolf = GameObject.FindGameObjectWithTag("wolf").transform.position;


            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.enabled = false;

            if (rock)
                GameObject.Destroy(rock);
            rock = Instantiate(targetObj, new Vector3(Adam.x, 1.5f, Adam.z), Quaternion.identity);
                
        }

        if (Input.GetButton("CreateRandom"))
        {
           
            if (Input.GetKey(KeyCode.J))
            {
                Wolf.x -= 0.04f;
            }
            if (Input.GetKey(KeyCode.L))
            {
                Wolf.x += 0.04f;
            }
            if (Input.GetKey(KeyCode.I))
            {
                Wolf.z += 0.04f;
            }
            if (Input.GetKey(KeyCode.K))
            {
                Wolf.z -= 0.04f;
            }

            dir = new Vector3(Wolf.x, Wolf.y + 0.2f, Wolf.z) - rock.transform.position;

            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, rock.transform.position);
            lineRenderer.SetPosition(1, new Vector3(Wolf.x, Wolf.y + 0.2f, Wolf.z));
            lineRenderer.enabled = true;
        }

        if (Input.GetButtonUp("CreateRandom"))
        {
            Destroy(lineRenderer);
            rock.GetComponent<Rigidbody>().AddForce(dir * force, ForceMode.Impulse);

        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        GameObject.Destroy(rock);
    }

    
}
