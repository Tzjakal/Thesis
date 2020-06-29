using UnityEngine;
using System;
using System.Collections.Generic;
using DeepLearning;
using System.Security.Cryptography;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SIGGRAPH_2018 {
	[RequireComponent(typeof(Actor))]
	public class BioAnimation_Wolf : MonoBehaviour {

        public bool collisionFlag = true;
        public float colTime = 0;


		public bool Inspect = false;
        
        public bool ShowTrajectory = true;
		public bool ShowVelocities = true;

		public float TargetGain = 0.25f;
		public float TargetDecay = 0.05f;
		public bool TrajectoryControl = true;
		public float TrajectoryCorrection = 1f;

		public Controller Controller;

		public Actor Actor;
		private MANN NN;
		private Trajectory Trajectory;
        
        private Vector3 TargetDirection;
		private Vector3 TargetVelocity;
        private PIDController PID;

		//State
		private Vector3[] Positions = new Vector3[0];
		private Vector3[] Forwards = new Vector3[0];
		private Vector3[] Ups = new Vector3[0];
		private Vector3[] Velocities = new Vector3[0];

		//NN Parameters
		private const int TrajectoryDimIn = 13;
		private const int TrajectoryDimOut = 6;
		private const int JointDimIn = 12;
		private const int JointDimOut = 12;

		//Trajectory for 60 Hz framerate
		private const int Framerate = 60;
		private const int Points = 111;
		private const int PointSamples = 12;
		private const int PastPoints = 60;
		private const int FuturePoints = 50;
		private const int RootPointIndex = 60;
		private const int PointDensity = 10;

		//Post-Processing
		private MotionEditing MotionEditing;
		
		//GUI
		private Texture Forward, Left, Right, Back, TurnLeft, TurnRight, Disc;
		private Texture Idle, Move, Jump, Sit, Lie, Stand;
		private GUIStyle FontStyle;
		
		//Performance
		private float NetworkPredictionTime;
       
        void Reset() {
			Controller = new Controller();
		}
        
		void Awake() {
			Actor = GetComponent<Actor>();
			NN = GetComponent<MANN>();
			MotionEditing = GetComponent<MotionEditing>();
            TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
			TargetVelocity = Vector3.zero;
			PID = new PIDController(0.2f, 0.8f, 0f);
			Positions = new Vector3[Actor.Bones.Length];
			Forwards = new Vector3[Actor.Bones.Length];
			Ups = new Vector3[Actor.Bones.Length];
			Velocities = new Vector3[Actor.Bones.Length];
			Trajectory = new Trajectory(Points, Controller.GetNames(), transform.position, TargetDirection);
           
			if(Controller.Styles.Length > 0) {
				for(int i=0; i<Trajectory.Points.Length; i++) {
					Trajectory.Points[i].Styles[0] = 1f;
				}
			}
			for(int i=0; i<Actor.Bones.Length; i++) {
				Positions[i] = Actor.Bones[i].Transform.position;
				Forwards[i] = Actor.Bones[i].Transform.forward;
				Ups[i] = Actor.Bones[i].Transform.up;
				Velocities[i] = Vector3.zero;
			}
			if(NN.Parameters == null) {
				Debug.Log("No parameters saved.");
				return;
			}
			NN.LoadParameters();
           
		}

		void Start() {
			Utility.SetFPS(60);

            Forward = (Texture)Resources.Load("Forward");
			Left = (Texture)Resources.Load("Left");
			Right = (Texture)Resources.Load("Right");
			Back = (Texture)Resources.Load("Back");
			TurnLeft = (Texture)Resources.Load("TurnLeft");
			TurnRight = (Texture)Resources.Load("TurnRight");
			Disc = (Texture)Resources.Load("Disc");
			Idle = (Texture)Resources.Load("Idle");
			Move = (Texture)Resources.Load("Move");
			Jump = (Texture)Resources.Load("Jump");
			Sit = (Texture)Resources.Load("Sit");
			Lie = (Texture)Resources.Load("Lie");
			Stand = (Texture)Resources.Load("Stand");
			FontStyle = new GUIStyle();
			FontStyle.font = (Font)Resources.Load("Fonts/Coolvetica");
			FontStyle.normal.textColor = Color.white;
			FontStyle.alignment = TextAnchor.MiddleCenter;
        }

        public Trajectory GetTrajectory() {
			return Trajectory;
		}

		public void Reinitialise() {
			transform.position = Vector3.zero;
			TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
			TargetVelocity = Vector3.zero;
			PID = new PIDController(0.2f, 0.8f, 0f);
			Positions = new Vector3[Actor.Bones.Length];
			Forwards = new Vector3[Actor.Bones.Length];
			Ups = new Vector3[Actor.Bones.Length];
			Velocities = new Vector3[Actor.Bones.Length];
			Trajectory = new Trajectory(Points, Controller.GetNames(), transform.position, TargetDirection);
			if(Controller.Styles.Length > 0) {
				for(int i=0; i<Trajectory.Points.Length; i++) {
					Trajectory.Points[i].Styles[0] = 1f;
				}
			}
			for(int i=0; i<Actor.Bones.Length; i++) {
				Positions[i] = Actor.Bones[i].Transform.position;
				Forwards[i] = Actor.Bones[i].Transform.forward;
				Ups[i] = Actor.Bones[i].Transform.up;
				Velocities[i] = Vector3.zero;
			}
		}

		void Update() {
			if(NN.Parameters == null) {
				return;
			}

			if(TrajectoryControl) {
				PredictTrajectory();
			}

			if(NN.Parameters != null) {
				Animate();
			}

			if(MotionEditing != null) {
				MotionEditing.Process();
				for(int i=0; i<Actor.Bones.Length; i++) {
					Vector3 position = Actor.Bones[i].Transform.position;
					position.y = Positions[i].y;
					Positions[i] = Vector3.Lerp(Positions[i], position, MotionEditing.GetStability());
				}
			}
			
		}
       
        private void PredictTrajectory() {
            //Query Control
            double distance;
            Vector3 targetpos = new Vector3(GameObject.FindGameObjectWithTag("adam").transform.position.x, 0, GameObject.FindGameObjectWithTag("adam").transform.position.z);
			GameObject[] blocks = GameObject.FindGameObjectsWithTag("obstacle");

			Vector3 ray = Trajectory.Points[RootPointIndex].GetDirection();
			Vector3 destination = Actor.Bones[4].Transform.position;

			float turn = Controller.QueryTurn();
			Vector3 move = Controller.QueryMove();
			float[] style = Controller.GetStyle();
            
            bool control = turn != 0f || move.magnitude != 0f || style[1] != 0f;
            style = new float[style.Length];
            control = true;

            if (GameObject.FindGameObjectWithTag("target"))
                targetpos = new Vector3(GameObject.FindGameObjectWithTag("target").transform.position.x , 0, GameObject.FindGameObjectWithTag("target").transform.position.z);                       
         
            turn = GoTo(targetpos.x, targetpos.z);
            distance = Math.Sqrt((targetpos.x - Trajectory.Points[RootPointIndex].GetPosition().x) * (targetpos.x - Trajectory.Points[RootPointIndex].GetPosition().x) + (targetpos.z - Trajectory.Points[RootPointIndex].GetPosition().z) * (targetpos.z - Trajectory.Points[RootPointIndex].GetPosition().z));

			RaycastHit hit;

			
			foreach (GameObject block in blocks)
			{
				if (Physics.SphereCast(destination, block.GetComponent<MeshRenderer>().bounds.extents.magnitude * 1.2f, ray, out hit, 3))
				{
					if (hit.transform == block.transform)
					{
						turn = 1;
						//Debug.Log("BLOCK");
					}

				}
			}
            

			if (distance < 150f)
            {
                style[0] = 1f;
                turn = 0;
                move = Vector3.zero;
            }
            else
            {
                move = Vector3.forward;
                style[1] = 1f;
                style[5] = 0f;
            }

            //Process Control

            float curvature = Trajectory.GetCurvature(0, 111, 10);
			float target = PoolBias();
            //Epeidi den ruthmizw tous multipliers opote to target den pairnei timi apo to PoolBias(), to rythmizw analogo ths apostasis apo to stoxo
            target = Mathf.Clamp((float)distance, 0.8f, 4.0f);
            float current = Trajectory.Points[RootPointIndex].GetVelocity().magnitude;
			float bias = target;

            if (turn == 0f) 
                bias += PID.Update(Utility.Interpolate(target, current, curvature), current, 1f / Framerate);
            else          
                PID.Reset();
            move = bias * move.normalized;
            
            if (move.magnitude == 0f && turn != 0f) {
				move = 2f/3f*Vector3.forward;
			} else {
				if(move.z == 0f && turn != 0f && !Input.GetKey(Controller.Styles[1].Multipliers[1].Key)) {
					move = bias * new Vector3(move.x, 0f, 1f).normalized;
				} else {
					move = Vector3.Lerp(move, bias*Vector3.forward, Trajectory.Points[RootPointIndex].GetVelocity().magnitude / 6f);
				}
			}
			if(style[2] == 0f) {
				style[1] = Mathf.Max(style[1], Mathf.Pow(Mathf.Clamp(Trajectory.Points[RootPointIndex].GetVelocity().magnitude, 0f, 1f), 2f));
				if(style[1] > 0f) {
					move.z = Mathf.Max(move.z, 0.1f * style[1]);
				}
			} else {
				move.z = bias;
				move.x = 0f;
				turn = 0f;
				if(curvature > 0.25f) {
					style[0] = 0f;
					style[1] = 1f;
					style[2] = 0f;
					style[3] = 0f;
					style[4] = 0f;
					style[5] = 0f;
				} else {
					style[0] = 0f;
					style[1] = Trajectory.Points[RootPointIndex].GetVelocity().magnitude < 0.5f ? 1f : 0f;
					style[2] = Trajectory.Points[RootPointIndex].GetVelocity().magnitude < 0.5f ? 0f : 1f;
					style[3] = 0f;
					style[4] = 0f;
					style[5] = 0f;
				}
			}
			if(style[3] > 0f || style[4] > 0f || style[5] > 0f) {
				bias = 0f;
				if(Trajectory.Points[RootPointIndex].GetVelocity().magnitude > 0.5f) {
					style[3] = 0f;
					style[4] = 0f;
					style[5] = 0f;
				}
			}

			//Update Target Direction / Velocity / Correction
			TargetDirection = Vector3.Lerp(TargetDirection, Quaternion.AngleAxis(turn * 60f, Vector3.up) * Trajectory.Points[RootPointIndex].GetDirection(), control ? TargetGain : TargetDecay);
			TargetVelocity = Vector3.Lerp(TargetVelocity, Quaternion.LookRotation(TargetDirection, Vector3.up) * move, control ? TargetGain : TargetDecay);
			TrajectoryCorrection = Utility.Interpolate(TrajectoryCorrection, Mathf.Max(move.normalized.magnitude, Mathf.Abs(turn)), control ? TargetGain : TargetDecay);

			//Predict Future Trajectory
			Vector3[] trajectory_positions_blend = new Vector3[Trajectory.Points.Length];
			trajectory_positions_blend[RootPointIndex] = Trajectory.Points[RootPointIndex].GetTransformation().GetPosition();
			for(int i=RootPointIndex+1; i<Trajectory.Points.Length; i++) {
				float bias_pos = 0.75f;
				float bias_dir = 1.25f;
				float bias_vel = 1.0f;
				float weight = (float)(i - RootPointIndex) / (float)FuturePoints; //w between 1/FuturePoints and 1
				float scale_pos = 1f - Mathf.Pow(1f - weight, bias_pos);
				float scale_dir = 1f - Mathf.Pow(1f - weight, bias_dir);
				float scale_vel = 1f - Mathf.Pow(1f - weight, bias_vel);
				float scale = 1f / (Trajectory.Points.Length - (RootPointIndex + 1f));
				trajectory_positions_blend[i] = trajectory_positions_blend[i-1] + Vector3.Lerp(Trajectory.Points[i].GetPosition() - Trajectory.Points[i-1].GetPosition(), scale * TargetVelocity, scale_pos);
				Trajectory.Points[i].SetDirection(Vector3.Lerp(Trajectory.Points[i].GetDirection(), TargetDirection, scale_dir));
				Trajectory.Points[i].SetVelocity(Vector3.Lerp(Trajectory.Points[i].GetVelocity(), TargetVelocity, scale_vel));
			}
			for(int i=RootPointIndex+1; i<Trajectory.Points.Length; i++) {
				Trajectory.Points[i].SetPosition(trajectory_positions_blend[i]);
			}
			for(int i=RootPointIndex; i<Trajectory.Points.Length; i++) {
				float weight = (float)(i - RootPointIndex) / (float)FuturePoints; //w between 0 and 1
				for(int j=0; j<Trajectory.Points[i].Styles.Length; j++) {
					Trajectory.Points[i].Styles[j] = Utility.Interpolate(Trajectory.Points[i].Styles[j], style[j], Utility.Normalise(weight, 0f, 1f, Controller.Styles[j].Transition, 1f));
				}
				Utility.Normalise(ref Trajectory.Points[i].Styles);
				Trajectory.Points[i].SetSpeed(Utility.Interpolate(Trajectory.Points[i].GetSpeed(), TargetVelocity.magnitude, control ? TargetGain : TargetDecay));
			}
		}

		private void Animate() {
			//Calculate Root
			Matrix4x4 currentRoot = Trajectory.Points[RootPointIndex].GetTransformation();
			currentRoot[1,3] = 0f; //For flat terrain

			int start = 0;
			//Input Trajectory Positions / Directions / Velocities / Styles
			for(int i=0; i<PointSamples; i++) {
				Vector3 pos = GetSample(i).GetPosition().GetRelativePositionTo(currentRoot);
				Vector3 dir = GetSample(i).GetDirection().GetRelativeDirectionTo(currentRoot);
				Vector3 vel = GetSample(i).GetVelocity().GetRelativeDirectionTo(currentRoot);
				float speed = GetSample(i).GetSpeed();
				NN.SetInput(start + i*TrajectoryDimIn + 0, pos.x);
				NN.SetInput(start + i*TrajectoryDimIn + 1, pos.z);
				NN.SetInput(start + i*TrajectoryDimIn + 2, dir.x);
				NN.SetInput(start + i*TrajectoryDimIn + 3, dir.z);
				NN.SetInput(start + i*TrajectoryDimIn + 4, vel.x);
				NN.SetInput(start + i*TrajectoryDimIn + 5, vel.z);
				NN.SetInput(start + i*TrajectoryDimIn + 6, speed);
				for(int j=0; j<Controller.Styles.Length; j++) {
					NN.SetInput(start + i*TrajectoryDimIn + (TrajectoryDimIn - Controller.Styles.Length) + j, GetSample(i).Styles[j]);
				}
			}
			start += TrajectoryDimIn*PointSamples;

			Matrix4x4 previousRoot = Trajectory.Points[RootPointIndex-1].GetTransformation();
			previousRoot[1,3] = 0f; //For flat terrain

			//Input Previous Bone Positions / Velocities
			for(int i=0; i<Actor.Bones.Length; i++) {
				Vector3 pos = Positions[i].GetRelativePositionTo(previousRoot);
				Vector3 forward = Forwards[i].GetRelativeDirectionTo(previousRoot);
				Vector3 up = Ups[i].GetRelativeDirectionTo(previousRoot);
				Vector3 vel = Velocities[i].GetRelativeDirectionTo(previousRoot);
				NN.SetInput(start + i*JointDimIn + 0, pos.x);
				NN.SetInput(start + i*JointDimIn + 1, pos.y);
				NN.SetInput(start + i*JointDimIn + 2, pos.z);
				NN.SetInput(start + i*JointDimIn + 3, forward.x);
				NN.SetInput(start + i*JointDimIn + 4, forward.y);
				NN.SetInput(start + i*JointDimIn + 5, forward.z);
				NN.SetInput(start + i*JointDimIn + 6, up.x);
				NN.SetInput(start + i*JointDimIn + 7, up.y);
				NN.SetInput(start + i*JointDimIn + 8, up.z);
				NN.SetInput(start + i*JointDimIn + 9, vel.x);
				NN.SetInput(start + i*JointDimIn + 10, vel.y);
				NN.SetInput(start + i*JointDimIn + 11, vel.z);
			}
			start += JointDimIn*Actor.Bones.Length;

			//Predict
			System.DateTime timestamp = Utility.GetTimestamp();
			NN.Predict();
			NetworkPredictionTime = (float)Utility.GetElapsedTime(timestamp);

			//Update Past Trajectory
			for(int i=0; i<RootPointIndex; i++) {
				Trajectory.Points[i].SetPosition(Trajectory.Points[i+1].GetPosition());
				Trajectory.Points[i].SetDirection(Trajectory.Points[i+1].GetDirection());
				Trajectory.Points[i].SetVelocity(Trajectory.Points[i+1].GetVelocity());
				Trajectory.Points[i].SetSpeed(Trajectory.Points[i+1].GetSpeed());
				for(int j=0; j<Trajectory.Points[i].Styles.Length; j++) {
					Trajectory.Points[i].Styles[j] = Trajectory.Points[i+1].Styles[j];
				}
			}

			//Update Root
			float update = Mathf.Min(
				Mathf.Pow(1f - (Trajectory.Points[RootPointIndex].Styles[0]), 0.25f),
				Mathf.Pow(1f - (Trajectory.Points[RootPointIndex].Styles[3] 
								+ Trajectory.Points[RootPointIndex].Styles[4] 
								+ Trajectory.Points[RootPointIndex].Styles[5]
							), 	0.5f)
			);
			Vector3 rootMotion = update * new Vector3(NN.GetOutput(TrajectoryDimOut*6 + JointDimOut*Actor.Bones.Length + 0), NN.GetOutput(TrajectoryDimOut*6 + JointDimOut*Actor.Bones.Length + 1), NN.GetOutput(TrajectoryDimOut*6 + JointDimOut*Actor.Bones.Length + 2));
			rootMotion /= Framerate;
			Vector3 translation = new Vector3(rootMotion.x, 0f, rootMotion.z);
			float angle = rootMotion.y;

			Trajectory.Points[RootPointIndex].SetPosition(translation.GetRelativePositionFrom(currentRoot));
			Trajectory.Points[RootPointIndex].SetDirection(Quaternion.AngleAxis(angle, Vector3.up) * Trajectory.Points[RootPointIndex].GetDirection());
			Trajectory.Points[RootPointIndex].SetVelocity(translation.GetRelativeDirectionFrom(currentRoot) * Framerate);
			Matrix4x4 nextRoot = Trajectory.Points[RootPointIndex].GetTransformation();
			nextRoot[1,3] = 0f; //For flat terrain

            //Update Future Trajectory
        
			for(int i=RootPointIndex+1; i<Trajectory.Points.Length; i++) {
              
                Trajectory.Points[i].SetPosition(Trajectory.Points[i].GetPosition() + translation.GetRelativeDirectionFrom(nextRoot));
                Trajectory.Points[i].SetDirection(Quaternion.AngleAxis(angle, Vector3.up) * Trajectory.Points[i].GetDirection());
                Trajectory.Points[i].SetVelocity(Trajectory.Points[i].GetVelocity() + translation.GetRelativeDirectionFrom(nextRoot) * Framerate);
    
            }
			start = 0;
			for(int i=RootPointIndex+1; i<Trajectory.Points.Length; i++) {
				//ROOT	1		2		3		4		5
				//.x....x.......x.......x.......x.......x
				int index = i;
				int prevSampleIndex = GetPreviousSample(index).GetIndex() / PointDensity;
				int nextSampleIndex = GetNextSample(index).GetIndex() / PointDensity;
				float factor = (float)(i % PointDensity) / PointDensity;

				Vector3 prevPos = new Vector3(
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 0),
					0f,
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 1)
				).GetRelativePositionFrom(nextRoot);
				Vector3 prevDir = new Vector3(
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 2),
					0f,
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 3)
				).normalized.GetRelativeDirectionFrom(nextRoot);
				Vector3 prevVel = new Vector3(
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 4),
					0f,
					NN.GetOutput(start + (prevSampleIndex-6)*TrajectoryDimOut + 5)
				).GetRelativeDirectionFrom(nextRoot);

				Vector3 nextPos = new Vector3(
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 0),
					0f,
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 1)
				).GetRelativePositionFrom(nextRoot);
				Vector3 nextDir = new Vector3(
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 2),
					0f,
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 3)
				).normalized.GetRelativeDirectionFrom(nextRoot);
				Vector3 nextVel = new Vector3(
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 4),
					0f,
					NN.GetOutput(start + (nextSampleIndex-6)*TrajectoryDimOut + 5)
				).GetRelativeDirectionFrom(nextRoot);

                Vector3 pos = (1f - factor) * prevPos + factor * nextPos;
				Vector3 dir = ((1f - factor) * prevDir + factor * nextDir).normalized;
				Vector3 vel = (1f - factor) * prevVel + factor * nextVel;

				pos = Vector3.Lerp(Trajectory.Points[i].GetPosition() + vel / Framerate, pos, 0.5f);

				Trajectory.Points[i].SetPosition(
					Utility.Interpolate(
						Trajectory.Points[i].GetPosition(),
						pos,
						TrajectoryCorrection
						)
					);
				Trajectory.Points[i].SetDirection(
					Utility.Interpolate(
						Trajectory.Points[i].GetDirection(),
						dir,
						TrajectoryCorrection
						)
					);
				Trajectory.Points[i].SetVelocity(
					Utility.Interpolate(
						Trajectory.Points[i].GetVelocity(),
						vel,
						TrajectoryCorrection
						)
					);
			}
			start += TrajectoryDimOut*6;

			//Compute Posture
			for(int i=0; i<Actor.Bones.Length; i++) {
				Vector3 position = new Vector3(NN.GetOutput(start + i*JointDimOut + 0), NN.GetOutput(start + i*JointDimOut + 1), NN.GetOutput(start + i*JointDimOut + 2)).GetRelativePositionFrom(currentRoot);
				Vector3 forward = new Vector3(NN.GetOutput(start + i*JointDimOut + 3), NN.GetOutput(start + i*JointDimOut + 4), NN.GetOutput(start + i*JointDimOut + 5)).normalized.GetRelativeDirectionFrom(currentRoot);
				Vector3 up = new Vector3(NN.GetOutput(start + i*JointDimOut + 6), NN.GetOutput(start + i*JointDimOut + 7), NN.GetOutput(start + i*JointDimOut + 8)).normalized.GetRelativeDirectionFrom(currentRoot);
				Vector3 velocity = new Vector3(NN.GetOutput(start + i*JointDimOut + 9), NN.GetOutput(start + i*JointDimOut + 10), NN.GetOutput(start + i*JointDimOut + 11)).GetRelativeDirectionFrom(currentRoot);
				
				Positions[i] = Vector3.Lerp(Positions[i] + velocity / Framerate, position, 0.5f);
				Forwards[i] = forward;
				Ups[i] = up;
				Velocities[i] = velocity;
			}
			start += JointDimOut*Actor.Bones.Length;

			//Assign Posture
			Vector3 targetpos = new Vector3(GameObject.FindGameObjectWithTag("adam").transform.position.x, 0, GameObject.FindGameObjectWithTag("adam").transform.position.z);
			if (GameObject.FindGameObjectWithTag("target"))
				targetpos = new Vector3(GameObject.FindGameObjectWithTag("target").transform.position.x, GameObject.FindGameObjectWithTag("target").transform.position.y, GameObject.FindGameObjectWithTag("target").transform.position.z);

            
            //if (flag)
            {
                transform.position = nextRoot.GetPosition();
                transform.rotation = nextRoot.GetRotation();
           
                for (int i = 0; i < Actor.Bones.Length; i++)
                {
                    if (i == 4 && !collisionFlag)
                    {     

                        continue;
                    }
                       
                    Actor.Bones[i].Transform.position = Positions[i];
                    Actor.Bones[i].Transform.rotation = Quaternion.LookRotation(Forwards[i], Ups[i]);

                }

                //Vector3 home = Actor.Bones[5].Transform.position - Actor.Bones[4].Transform.position;
                //Vector3 destination = Vector3.Slerp(home, targetpos - Actor.Bones[4].Transform.position, 0.8f);

                //Vector3 homeX = Vector3.ProjectOnPlane(home, Vector3.up);
                //Vector3 destinationX = Vector3.ProjectOnPlane(destination, Vector3.up);

                //Vector3 homeY = Vector3.ProjectOnPlane(home, Vector3.right);
                //Vector3 destinationY = Vector3.ProjectOnPlane(destination, Vector3.right);

                //float targetAngleX = Mathf.Clamp(Vector3.SignedAngle(homeX, destinationX, Vector3.up), -10, 160);
                //float targetAngleY = (targetpos.y > Actor.Bones[4].Transform.position.y) ? Mathf.Clamp(Vector3.Angle(homeY, destinationY), 0, 75) : -Mathf.Clamp(Vector3.Angle(homeY, destinationY), 0, 10);




                ////Actor.Bones[4].Transform.rotation = Quaternion.Slerp(Actor.Bones[4].Transform.rotation, Quaternion.Euler(new Vector3(0, targetAngleX, targetAngleY)), 1);

                ////Actor.Bones[4].Transform.rotation = Quaternion.FromToRotation(home, -Vector3.ClampMagnitude(destination, home.magnitude));

                //Actor.Bones[4].Transform.Rotate(new Vector3(0, targetAngleX, targetAngleY));
            }
            if (Time.time - colTime > 0.3f)
                collisionFlag = true;
     
				
		}

        private void OnCollisionEnter(Collision collision)
        {
          
          
        }




        private float PoolBias() {
			float[] styles = Trajectory.Points[RootPointIndex].Styles;
			float bias = 0f;
			for(int i=0; i<styles.Length; i++) {
				float _bias = Controller.Styles[i].Bias;
				float max = 0f;
				for(int j=0; j<Controller.Styles[i].Multipliers.Length; j++) {
					if(Controller.Styles[i].Query() && Input.GetKey(Controller.Styles[i].Multipliers[j].Key)) {
						max = Mathf.Max(max, Controller.Styles[i].Bias * Controller.Styles[i].Multipliers[j].Value);
					}
				}
				for(int j=0; j<Controller.Styles[i].Multipliers.Length; j++) {
					if(Controller.Styles[i].Query() && Input.GetKey(Controller.Styles[i].Multipliers[j].Key)) {
						_bias = Mathf.Min(max, _bias * Controller.Styles[i].Multipliers[j].Value);
					}
				}
				bias += styles[i] * _bias;
			}
			return bias;
		}


        public float GoTo(float targetx, float targetz)
        {
            float turn;
            Vector3 destination = new Vector3(targetx - Trajectory.Points[RootPointIndex].GetPosition().x, 0, targetz - Trajectory.Points[RootPointIndex].GetPosition().z); 
            float targetAngle = Vector3.SignedAngle(destination, Trajectory.Points[RootPointIndex].GetDirection(), Vector3.up);
            if (targetAngle > 10)
                turn = -1;
            else if (targetAngle < -10)
                turn = 1;
            else
                turn = 0;

            //Trajectory.Points[RootPointIndex].SetDirection(Vector3.Lerp(Trajectory.Points[RootPointIndex].GetDirection(), destination, (Time.time - Test.stime) / (length * 60)));
            return turn;

        }

        private Trajectory.Point GetSample(int index) {
			return Trajectory.Points[Mathf.Clamp(index*10, 0, Trajectory.Points.Length-1)];
		}

		private Trajectory.Point GetPreviousSample(int index) {
			return GetSample(index / 10);
		}

		private Trajectory.Point GetNextSample(int index) {
			if(index % 10 == 0) {
				return GetSample(index / 10);
			} else {
				return GetSample(index / 10 + 1);
			}
		}

		void OnGUI() {
			if(NN.Parameters == null) {
				return;
			}

			UltiDraw.DrawGUITexture(new Vector2(0.5f, 0.05f), 0.03f, Disc, Input.GetKey(Controller.Forward) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.5f, 0.05f), 0.03f, Forward, UltiDraw.White);

			UltiDraw.DrawGUITexture(new Vector2(0.465f, 0.05f), 0.03f, Disc, Input.GetKey(Controller.TurnLeft) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.465f, 0.05f), 0.03f, TurnLeft, UltiDraw.White);

			UltiDraw.DrawGUITexture(new Vector2(0.535f, 0.05f), 0.03f, Disc, Input.GetKey(Controller.TurnRight) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.535f, 0.05f), 0.03f, TurnRight, UltiDraw.White);

			UltiDraw.DrawGUITexture(new Vector2(0.5f, 0.11f), 0.03f, Disc, Input.GetKey(Controller.Back) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.5f, 0.11f), 0.03f, Back, UltiDraw.White);

			UltiDraw.DrawGUITexture(new Vector2(0.465f, 0.11f), 0.03f, Disc, Input.GetKey(Controller.Left) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.465f, 0.11f), 0.03f, Left, UltiDraw.White);

			UltiDraw.DrawGUITexture(new Vector2(0.535f, 0.11f), 0.03f, Disc, Input.GetKey(Controller.Right) ? UltiDraw.Orange : UltiDraw.BlackGrey);
			UltiDraw.DrawGUITexture(new Vector2(0.535f, 0.11f), 0.03f, Right, UltiDraw.White);

			FontStyle.fontSize = Mathf.RoundToInt(0.0125f * Screen.width);

			UltiDraw.DrawGUITexture(new Vector2(0.4f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[0]));
			UltiDraw.DrawGUITexture(new Vector2(0.4f, 0.18f), 0.025f, Idle, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.385f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[0]*100f) + "%", FontStyle);

			UltiDraw.DrawGUITexture(new Vector2(0.44f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[1]));
			UltiDraw.DrawGUITexture(new Vector2(0.44f, 0.18f), 0.02f, Move, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.425f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[1]*100f) + "%", FontStyle);

			UltiDraw.DrawGUITexture(new Vector2(0.48f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[2]));
			UltiDraw.DrawGUITexture(new Vector2(0.48f, 0.18f), 0.02f, Jump, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.465f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[2]*100f) + "%", FontStyle);

			UltiDraw.DrawGUITexture(new Vector2(0.52f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[3]));
			UltiDraw.DrawGUITexture(new Vector2(0.52f, 0.18f), 0.02f, Sit, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.505f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[3]*100f) + "%", FontStyle);

			UltiDraw.DrawGUITexture(new Vector2(0.56f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[4]));
			UltiDraw.DrawGUITexture(new Vector2(0.56f, 0.18f), 0.02f, Stand, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.545f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[4]*100f) + "%", FontStyle);

			UltiDraw.DrawGUITexture(new Vector2(0.6f, 0.18f), 0.03f, Disc, Color.Lerp(UltiDraw.BlackGrey, UltiDraw.Orange, Trajectory.Points[RootPointIndex].Styles[5]));
			UltiDraw.DrawGUITexture(new Vector2(0.6f, 0.18f), 0.02f, Lie, UltiDraw.White);
			GUI.Label(Utility.GetGUIRect(0.585f, 0.21f, 0.03f, 0.03f), Mathf.RoundToInt(Trajectory.Points[RootPointIndex].Styles[5]*100f) + "%", FontStyle);

			GUI.Label(Utility.GetGUIRect(0.5f - 0.25f/2f, 0.235f, 0.25f, 0.05f), "Velocity " + Trajectory.Points[RootPointIndex].GetVelocity().magnitude.ToString("F1") + "m/s", FontStyle);

			GUIStyle style = new GUIStyle();
			int size = Mathf.RoundToInt(0.01f*Screen.width);
			Rect rect = new Rect(10, Screen.height-10-size-size, Screen.width-2f*10, size);
			style.alignment = TextAnchor.MiddleRight;
			style.fontSize = size;
			style.normal.textColor = Color.black;
			float msec = NetworkPredictionTime * 1000.0f;
			float nn = 1.0f / NetworkPredictionTime;
			string text = string.Format("{0:0.0} ms for NN", msec, nn);
			GUI.Label(rect, text, style);
		}

		void OnRenderObject() {
			if(Application.isPlaying) {
				if(NN.Parameters == null) {
					return;
				}

				if(ShowTrajectory) {
					UltiDraw.Begin();
					UltiDraw.DrawLine(Trajectory.Points[RootPointIndex].GetPosition(), Trajectory.Points[RootPointIndex].GetPosition() + TargetDirection, 0.05f, 0f, UltiDraw.Red.Transparent(0.75f));
					UltiDraw.DrawLine(Trajectory.Points[RootPointIndex].GetPosition(), Trajectory.Points[RootPointIndex].GetPosition() + TargetVelocity, 0.05f, 0f, UltiDraw.Green.Transparent(0.75f));
					UltiDraw.End();
					Trajectory.Draw(10);
				}

				if(ShowVelocities) {
					UltiDraw.Begin();
					for(int i=0; i<Actor.Bones.Length; i++) {
						UltiDraw.DrawArrow(
							Actor.Bones[i].Transform.position,
							Actor.Bones[i].Transform.position + Velocities[i],
							0.75f,
							0.0075f,
							0.05f,
							UltiDraw.Purple.Transparent(0.5f)
						);
					}
					UltiDraw.End();
				}
				
				UltiDraw.Begin();
				UltiDraw.DrawGUIHorizontalBar(new Vector2(0.5f, 0.74f), new Vector2(0.25f, 0.025f), UltiDraw.DarkGrey.Transparent(0.5f), 0.0025f, UltiDraw.Black, Trajectory.Points[RootPointIndex].GetVelocity().magnitude / 4f, UltiDraw.DarkGreen);
				UltiDraw.End();
			}
		}


		
		void OnDrawGizmos() {
			if (!Application.isPlaying) {
				
				OnRenderObject();
			}
			
		}

		#if UNITY_EDITOR
		[CustomEditor(typeof(BioAnimation_Wolf))]
		public class BioAnimation_Wolf_Editor : Editor {

			public BioAnimation_Wolf Target;

			void Awake() {
				Target = (BioAnimation_Wolf)target;
			}

			public override void OnInspectorGUI() {
				Undo.RecordObject(Target, Target.name);

				Inspector();
				Target.Controller.Inspector();

				if(GUI.changed) {
					EditorUtility.SetDirty(Target);
				}
			}

			private void Inspector() {
				Utility.SetGUIColor(UltiDraw.Grey);
				using(new EditorGUILayout.VerticalScope ("Box")) {
					Utility.ResetGUIColor();

					if(Utility.GUIButton("Animation", UltiDraw.DarkGrey, UltiDraw.White)) {
						Target.Inspect = !Target.Inspect;
					}

					if(Target.Inspect) {
						using(new EditorGUILayout.VerticalScope ("Box")) {
							Target.ShowTrajectory = EditorGUILayout.Toggle("Show Trajectory", Target.ShowTrajectory);
							Target.ShowVelocities = EditorGUILayout.Toggle("Show Velocities", Target.ShowVelocities);
							Target.TargetGain = EditorGUILayout.Slider("Target Gain", Target.TargetGain, 0f, 1f);
							Target.TargetDecay = EditorGUILayout.Slider("Target Decay", Target.TargetDecay, 0f, 1f);
							Target.TrajectoryControl = EditorGUILayout.Toggle("Trajectory Control", Target.TrajectoryControl);
							Target.TrajectoryCorrection = EditorGUILayout.Slider("Trajectory Correction", Target.TrajectoryCorrection, 0f, 1f);
						}
					}
				}
			}
		}
		#endif
	}
}