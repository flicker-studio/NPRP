Shader "Toon Render/Toon_Lit"
{
    Properties
    {
        _BaseColor("Base Color",color ) = (1,1,1,1)
        _OutlineColor("Outline Color",color) = (0,0,0,1)
        _OutlineWidth("Outline Width",Range(0,1)) = .2
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
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END

            struct app_data
            {
                float4 vertex_position : POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color :COLOR;
            };

            struct vertex_data
            {
                float4 position : SV_POSITION;
            };

            vertex_data vert(app_data app_input)
            {
                vertex_data output;
                const VertexPositionInputs position_inputs = GetVertexPositionInputs(app_input.vertex_position.xyz);
                output.position = position_inputs.positionCS;
                return output;
            }


            float4 frag(vertex_data vertex_input):SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL

        }
        Pass
        {
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./Build-in.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float1 _OutlineWidth;
            CBUFFER_END

            struct app_data
            {
                float4 position : POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 color :COLOR;
            };

            struct vertex_data
            {
                float4 position : SV_POSITION;
            };

            vertex_data vert(app_data app_data)
            {
                vertex_data output;
                //Vertex spreads out along the normal direction
                const VertexPositionInputs position_inputs = GetVertexPositionInputs(app_data.position.xyz);
                float4 vertex_position = position_inputs.positionCS;

                //rotation is the matrix of model space to tangent space
                float3 bi_normal = cross(app_data.normal, app_data.tangent.xyz) * app_data.tangent.w;
                const float3x3 rotation = float3x3(app_data.tangent.xyz, bi_normal, app_data.normal.xyz);

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
                output.position = vertex_position;
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