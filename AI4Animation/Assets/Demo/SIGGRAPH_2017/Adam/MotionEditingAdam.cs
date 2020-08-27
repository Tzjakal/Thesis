using UnityEngine;
using System;
using System.Collections.Generic;

namespace SIGGRAPH_2018
{
    [RequireComponent(typeof(Actor))]
    [RequireComponent(typeof(BioAnimation_Adam))]
    public class MotionEditingAdam : MonoBehaviour
    {

        public FootIK[] LegSolvers;

        private Actor Actor;
        private BioAnimation_Adam Animation;

        private float Stability;

        private Actor GetActor()
        {
            if (Actor == null)
            {
                Actor = GetComponent<Actor>();
            }
            return Actor;
        }

        private BioAnimation_Adam GetAnimation()
        {
            if (Animation == null)
            {
                Animation = GetComponent<BioAnimation_Adam>();
            }
            return Animation;
        }

        public void Process()
        {
           

            Vector3[] pivotPositions = new Vector3[LegSolvers.Length];
            Quaternion[] pivotRotations = new Quaternion[LegSolvers.Length];
            for (int i = 0; i < LegSolvers.Length; i++)
            {
                pivotPositions[i] = LegSolvers[i].GetPivotPosition();
                pivotRotations[i] = LegSolvers[i].GetPivotRotation();
            }

            Transform hips = Array.Find(GetActor().Bones, x => x.Transform.name == "Hips").Transform;
            Transform spine = Array.Find(GetActor().Bones, x => x.Transform.name == "Chest").Transform;
            Transform neck = Array.Find(GetActor().Bones, x => x.Transform.name == "Neck").Transform;
            Transform leftShoulder = Array.Find(GetActor().Bones, x => x.Transform.name == "LeftHip").Transform;
            Transform rightShoulder = Array.Find(GetActor().Bones, x => x.Transform.name == "RightHip").Transform;

            Vector3 hipsPosition = hips.position;
            Vector3 spinePosition = spine.position;
            Vector3 neckPosition = neck.position;
            Vector3 leftShoulderPosition = leftShoulder.position;
            Vector3 rightShoulderPosition = rightShoulder.position;

            float spineHeight = Utility.GetHeight(spine.position, LayerMask.GetMask("Ground"));
            float neckHeight = Utility.GetHeight(neck.position, LayerMask.GetMask("Ground"));
            float leftShoulderHeight = Utility.GetHeight(leftShoulder.position, LayerMask.GetMask("Ground"));
            float rightShoulderHeight = Utility.GetHeight(rightShoulder.position, LayerMask.GetMask("Ground"));
            hips.rotation = Quaternion.Slerp(hips.rotation, Quaternion.FromToRotation(neckPosition - hipsPosition, new Vector3(neckPosition.x, neckHeight + (neckPosition.y - transform.position.y), neckPosition.z) - hipsPosition) * hips.rotation, 0.5f);
            spine.rotation = Quaternion.Slerp(spine.rotation, Quaternion.FromToRotation(neckPosition - spinePosition, new Vector3(neckPosition.x, neckHeight + (neckPosition.y - transform.position.y), neckPosition.z) - spinePosition) * spine.rotation, 0.5f);
            spine.position = new Vector3(spinePosition.x, spineHeight + (spinePosition.y - transform.position.y), spinePosition.z);
            neck.position = new Vector3(neckPosition.x, neckHeight + (neckPosition.y - transform.position.y), neckPosition.z);
            leftShoulder.position = new Vector3(leftShoulderPosition.x, leftShoulderHeight + (leftShoulderPosition.y - transform.position.y), leftShoulderPosition.z);
            rightShoulder.position = new Vector3(rightShoulderPosition.x, rightShoulderHeight + (rightShoulderPosition.y - transform.position.y), rightShoulderPosition.z);

            for (int i = 0; i < LegSolvers.Length; i++)
            {
                LegSolvers[i].Solve(pivotPositions[i], pivotRotations[i], Stability);
            }
        }

        public float GetStability()
        {
            return Stability;
        }

    }
}