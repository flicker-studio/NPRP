using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

internal struct BakeNormalJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<Vector3> _normals;
    [ReadOnly] private NativeArray<Vector4> _tangents;
    [ReadOnly] private NativeArray<Vector3> _vertex;
    [ReadOnly] private NativeParallelMultiHashMap<Vector3, Vector3> _result;
    [ReadOnly] private readonly bool _existColors;
    private NativeArray<Color> _colors;

    public BakeNormalJob(NativeArray<Vector3> vertex, NativeArray<Vector3> normals, NativeArray<Vector4> tangents,
        NativeParallelMultiHashMap<Vector3, Vector3> result, bool existColors, NativeArray<Color> colors)
    {
        _normals = normals;
        _tangents = tangents;
        _vertex = vertex;
        _result = result;
        _existColors = existColors;
        _colors = colors;
    }

    void IJobParallelFor.Execute(int index)
    {
        var smoothedNormals = Vector3.zero;
        var vertex = _vertex[index];

        foreach (var values in _result.GetValuesForKey(vertex)) smoothedNormals += values;

        smoothedNormals = smoothedNormals.normalized;

        var biNormal = (Vector3.Cross(_normals[index], _tangents[index]) * _tangents[index].w).normalized;

        var tbn = new Matrix4x4(
            _tangents[index],
            biNormal,
            _normals[index],
            Vector4.zero);
        tbn = tbn.transpose;

        //Calculate the normal vector in model space
        var bakedNormal = tbn.MultiplyVector(smoothedNormals).normalized;

        //Remapping [-1,1] to [0,1]
        var color = new Color
        {
            r = bakedNormal.x * 0.5f + 0.5f,
            g = bakedNormal.y * 0.5f + 0.5f,
            b = bakedNormal.z * 0.5f + 0.5f,
            /*
             * Choose a color channel
             * b = _existColors ? m_Colors[index].b : 1,
             */
            a = _existColors ? _colors[index].a : 1
        };
        _colors[index] = color;
    }
}