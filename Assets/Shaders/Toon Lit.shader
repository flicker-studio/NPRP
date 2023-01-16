Shader "Toon Render/Toon_Lit"
{
    Properties
    {
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

            struct app_data
            {
                float4 vertex_position : POSITION;
            };

            struct v2f
            {
                float4 fragment_position : SV_POSITION;
            };

            v2f vert(app_data input)
            {
                v2f output;
                const VertexPositionInputs position_inputs = GetVertexPositionInputs(input.vertex_position.xyz);
                output.fragment_position = position_inputs.positionCS;
                return output;
            }


            float4 frag(v2f input):SV_Target
            {
                float4 last_color = {1, 1, 1, 1};
                return last_color;
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
                //Transform normals to NDC space
                float3 view_normal = mul((float3x3)UNITY_MATRIX_IT_MV, app_data.normal.xyz);
                float3 ndc_normal = normalize(TransformViewToProjection(view_normal.xyz)) * vertex_position.w;
                vertex_position.xy += 0.01 * _OutlineWidth * ndc_normal.xy;
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