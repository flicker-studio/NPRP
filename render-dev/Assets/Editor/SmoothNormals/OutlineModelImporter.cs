using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class OutlineModelImporter : AssetPostprocessor
{
    private const string Suffix = "_ol";
    private const string Prefix = "smoothed_";

    // Called after the Game Object is generated
    // The modification to the Game Object will affect the generated result but the reference will not be retained
    private void OnPostprocessModel(GameObject modelObject)
    {
        // Only xxx_ol can enter
        if (!modelObject.name.Contains(Suffix)) return;
        if (modelObject.name.Contains(Prefix)) return;

        var model = assetImporter as ModelImporter;
        if (model == null) throw new NullReferenceException();

        var srcPath = model.assetPath;
        var dstPath = Path.GetDirectoryName(srcPath) + "/" + Prefix + Path.GetFileName(srcPath);

        var copyPath = Application.dataPath + "/" + dstPath[7..];
        if (File.Exists(copyPath))
        {
            //If the object exists, smooth its normal
            var copy = AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);

            var originalMesh = GetMesh(modelObject);
            var smoothedMesh = GetMesh(copy);

            foreach (var (name, mesh) in originalMesh)
                mesh.colors = ComputeSmoothedNormalByJob(smoothedMesh[Prefix + name], mesh);

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

    /// <summary>
    ///     Get all meshes in the model and children,MeshFilter and SkinnedMesh
    /// </summary>
    /// <param name="go">Target model</param>
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

        // Original data
        var originalNormals = new NativeArray<Vector3>(originalMesh.normals, Allocator.Persistent);
        var originalVertices = new NativeArray<Vector3>(originalMesh.vertices, Allocator.Persistent);
        var originalTangents = new NativeArray<Vector4>(originalMesh.tangents, Allocator.Persistent);
        var originalColors = new NativeArray<Color>(originalMeshVertexCount, Allocator.Persistent);
        var existColors = originalMesh.colors.Length == originalMeshVertexCount;
        if (existColors) originalColors.CopyFrom(originalMesh.colors);

        // Smoothed data
        var smoothedNormals = new NativeArray<Vector3>(smoothedMesh.normals, Allocator.Persistent);
        var smoothedVertices = new NativeArray<Vector3>(smoothedMesh.vertices, Allocator.Persistent);
        var targetNormals = new NativeArray<Vector3>(smoothedMeshVertexCount, Allocator.Persistent);

        // Result data
        var result =
            new NativeParallelMultiHashMap<Vector3, Vector3>(originalMeshVertexCount * 3, Allocator.Persistent);
        var resultParallel = result.AsParallelWriter();

        var collectNormalJob = new CollectNormalJob(smoothedNormals, smoothedVertices, resultParallel);
        var normalBakeJob = new BakeNormalJob(originalVertices, originalNormals, originalTangents, result, existColors,
            originalColors);

        // Job execution
        normalBakeJob.Schedule(originalMeshVertexCount, 100,
            collectNormalJob.Schedule(smoothedMeshVertexCount, 100)).Complete();

        // Copy result
        var resultColors = new Color[originalColors.Length];
        originalColors.CopyTo(resultColors);

        // Release memory
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