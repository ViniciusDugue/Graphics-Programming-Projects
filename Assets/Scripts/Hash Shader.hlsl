#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<uint> _Hashes;
	StructuredBuffer<float3> _Positions, _Normals;
#endif

float4 _Config; // holds information like (resolution, reverse resolution,displacement) for calculations

void ConfigureProcedural () {
	// set position of instance based on unique id
	// config xyz is resolutoin, inverse resolution, and displacement
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(
			_Positions[unity_InstanceID],
			1.0
		);
		unity_ObjectToWorld._m03_m13_m23 +=
			(_Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5)) *
			_Normals[unity_InstanceID];
		unity_ObjectToWorld._m00_m11_m22 = _Config.y;
	#endif
}
// takes hash value based on object ids hash value in hash buffer and returns an rgb color 
//based off the hash value
float3 GetHashColor () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		uint hash = _Hashes[unity_InstanceID];
		return (1.0 / 255.0) * float3(
			hash & 255,
			(hash >> 8) & 255,
			(hash >> 16) & 255
		);
	#else
		return 1.0;
	#endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
	Out = In;
	Color = GetHashColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
	Out = In;
	Color = GetHashColor();
}