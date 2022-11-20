Shader "Showcase Room"
{
    Properties
    {
        _Color("color",color)=(1,1,1,1)
        _MainTex("Base", 2D) = "white" {}
        [Toggle]_Invert("Invert", Float) = 0
        [HideInInspector] _NormalTex("法线贴图", 2D) = "bump" {}
        [HideInInspector] _NormalScale("法线强度", Float) = 1.0
        [HideInInspector] __dirty( "", Int ) = 1
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
            float _NormalScale;
            CBUFFER_END
            float _Invert;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);


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
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 biTangentWS : TEXCOORD4;
            };


            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;


                const VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.position = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;

                const VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normalOS.xyz, v.tangentOS);

                o.normalWS = normalInputs.normalWS;
                o.tangentWS = normalInputs.tangentWS;
                o.biTangentWS = normalInputs.bitangentWS;
                o.uv = v.uv;

                return o;
            }

            float4 frag(VertexOutput i): SV_Target
            {
                //------法线贴图转世界法线--------
                //载入法线贴图
                float4 normalTXS = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv);
                //贴图颜色 0~1 转 -1~1并且缩放法线强度
                float3 normalTS = UnpackNormalScale(normalTXS, _NormalScale);
                //贴图法线转换为世界法线
                half3 normalWS = TransformTangentToWorld(normalTS,real3x3(i.tangentWS, i.biTangentWS, i.normalWS));

                //-----------阴影数据--------------
                //当前模型接收阴影
                float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.positionWS);
                //放入光照数据
                Light lightData = GetMainLight(SHADOW_COORDS);

                //阴影数据
                half shadow = lightData.shadowAttenuation;
                //光照渐变
                float Ramp_light = saturate(dot(lightData.direction, normalWS));

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                baseTex.rgb = (_Invert ? 1.0 - baseTex : baseTex).rgb;

                _Color.rgb *= Ramp_light * lightData.color.rgb * shadow;
                _Color.rgb += _GlossyEnvironmentColor.rgb;
                _Color.rgb *= baseTex.rgb;
                _Color.a += baseTex.a;
                clip(_Color.a - 0.5);
                return _Color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}