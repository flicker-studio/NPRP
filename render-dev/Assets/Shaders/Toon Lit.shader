Shader "Toon Render/Toon_Lit"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _BaseColor("Base Color",color ) = (1,1,1,1)

        [Header(Shadow settings)][Space(10)]
        _ShadowColor ("Shadow Color", Color) = (0.7, 0.7, 0.8)
        _ShadowRange ("Shadow Range", Range(0, 1)) = 0.5
        _ShadowSmooth("Shadow Smooth", Range(0, 1)) = 0.1

        [Header(Outline settings)][Space(10)]
        _OutlineColor("Outline Color",color) = (0,0,0,1)
        _OutlineWidth("Outline Width",Range(0,1)) = .7
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Base"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _ShadowColor;
            float1 _ShadowRange;
            float1 _ShadowSmooth;
            float4 _OutlineColor;
            float1 _OutlineWidth;
            CBUFFER_END

            struct app_data
            {
                float4 object_position : POSITION;
                float4 object_normal : NORMAL;
                float4 object_tangent : TANGENT;
                float2 uv :TEXCOORD0;
            };

            struct vertex_data
            {
                float4 clip_position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world_position : TEXCOORD1;
                float3 world_normal:TEXCOORD2;
            };

            vertex_data vert(app_data app_input)
            {
                const VertexPositionInputs vertex_position = GetVertexPositionInputs(app_input.object_position.xyz);
                const VertexNormalInputs vertex_normal = GetVertexNormalInputs(app_input.object_normal.xyz);

                vertex_data output;
                output.clip_position = vertex_position.positionCS;
                output.world_position = vertex_position.positionWS;
                output.world_normal = normalize(vertex_normal.normalWS);
                output.uv = app_input.uv;
                return output;
            }


            float4 frag(vertex_data vertex_data):SV_Target
            {
                const float4 shadow_coords = TransformWorldToShadowCoord(vertex_data.world_position);
                const Light main_light = GetMainLight(shadow_coords);
                const float3 main_light_color = main_light.color;
                const float3 main_light_direction = normalize(main_light.direction);
                
                //const float3 view_direction = normalize(_WorldSpaceCameraPos.xyz - vertex_data.world_position.xyz);
                const float1 half_lambert_shadow = dot(vertex_data.world_normal, main_light_direction) * 0.5 + 0.5;

                const float4 base_texture_color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, vertex_data.uv);

                const float1 ramp_smooth = smoothstep(0, _ShadowSmooth, half_lambert_shadow - _ShadowRange);
                const float3 diffuse_color = lerp(_ShadowColor, _BaseColor, ramp_smooth.x).xyz;

                float4 color = 1;
                color.rgb = diffuse_color * base_texture_color.rgb * main_light_color;
                return color;
            }
            ENDHLSL

        }
        //Outline pass
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./Build-in.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _ShadowColor;
            float1 _ShadowRange;
            float1 _ShadowSmooth;
            float4 _OutlineColor;
            float1 _OutlineWidth;
            CBUFFER_END

            struct app_data
            {
                float4 position : POSITION;
                float4 object_normal : NORMAL;
                float4 object_tangent : TANGENT;
                float4 color :COLOR;
            };

            struct vertex_data
            {
                float4 clip_position : SV_POSITION;
            };

            vertex_data vert(app_data app_data)
            {
                vertex_data output;
                //Vertex spreads out along the normal direction
                const VertexPositionInputs position_inputs = GetVertexPositionInputs(app_data.position.xyz);
                float4 vertex_position = position_inputs.positionCS;

                //rotation is the matrix of model space to tangent space
                float3 bi_normal = cross(app_data.object_normal.xyz, app_data.object_tangent.xyz) * app_data.
                    object_tangent.w;
                const float3x3 rotation = float3x3(app_data.object_tangent.xyz, bi_normal, app_data.object_normal.xyz);

                //Remapping [0,1] to [-1,1]
                float3 a_normal = app_data.color.rgb * 2 - 1;
                a_normal = normalize(mul(transpose(rotation), a_normal));

                //Transform normals to NDC space
                float3 view_normal = mul((float3x3)UNITY_MATRIX_IT_MV, a_normal);
                float3 ndc_normal = normalize(TransformViewToProjection(view_normal.xyz)) * vertex_position.w;

                //Transform the vertex near the upper right corner of the clipping plane to view space
                const float aspect = _ScreenParams.x / _ScreenParams.y;
                ndc_normal.x /= aspect;

                //vertex offset
                const float outline_clamp = clamp(1 / vertex_position.w, 0, 1);
                vertex_position.xy += 0.01 * _OutlineWidth * ndc_normal.xy * outline_clamp;

                //Output to frag
                output.clip_position = vertex_position;
                return output;
            }


            float4 frag(vertex_data vertex_data):SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL

        }
    }
}