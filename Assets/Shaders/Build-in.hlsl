//The reproduction of some Build-in Render Pipeline functions
inline float2 TransformViewToProjection(float2 v)
{
    return mul((float2x2)UNITY_MATRIX_P, v);
}

inline float3 TransformViewToProjection(float3 v)
{
    return mul((float3x3)UNITY_MATRIX_P, v);
}
