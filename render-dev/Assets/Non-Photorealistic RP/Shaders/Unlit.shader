Shader "Non-Photorealistic RP/Unlit"
{

    Properties
    {
        _BaseColor("Color", Color)= (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {

        Pass
        {
            HLSLPROGRAM
            #include "UnlitPass.hlsl"
            #pragma multi_compile_instancing
            #pragma vertex unlit_pass_vertex
            #pragma fragment unlit_pass_fragment
            ENDHLSL

        }
    }
}