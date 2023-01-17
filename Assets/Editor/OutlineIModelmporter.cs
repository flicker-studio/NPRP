using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;

public class ModelOutlineImporter : AssetPostprocessor
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
        Debug.Log($"Outline copy {model.name} import success!");
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
            Debug.Log(modelObject.name + "delete the smoothed");
        }
        else
        {
            //Copy the model and add the Prefix
            AssetDatabase.CopyAsset(srcPath, dstPath);
            AssetDatabase.ImportAsset(dstPath);
        }
    }

    /// <summary>
    /// Get all meshes in the model and children,MeshFilter and SkinnedMesh
    /// </summary>
    /// <param name="go">target model</param>
    /// <returns>The meshes dictionary </returns>
    private Dictionary<string, Mesh> GetMesh(GameObject go)
    {
        Dictionary<string, Mesh> dic = new Dictionary<string, Mesh>();
        foreach (var item in go.GetComponentsInChildren<MeshFilter>())
            dic.Add(item.name, item.sharedMesh);
        if (dic.Count == 0)
            foreach (var item in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                dic.Add(item.name, item.sharedMesh);
        return dic;
    }

    private struct CollectNormalJob : IJobParallelFor
    {
        // Mark read-only to improve performance
        [ReadOnly] private NativeArray<Vector3> m_Normals, m_Vertex;
        // Cancel Job Security Check
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<UnsafeHashMap<Vector3, Vector3>.ParallelWriter> m_Result;

        public CollectNormalJob(NativeArray<Vector3> normals, NativeArray<Vector3> vertex, NativeArray<UnsafeHashMap<Vector3, Vector3>.ParallelWriter> result)
        {
            m_Normals = normals;
            m_Vertex = vertex;
            m_Result = result;
        }

        void IJobParallelFor.Execute(int index)
        {
            for (var i = 0; i < m_Result.Length + 1; i++)
            {
                if (i == m_Result.Length)
                {
                    Debug.LogError($"The number of coincident vertices ({i}) exceeds the limit!");
                    break;
                }

                if (m_Result[i].TryAdd(m_Vertex[index], m_Normals[index]))
                {
                    break;
                }
            }
        }
    }

    private struct BakeNormalJob : IJobParallelFor
    {
        [ReadOnly] private NativeArray<Vector3> m_Normals;
        [ReadOnly] private NativeArray<Vector4> m_Tangents;
        [ReadOnly] private NativeArray<Vector3> m_Vertex;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] private NativeArray<UnsafeHashMap<Vector3, Vector3>> m_Result;
        [ReadOnly] private readonly bool m_ExistColors;
        private NativeArray<Color> m_Colors;

        public BakeNormalJob(NativeArray<Vector3> vertex, NativeArray<Vector3> normals, NativeArray<Vector4> tangents, NativeArray<UnsafeHashMap<Vector3, Vector3>> result, bool existColors, NativeArray<Color> colors)
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
            for (var i = 0; i < m_Result.Length; i++)
            {
                if (m_Result[i][m_Vertex[index]] != Vector3.zero)
                    smoothedNormals += m_Result[i][m_Vertex[index]];
                else
                    break;
            }

            smoothedNormals = smoothedNormals.normalized;

            var biNormal = (Vector3.Cross(m_Normals[index], m_Tangents[index]) * m_Tangents[index].w).normalized;

            var tbn = new Matrix4x4(
                m_Tangents[index],
                biNormal,
                m_Normals[index],
                Vector4.zero);
            tbn = tbn.transpose;
            var bakedNormal = tbn.MultiplyVector(smoothedNormals).normalized;

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

    private Color[] ComputeSmoothedNormalByJob(Mesh smoothedMesh, Mesh originalMesh, int maxOverlapVertices = 1000)
    {
        int svc = smoothedMesh.vertexCount, ovc = originalMesh.vertexCount;

        // CollectNormalJob Data
        NativeArray<Vector3> normals = new NativeArray<Vector3>(smoothedMesh.normals, Allocator.Persistent),
            vertex = new NativeArray<Vector3>(smoothedMesh.vertices, Allocator.Persistent),
            smoothedNormals = new NativeArray<Vector3>(svc, Allocator.Persistent);
        var result = new NativeArray<UnsafeHashMap<Vector3, Vector3>>(maxOverlapVertices, Allocator.Persistent);
        var resultParallel = new NativeArray<UnsafeHashMap<Vector3, Vector3>.ParallelWriter>(result.Length, Allocator.Persistent);
        // NormalBakeJob Data
        NativeArray<Vector3> normalsO = new NativeArray<Vector3>(originalMesh.normals, Allocator.Persistent),
            vertexO = new NativeArray<Vector3>(originalMesh.vertices, Allocator.Persistent);
        var tangents = new NativeArray<Vector4>(originalMesh.tangents, Allocator.Persistent);
        var colors = new NativeArray<Color>(ovc, Allocator.Persistent);

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new UnsafeHashMap<Vector3, Vector3>(svc, Allocator.Persistent);
            resultParallel[i] = result[i].AsParallelWriter();
        }

        bool existColors = originalMesh.colors.Length == ovc;
        if (existColors)
            colors.CopyFrom(originalMesh.colors);

        CollectNormalJob collectNormalJob = new CollectNormalJob(normals, vertex, resultParallel);
        BakeNormalJob normalBakeJob = new BakeNormalJob(vertexO, normalsO, tangents, result, existColors, colors);

        // 以100为粒度运行job，先运行collectNormalJob再运行normalBakeJob
        normalBakeJob.Schedule(ovc, 100, collectNormalJob.Schedule(svc, 100)).Complete();

        // Copy result, release memory
        var resultColors = new Color[colors.Length];
        colors.CopyTo(resultColors);

        normals.Dispose();
        vertex.Dispose();
        result.Dispose();
        smoothedNormals.Dispose();
        resultParallel.Dispose();
        normalsO.Dispose();
        vertexO.Dispose();
        tangents.Dispose();
        colors.Dispose();

        return resultColors;
    }
}