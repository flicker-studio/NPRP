Shader "Non-Photorealistic RP/Unlit"
{

    Properties {}

    SubShader
    {

        Pass
        {
            HLSLPROGRAM
            #include "UnlitPass.hlsl"
            #pragma vertex unlit_pass_vertex
            #pragma fragment unlit_pass_fragment
            ENDHLSL

        }
    }
}