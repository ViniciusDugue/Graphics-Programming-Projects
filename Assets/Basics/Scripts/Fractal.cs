using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour 
{

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	// a job struct using unitys job system for multithreading with cpu for parallel computations
	// calculates rotation and position of fractal part based off just its parent
	struct UpdateFractalLevelJob : IJobFor 
    {

		public float spinAngleDelta;
		public float scale;

		[ReadOnly] public NativeArray<FractalPart> parents;

		public NativeArray<FractalPart> parts;

		[WriteOnly] public NativeArray<float3x4> matrices;

		public void Execute (int i) 
        {
			FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += spinAngleDelta;
			part.worldRotation = mul(parent.worldRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
			part.worldPosition =parent.worldPosition + mul(parent.worldRotation, 1.5f * scale * part.direction);
			parts[i] = part;
			float3x3 r = float3x3(part.worldRotation) * scale;
			matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
		}
	}

	struct FractalPart 
    {
		public float3 direction, worldPosition;
		public quaternion rotation, worldRotation;
		public float spinAngle;
	}

	static readonly int matricesId = Shader.PropertyToID("_Matrices");

	static float3[] directions = {up(), right(), left(), forward(), back()};

	static quaternion[] rotations = 
    {
		quaternion.identity,
		quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
		quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
	};

	static MaterialPropertyBlock propertyBlock;

	[SerializeField, Range(1, 8)] int depth = 4;

	[SerializeField] Mesh mesh;

	[SerializeField] Material material;

	NativeArray<FractalPart>[] parts;

	NativeArray<float3x4>[] matrices;

	ComputeBuffer[] matricesBuffers;

	// puts fractal parts into 2darray where each level has 5 times more fractals than the previous
	void OnEnable () 
    {
		parts = new NativeArray<FractalPart>[depth];
		matrices = new NativeArray<float3x4>[depth];
		matricesBuffers = new ComputeBuffer[depth];
		int stride = 12 * 4;
		for (int i = 0, length = 1; i < parts.Length; i++, length *= 5) 
        {
			parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
			matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
			matricesBuffers[i] = new ComputeBuffer(length, stride);
		}

		parts[0][0] = CreatePart(0);
		for (int li = 1; li < parts.Length; li++) 
        {
			NativeArray<FractalPart> levelParts = parts[li];
			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) 
            {
				for (int ci = 0; ci < 5; ci++) 
                {
					levelParts[fpi + ci] = CreatePart(ci);
				}
			}
		}
		propertyBlock ??= new MaterialPropertyBlock();
	}

	void OnDisable () {
		for (int i = 0; i < matricesBuffers.Length; i++) {
			matricesBuffers[i].Release();
			parts[i].Dispose();
			matrices[i].Dispose();
		}
		parts = null;
		matrices = null;
		matricesBuffers = null;
	}

	void OnValidate () 
    {
		if (parts != null && enabled) {
			OnDisable();
			OnEnable();
		}
	}

	FractalPart CreatePart (int childIndex) => new FractalPart 
    {
		direction = directions[childIndex],
		rotation = rotations[childIndex]
	};

	void Update () 
    {
        //calculate the new rotation and position of fractal part
		float spinAngleDelta = 0.125f * PI * Time.deltaTime;
		FractalPart rootPart = parts[0][0];
		rootPart.spinAngle += spinAngleDelta;
		rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
		rootPart.worldPosition = transform.position;
		parts[0][0] = rootPart;
		float objectScale = transform.lossyScale.x;
		float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
		matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

		float scale = objectScale;
        //iterates through levels of fractals and starts a new parallel job at each level to ???
		JobHandle jobHandle = default;
		for (int li = 1; li < parts.Length; li++) 
        {
			scale *= 0.5f;
			jobHandle = new UpdateFractalLevelJob 
            {
				spinAngleDelta = spinAngleDelta,
				scale = scale,
				parents = parts[li - 1],
				parts = parts[li],
				matrices = matrices[li]
			}.ScheduleParallel(parts[li].Length, 5, jobHandle);
		}
		jobHandle.Complete();

		var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        // updates buffer with matrices from cpu buffer and sends command to shader to draw the fractal parts
		for (int i = 0; i < matricesBuffers.Length; i++) 
        {
			ComputeBuffer buffer = matricesBuffers[i];
			buffer.SetData(matrices[i]);
			propertyBlock.SetBuffer(matricesId, buffer);
			Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count, propertyBlock);
		}
	}
}
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Fractal : MonoBehaviour 
// {

// 	[SerializeField, Range(1, 8)] int depth = 4;
//     [SerializeField] Mesh mesh;

// 	[SerializeField] Material material;
//     static Vector3[] directions = {
// 		Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
// 	};

// 	static Quaternion[] rotations = {
// 		Quaternion.identity,
// 		Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
// 		Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
// 	};

//     struct FractalPart 
//     {
// 		public Vector3 direction;
// 		public Quaternion rotation;
// 		public Transform transform;
// 	}

//     FractalPart[][] parts;
//     // puts fractal parts into 2darray where each level has 5 times more fractals than the previous
//     void Awake () {
// 		parts = new FractalPart[depth][];
//         for (int i = 0, length = 1; i < parts.Length; i++, length *= 5) {
// 			parts[i] = new FractalPart[length];
// 		}
// 		float scale = 1f;
// 		parts[0][0] = CreatePart(0, 0, scale);

//         for (int li = 1; li < parts.Length; li++) {
//             scale *= 0.5f;
// 			FractalPart[] levelParts = parts[li];
// 			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
// 				for (int ci = 0; ci < 5; ci++) {
// 					levelParts[fpi + ci] = CreatePart(li, ci, scale);
// 				}
// 			}
// 		}
// 	}

//     //creates a fractal part
//     FractalPart CreatePart (int levelIndex, int childIndex, float scale) {
// 		var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
//         go.transform.localScale = scale * Vector3.one;
//         go.transform.SetParent(transform, false);
//         go.AddComponent<MeshFilter>().mesh = mesh;
//         go.AddComponent<MeshRenderer>().material = material;
//         return new FractalPart {direction = directions[childIndex], rotation = rotations[childIndex], transform = go.transform};
//     }
    
//     //updates rotation and position of fractal parts
//     void Update () {
// 		for (int li = 1; li < parts.Length; li++) {
//             FractalPart[] parentParts = parts[li - 1];
// 			FractalPart[] levelParts = parts[li];
// 			for (int fpi = 0; fpi < levelParts.Length; fpi++) 
//             {
//                 Transform parentTransform = parentParts[fpi / 5].transform;
// 				FractalPart part = levelParts[fpi];
//                 part.transform.localPosition = parentTransform.localPosition + parentTransform.localRotation *
// 						(1.5f * part.transform.localScale.x * part.direction);
// 			}
// 		}
// 	}
// }
    
