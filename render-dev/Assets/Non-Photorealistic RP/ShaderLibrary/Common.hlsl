#ifndef NPRP_COMMON_HLSL
#define NPRP_COMMON_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

//Required in Unity 2022
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif