using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Polycrime
{
    public class PropulsionTarget : MonoBehaviour
    {
        private Vector3 move;
        [Range(0.1f, 1.0f)]
        public float size = 0.2f;
        public Color color = Color.red;
        

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, size);
        }
        private void Start()
        {
            move = transform.position;
            
        }
        private void Update()
        {
            if (Input.GetKey(KeyCode.J))
            {
                move.x += 0.04f;
            }
            if (Input.GetKey(KeyCode.L))
            {
                move.x -= 0.04f;
            }
            if (Input.GetKey(KeyCode.I))
            {
                move.z += 0.04f;
            }
            if (Input.GetKey(KeyCode.K))
            {
                move.z -= 0.04f;
            }
            transform.localPosition = move;
            
        }
    }
}