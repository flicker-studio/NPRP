using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using System.IO;

internal struct CollectNormalJob : IJobParallelFor
{
    // Mark read-only to improve performance
    [ReadOnly] private NativeArray<Vector3> m_Normals, m_Vertex;

    private NativeMultiHashMap<Vector3, Vector3>.ParallelWriter m_Result;

    public CollectNormalJob(NativeArray<Vector3> normals, NativeArray<Vector3> vertex, NativeMultiHashMap<Vector3, Vector3>.ParallelWriter result)
    {
        m_Normals = normals;
        m_Vertex = vertex;
        m_Result = result;
    }

    void IJobParallelFor.Execute(int index)
    {
        m_Result.Add(m_Vertex[index], m_Normals[index]);
    }
}

internal struct BakeNormalJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<Vector3> m_Normals;
    [ReadOnly] private NativeArray<Vector4> m_Tangents;
    [ReadOnly] private NativeArray<Vector3> m_Vertex;
    [ReadOnly] private NativeMultiHashMap<Vector3, Vector3> m_Result;
    [ReadOnly] private readonly bool m_ExistColors;
    private NativeArray<Color> m_Colors;

    public BakeNormalJob(NativeArray<Vector3> vertex, NativeArray<Vector3> normals, NativeArray<Vector4> tangents, NativeMultiHashMap<Vector3, Vector3> result, bool existColors, NativeArray<Color> colors)
    {
        m_Normals = normals;
        m_Tangents = tangents;
        m_Vertex = vertex;
        m_Result = result;
        m_ExistColors = existColors;
        m_Colors = colors;
    }

    void IJobParallelFor.Execute(int index)
    {
        var smoothedNormals = Vector3.zero;
        var vertex = m_Vertex[index];

        foreach (var values in m_Result.GetValuesForKey(vertex))
        {
            smoothedNormals += values;
        }

        smoothedNormals = smoothedNormals.normalized;

        var biNormal = (Vector3.Cross(m_Normals[index], m_Tangents[index]) * m_Tangents[index].w).normalized;

        var tbn = new Matrix4x4(
            m_Tangents[index],
            biNormal,
            m_Normals[index],
            Vector4.zero);
        tbn = tbn.transpose;
        
        //Calculate the normal vector in model space
        var bakedNormal = tbn.MultiplyVector(smoothedNormals).normalized;

        //Remapping [-1,1] to [0,1]
        var color = new Color
        {
            r = (bakedNormal.x * 0.5f) + 0.5f,
            g = (bakedNormal.y * 0.5f) + 0.5f,
            b = (bakedNormal.z * 0.5f) + 0.5f,
            // b = m_ExistColors ? m_Colors[index].b : 1,
            a = m_ExistColors ? m_Colors[index].a : 1
        };
        m_Colors[index] = color;
    }
}

public class OutlineModelImporter : AssetPostprocessor
{
    private const string Suffix = "_ol";
    private const string Prefix = "smoothed_";

    // Called before model import
    private void OnPreprocessModel()
    {
        // Only smoothed_xxx can enter
        if (!assetPath.Contains(Prefix)) return;

        // Change the import settings
        // Use Unity's own algorithm to smooth the model will force the merge of coincident vertices
        var model = assetImporter as ModelImporter;
        if (model == null) throw new NullReferenceException();

        model.importNormals = ModelImporterNormals.Calculate;
        model.normalCalculationMode = ModelImporterNormalCalculationMode.AngleWeighted;
        model.normalSmoothingAngle = 180.0f;
        model.importAnimation = false;
        model.materialImportMode = ModelImporterMaterialImportMode.None;
        Debug.Log("Temporary file created successfully");
    }

