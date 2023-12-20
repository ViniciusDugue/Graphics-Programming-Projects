using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10,100)] int resolution;
    Transform[] points;
    void Awake () 
    {
        float step  =2f / resolution;
        Vector3 scale = Vector3.one * step;
        Vector3 position = Vector3.zero;
        points = new Transform[resolution];

		for (int i = 0; i < points.Length; i++) 
        {
			Transform point = points[i] = Instantiate(pointPrefab);
            position.x = (i+ 0.5f) * step -1f;
			point.localPosition = position;
            point.localScale = scale;
            point.SetParent(transform, false);
		}
	}

    void Update () 
    {
        float time = Time.time;
		for (int i = 0; i < points.Length; i++) 
        {
            Transform point = points[i];
			Vector3 position = point.localPosition;
			position.y = Mathf.Sin(Mathf.PI * (position.x + time));
            point.localPosition = position;
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