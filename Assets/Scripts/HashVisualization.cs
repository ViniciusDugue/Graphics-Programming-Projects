using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour {

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct HashJob : IJobFor {

		[ReadOnly]
		public NativeArray<float3x4> positions;

		[WriteOnly]
		public NativeArray<uint4> hashes;

		public SmallXXHash4 hash;

		public float3x4 domainTRS;

		// takes a transformation matrix and multiplies it to a position matrix that contains 
		//4 xyz position vectors
		float4x3 TransformPositions (float3x4 trs, float4x3 p) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
		);

		// transforms position matrix and calculates integer vectors to generate hash value
		// position vector is transposed because vectorized operations on vectors are done by doing 
		// all x y then z calculations
		public void Execute(int i) {
			float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));

			int4 u = (int4)floor(p.c0);
			int4 v = (int4)floor(p.c1);
			int4 w = (int4)floor(p.c2);

			hashes[i] = hash.Eat(u).Eat(v).Eat(w);
		}
	}

	public enum Shape { Plane, Sphere, Torus }

	static Shapes.ScheduleDelegate[] shapeJobs = {
		Shapes.Job<Shapes.Plane>.ScheduleParallel,
		Shapes.Job<Shapes.Sphere>.ScheduleParallel,
		Shapes.Job<Shapes.Torus>.ScheduleParallel
	};

	static int
		hashesId = Shader.PropertyToID("_Hashes"),// stores hash values of each object
		positionsId = Shader.PropertyToID("_Positions"),// stores new positions of each object
		normalsId = Shader.PropertyToID("_Normals"),// stores surface normals of each object for displacement
		configId = Shader.PropertyToID("_Config");

	[SerializeField] Mesh instanceMesh;

	[SerializeField] Material material;

	[SerializeField] Shape shape;

	[SerializeField, Range(0.1f, 10f)] float instanceScale = 2f;

	[SerializeField, Range(1, 512)] int resolution = 16;

	[SerializeField, Range(-0.5f, 0.5f)] float displacement = 0.1f;

	[SerializeField] int seed;

	// domain is the actual surface of the shape we are creating
	// transformations done on the points of the surface is done by multiplying the domain matrix 
	[SerializeField] SpaceTRS domain = new SpaceTRS {scale = 8f};

	NativeArray<uint4> hashes;

	NativeArray<float3x4> positions, normals;

	ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer;

	// a material properties memory block for setting per instance material properties for all objects
	// the material property block passes compute buffers to shaders
	MaterialPropertyBlock propertyBlock;

	bool isDirty;

	Bounds bounds;

	void OnEnable () {
		isDirty = true;

		int length = resolution * resolution;
		length = length / 4 + (length & 1);
		hashes = new NativeArray<uint4>(length, Allocator.Persistent);
		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		normals = new NativeArray<float3x4>(length, Allocator.Persistent);
		hashesBuffer = new ComputeBuffer(length * 4, 4);
		positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
		normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

		propertyBlock ??= new MaterialPropertyBlock();
		propertyBlock.SetBuffer(hashesId, hashesBuffer);
		propertyBlock.SetBuffer(positionsId, positionsBuffer);
		propertyBlock.SetBuffer(normalsId, normalsBuffer);
		propertyBlock.SetVector(configId, new Vector4(
			resolution, instanceScale / resolution, displacement
		));
	}

	void OnDisable () {
		hashes.Dispose();
		positions.Dispose();
		normals.Dispose();
		hashesBuffer.Release();
		positionsBuffer.Release();
		normalsBuffer.Release();
		hashesBuffer = null;
		positionsBuffer = null;
		normalsBuffer = null;
	}

	void OnValidate () {
		if (hashesBuffer != null && enabled) {
			OnDisable();
			OnEnable();
		}
	}

	void Update () {
		// isDirty checks if the state of object has changed and if needs to updated
		// avoids unnecesary recalculateions and updates when no changes have occured
		if (isDirty || transform.hasChanged) {
			isDirty = false;
			transform.hasChanged = false;

			JobHandle handle = shapeJobs[(int)shape](
				positions, normals, resolution, transform.localToWorldMatrix, default
			);

			new HashJob {
				positions = positions,
				hashes = hashes,
				hash = SmallXXHash4.Seed(seed),
				domainTRS = domain.Matrix
			}.ScheduleParallel(hashes.Length, resolution, handle).Complete();

			// reinterprets value in hashes, positions to a new value to unvectorize it
			// takes positions float3x4 matrix and reinterprets it into 4 position vectors
			// of 4 byte size
			hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
			positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
			normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

			// sets a bounding box for all game objects. bound is updated with displacement
			bounds = new Bounds(
				transform.position,
				float3(2f * cmax(abs(transform.lossyScale)) + displacement)
			);
		}

		Graphics.DrawMeshInstancedProcedural(
			instanceMesh, 0, material, bounds, resolution * resolution, propertyBlock
		);
	}
}

// takes in index of a point, calculates uv coordinates and uses uvs to calculate hash
// value and stores hash value in native array