using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class TargetAimIk : MonoBehaviour
{
    public AimIK aimIK;
    public Transform target;
    public Vector3 offset;

    private Vector3 startPos;

    void Start()
    {
        startPos = target.position;    
    }
    void Update()
    {
        offset = target.position - startPos;
    }
    void LateUpdate()
    {
        aimIK.solver.transform.LookAt(target.position);

        aimIK.solver.IKPosition = target.position + offset + new Vector3(0, 1.0f, 0);
    }
}
