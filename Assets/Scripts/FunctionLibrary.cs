using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary 
{
    public delegate Vector3 Function (float u, float v, float t);

    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, RotatingRidgedSphere, Torus, RotatingRidgedTorus };
    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, RotatingRidgedSphere, Torus, RotatingRidgedTorus };

    public static Function GetFunction (FunctionName name) 
    {
		return functions[(int)name];
	}

    public static Vector3 Wave (float u, float v, float t) //f(x,t) = sin(p * i(x+t))
    {
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (u + v + t));
		p.z = v;
		return p;
	}

    // adds 2 sin waves
    public static Vector3 MultiWave (float u, float v, float t) // f(x,t) = sin(pi(x+(0.5 *t)) + 0.5sin(2pi(x+t)) 
    {
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (u + 0.5f * t));
		p.y += 0.5f * Sin(2f * PI * (v + t));
		p.y += Sin(PI * (u + v + 0.25f * t));
		p.y *= 1f / 2.5f;
		p.z = v;
		return p;
	}

    public static Vector3 Ripple (float u, float v, float t) //d = |x|, f(d) = sin(4pi*d)/(1 + 10d)
    {
		float d = Sqrt(u * u + v * v);
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (4f * d - t));
		p.y /= 1f + 10f * d;
		p.z = v;
		return p;
	}

    //essentially u(azimuthal angle ) and v(polar angle) are taken and an xyz vector is outputted
    //basically calculating spherical coordinates with u/v and converting to cartesian coordinates xyz
    //advantage of spherical coordinates is its good at parameterizing 3d surfaces
    public static Vector3 Sphere (float u, float v, float t) 
    {
		float r = 1f;
		float s = r * Cos(0.5f * PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r * Sin(0.5f * PI * v);
		p.z = s * Cos(PI * u);
		return p;
	}

    public static Vector3 RotatingRidgedSphere (float u, float v, float t) 
    {
		float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
		float s = r * Cos(0.5f * PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r * Sin(0.5f * PI * v);
		p.z = s * Cos(PI * u);
		return p;
	}
    
    public static Vector3 Torus (float u, float v, float t) 
    {
		float r1 = 0.75f;// radius from the origin to the center of the tunnel
		float r2 = 0.25f;// radius from the center of the tunnel to the outside of tunnel 
		float s = r1 + r2 * Cos(PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r2 * Sin(PI * v);
		p.z = s * Cos(PI * u);
		return p;
	}

    public static Vector3 RotatingRidgedTorus (float u, float v, float t) 
    {
		float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));// radius from the origin to the center of the tunnel
		float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));// radius from the center of the tunnel to the outside of tunnel 
		float s = r1 + r2 * Cos(PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r2 * Sin(PI * v);
		p.z = s * Cos(PI * u);
		return p;
	}

}
