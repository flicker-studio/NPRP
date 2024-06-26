#ifndef NPRP_UNLIT_PASS_HLSL
#define NPRP_UNLIT_PASS_HLSL

#include "../ShaderLibrary/Common.hlsl"

float4 unlit_pass_vertex(float3 position_os : POSITION) : SV_POSITION
{
    const float3 position_ws = transform_object_to_world(position_os.xyz);
    return transform_world_to_h_clip(position_ws);
}

float4 unlit_pass_fragment() : SV_TARGET
{
    return 0.0;
}

#endif