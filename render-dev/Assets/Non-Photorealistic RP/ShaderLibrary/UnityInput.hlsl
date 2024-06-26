// ReSharper disable CppInconsistentNaming
#ifndef NPRP_UNITY_INPUT_HLSL
#define NPRP_UNITY_INPUT_HLSL

float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
real4 unity_WorldTransformParams;

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

//Required in Unity 2022
float4x4 unity_prev_MatrixM;
float4x4 unity_prev_MatrixIM;
float4x4 unity_MatrixInvV;

#endif