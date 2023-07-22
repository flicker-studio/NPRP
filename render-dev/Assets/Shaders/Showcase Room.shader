Shader "Showcase Room"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        _sharp("Sharp",Range(0.1,0.9)) = 0.5
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
            float1 _sharp;
            float1 _Invert;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            struct VertexInput
            {
                float4 position_os : POSITION;
                float2 uv : TEXCOORD0;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };


            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 position_ws : TEXCOORD1;
            };


            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;

                const VertexPositionInputs position_inputs = GetVertexPositionInputs(v.position_os.xyz);
                o.position = position_inputs.positionCS;
                o.position_ws = position_inputs.positionWS;
                o.uv = v.uv;
                return o;
            }

            float4 frag(VertexOutput i): SV_Target
            {
                const float4 shadow_coords = TransformWorldToShadowCoord(i.position_ws);
                Light light_data = GetMainLight(shadow_coords);
                const half shadow = light_data.shadowAttenuation;

                float4 base_tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                base_tex.rgb = (_Invert ? 1.0 - base_tex : base_tex).rgb;
                base_tex.rgb = (base_tex.rgb > _sharp ? 1.0 : 0).rgb;
                _Color.rgb *= base_tex.rgb * light_data.color.rgb * shadow;
                return _Color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}