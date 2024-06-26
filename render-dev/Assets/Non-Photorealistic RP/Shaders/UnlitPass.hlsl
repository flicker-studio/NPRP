#ifndef NPRP_UNLIT_PASS_HLSL
#define NPRP_UNLIT_PASS_HLSL

#include "../ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
CBUFFER_END

float4 unlit_pass_vertex(float3 position_os : POSITION) : SV_POSITION
{
    const float3 position_ws = TransformObjectToWorld(position_os.xyz);
    return TransformWorldToHClip(position_ws);
}

float4 unlit_pass_fragment() : SV_TARGET
{
    return _BaseColor;
}

#endif