using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject bullet;
    public GameObject aim;
    public GameObject gunbarrel;
    public float Force = 5000.0f;

    private GameObject origin;
    void Start()
    {
        origin = bullet;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1"))
        {
            ShootBullet();
        }
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("HitTarget");

        DestroyOldBullets(bullets);
    }

    void ShootBullet()
    {
        bullet = Instantiate(origin, gunbarrel.transform.position, Quaternion.FromToRotation(Vector3.up, transform.forward));
        bullet.GetComponent<Rigidbody>().AddForce((gunbarrel.transform.position - aim.transform.position) * Force, ForceMode.Acceleration);
    }

    void DestroyOldBullets(GameObject[] oldBullets)
    {
        for(int i = 0; i < oldBullets.Length; i ++)
        {
            if (Vector3.Magnitude(aim.transform.position - oldBullets[i].transform.position) > 8.0f)
                GameObject.Destroy(oldBullets[i]);
        }
        
    }
}
