#ifndef NPRP_UNLIT_PASS_HLSL
#define NPRP_UNLIT_PASS_HLSL

#include "../ShaderLibrary/Common.hlsl"

float4 _BaseColor;

float4 unlit_pass_vertex(float3 position_os : POSITION) : SV_POSITION
{
    const float3 position_ws = TransformObjectToWorld(position_os.xyz);
    return TransformObjectToHClip(position_ws);
}

float4 unlit_pass_fragment() : SV_TARGET
{
    return _BaseColor;
}

#endif