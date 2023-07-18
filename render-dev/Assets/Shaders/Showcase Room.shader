Shader "Showcase Room"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        [Toggle]_Invert("Invert", Float) = 0
        [HideInInspector] _Color("Default color", color) = (1,1,1,1)
      
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "LightMode" = "UniversalForward"
        }
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile  _MAIN_LIGHT_SHADOWS
            #pragma multi_compile  _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile  _SHADOWS_SOFT

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            float _Invert;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };


            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };


            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;


                const VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.position = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;
                o.uv = v.uv;
                return o;
            }

            float4 frag(VertexOutput i): SV_Target
            {
                const float4 shadow_coords = TransformWorldToShadowCoord(i.positionWS);
                Light light_data = GetMainLight(shadow_coords);
                const half shadow = light_data.shadowAttenuation;

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                baseTex.rgb = (_Invert ? 1.0 - baseTex : baseTex).rgb;
                _Color.rgb *= baseTex.rgb * light_data.color.rgb * shadow;
                return _Color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}