    // Called after the Game Object is generated
    // The modification to the Game Object will affect the generated result but the reference will not be retained
    private void OnPostprocessModel(GameObject modelObject)
    {
        // Only xxx_ol can enter
        if (!modelObject.name.Contains(Suffix)) return;
        if (modelObject.name.Contains(Prefix)) return;

        var model = assetImporter as ModelImporter;
        if (model == null) throw new NullReferenceException();

        string srcPath = model.assetPath;
        string dstPath = Path.GetDirectoryName(srcPath) + "/" + Prefix + Path.GetFileName(srcPath);

        string copyPath = Application.dataPath + "/" + dstPath[7..];
        if (File.Exists(copyPath))
        {
            //If it exists, smooth its normal
            var copy = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);

            Dictionary<string, Mesh> originalMesh = GetMesh(modelObject);
            Dictionary<string, Mesh> smoothedMesh = GetMesh(copy);

            foreach (var (name, mesh) in originalMesh)
            {
                mesh.colors = ComputeSmoothedNormalByJob(smoothedMesh[Prefix + name], mesh);
            }

            AssetDatabase.DeleteAsset(dstPath);
            AssetDatabase.Refresh();
            Debug.Log("Temporary file deleted successfully");
        }
        else
        {
            //Copy the model and add the Prefix
            AssetDatabase.CopyAsset(srcPath, dstPath);
            AssetDatabase.ImportAsset(dstPath);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Get all meshes in the model and children,MeshFilter and SkinnedMesh
    /// </summary>
    /// <param name="go">target model</param>
    /// <returns>The meshes dictionary </returns>
    private Dictionary<string, Mesh> GetMesh(GameObject go)
    {
        var dic = new Dictionary<string, Mesh>();

        foreach (var item in go.GetComponentsInChildren<MeshFilter>())
            dic.Add(item.name, item.sharedMesh);
        foreach (var item in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            dic.Add(item.name, item.sharedMesh);

        return dic;
    }


    private Color[] ComputeSmoothedNormalByJob(Mesh smoothedMesh, Mesh originalMesh)
    {
        var smoothedMeshVertexCount = smoothedMesh.vertexCount;
        var originalMeshVertexCount = originalMesh.vertexCount;

        // original data
        var originalNormals = new NativeArray<Vector3>(originalMesh.normals, Allocator.Persistent);
        var originalVertices = new NativeArray<Vector3>(originalMesh.vertices, Allocator.Persistent);
        var originalTangents = new NativeArray<Vector4>(originalMesh.tangents, Allocator.Persistent);
        var originalColors = new NativeArray<Color>(originalMeshVertexCount, Allocator.Persistent);
        var existColors = originalMesh.colors.Length == originalMeshVertexCount;
        if (existColors) originalColors.CopyFrom(originalMesh.colors);

        // smoothed data
        var smoothedNormals = new NativeArray<Vector3>(smoothedMesh.normals, Allocator.Persistent);
        var smoothedVertices = new NativeArray<Vector3>(smoothedMesh.vertices, Allocator.Persistent);
        var targetNormals = new NativeArray<Vector3>(smoothedMeshVertexCount, Allocator.Persistent);

        //result data
        var result = new NativeMultiHashMap<Vector3, Vector3>(originalMeshVertexCount * 3, Allocator.Persistent);
        var resultParallel = result.AsParallelWriter();

        var collectNormalJob = new CollectNormalJob(smoothedNormals, smoothedVertices, resultParallel);
        var normalBakeJob = new BakeNormalJob(originalVertices, originalNormals, originalTangents, result, existColors, originalColors);

        //Job execution
        normalBakeJob.Schedule(originalMeshVertexCount, 100,
            collectNormalJob.Schedule(smoothedMeshVertexCount, 100)).Complete();

        // Copy result
        var resultColors = new Color[originalColors.Length];
        originalColors.CopyTo(resultColors);

        // release memory
        smoothedNormals.Dispose();
        smoothedVertices.Dispose();
        targetNormals.Dispose();
        originalNormals.Dispose();
        originalVertices.Dispose();
        originalTangents.Dispose();
        originalColors.Dispose();
        result.Dispose();

        Debug.Log("Compute smoothed normal by job complete");
        return resultColors;
    }
}