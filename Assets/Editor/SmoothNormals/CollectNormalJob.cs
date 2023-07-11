using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

internal struct CollectNormalJob : IJobParallelFor
{
    // Mark read-only to improve performance
    [ReadOnly] private NativeArray<Vector3> _normals, _vertex;

    private NativeParallelMultiHashMap<Vector3, Vector3>.ParallelWriter _result;

    public CollectNormalJob(NativeArray<Vector3> normals, NativeArray<Vector3> vertex,
        NativeParallelMultiHashMap<Vector3, Vector3>.ParallelWriter result)
    {
        _normals = normals;
        _vertex = vertex;
        _result = result;
    }

    void IJobParallelFor.Execute(int index)
    {
        _result.Add(_vertex[index], _normals[index]);
    }
}