using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10,100)] int resolution;

    [SerializeField] FunctionLibrary.FunctionName function;

    Transform[] points;
    void Awake () 
    {
        // instantiates points at certain resolution
        float step = 2f / resolution;
        Vector3 scale = Vector3.one * step;
        points = new Transform[resolution * resolution];

		for (int i = 0; i < points.Length; i++) 
        {
			Transform point = points[i] = Instantiate(pointPrefab);
            point.localScale = scale;
            point.SetParent(transform, false);
		}
	}

    void Update () 
    {
        // initializes points in a grid pattern, spaced out with step 
        // updates those points 
		FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
		float time = Time.time;
		float step = 2f / resolution;
		float v = 0.5f * step - 1f;
		for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++) 
        {
			if (x == resolution) 
            {
				x = 0;
				z += 1;
				v = (z + 0.5f) * step - 1f;// the 0.5 shifts each point to be located in the center of a grid cell
			}
			float u = (x + 0.5f) * step - 1f;
			points[i].localPosition = f(u, v, time);
	    }
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