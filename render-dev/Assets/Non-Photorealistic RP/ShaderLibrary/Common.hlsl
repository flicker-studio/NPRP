#ifndef NPRP_COMMON_HLSL
#define NPRP_COMMON_HLSL

#include "UnityInput.hlsl"

float3 transform_object_to_world(float3 position_os)
{
    return mul(unity_ObjectToWorld, float4(position_os, 1.0)).xyz;
}


float4 transform_world_to_h_clip(float3 position_ws)
{
    return mul(unity_MatrixVP, float4(position_ws, 1.0));
}
#endif