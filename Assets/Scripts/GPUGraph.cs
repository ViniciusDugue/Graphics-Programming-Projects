using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000;
    [SerializeField, Range(10,maxResolution)] int resolution;

    [SerializeField] FunctionLibrary.FunctionName function;
    public enum TransitionMode { Cycle, Random }
	[SerializeField] TransitionMode transitionMode;
    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;

    float duration;
    bool transitioning;
	FunctionLibrary.FunctionName transitionFunction;

    ComputeBuffer positionsBuffer;
    [SerializeField] ComputeShader computeShader;

    static readonly int
    positionsId = Shader.PropertyToID("_Positions"),
    resolutionId = Shader.PropertyToID("_Resolution"),
    stepId = Shader.PropertyToID("_Step"),
    timeId = Shader.PropertyToID("_Time"),
	transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    [SerializeField] Material material;

	[SerializeField] Mesh mesh;
    // updates the resolution, step, time values of the compute shader
    void UpdateFunctionOnGPU () 
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if (transitioning) 
        {
			computeShader.SetFloat(transitionProgressId, Mathf.SmoothStep(0f, 1f, duration / transitionDuration));
		}
        var kernelIndex = (int)function + (int)(transitioning ? transitionFunction : function) * 8;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        material.SetBuffer(positionsId, positionsBuffer);
		material.SetFloat(stepId, step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }

	void OnEnable () {
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
	}

    void OnDisable () 
    {
		positionsBuffer.Release();
        positionsBuffer = null;
	}

    void Update () 
    {
        duration += Time.deltaTime;
		if (transitioning) 
        {
			if (duration >= transitionDuration) 
            {
				duration -= transitionDuration;
				transitioning = false;
			}
		}
		else if (duration >= functionDuration) 
        {
			duration -= functionDuration;
			transitioning = true;
			transitionFunction = function;
			PickNextFunction();
		}
        UpdateFunctionOnGPU();
    }

    void PickNextFunction () 
    {
		function = transitionMode == TransitionMode.Cycle ?
			FunctionLibrary.GetNextFunctionName(function) :
			FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}
}

// Static graph that produces cubic line without movement over time
// [SerializeField] Transform pointPrefab;
// [SerializeField, Range(10,100)] int resolution;
// void Awake () 
// {
//     float step  =2f / resolution;
//     Vector3 scale = Vector3.one * step;
//     Vector3 position = Vector3.zero;
//     for (int i = 0; i < resolution; i++) 
//     {
//         Transform point = Instantiate(pointPrefab);
//         position.x = (i+ 0.5f) * step -1f;
//         position.y = position.x * position.x * position.x;
//         point.localPosition = position;
//         point.localScale = scale;
//         point.SetParent(transform, false);
//     }
// }