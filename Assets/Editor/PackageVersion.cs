using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class PackageVersion
{
    private static ListRequest _request;
    private static int ProgressId = Progress.Start("Package version check");

    [MenuItem("Window/Package Version Check Example")]
    public static void GetPackageList()
    {
        // List packages installed for the project
        _request = Client.List();

        //Injection update loop
        EditorApplication.update += VersionCheck;
    }

    private static void VersionCheck()
    {
        switch (_request.Status)
        {
            case StatusCode.Success:
            {
                Progress.Report(ProgressId, 2, 3, "Checking the version");
                //version detection operation
                foreach (var info in _request.Result)
                {
                    if (info.name == "com.unity.collections")
                    {
                        Debug.Log(info.version);
                    }
                }
                Progress.Report(ProgressId, 3, 3);
                break;
            }
            case StatusCode.Failure:
            {
                Debug.LogWarning(_request.Error.message);
                break;
            }
            case StatusCode.InProgress:
            {
                Progress.Report(ProgressId, 1, 3, "Waiting package list");
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        Progress.Remove(ProgressId);
        EditorApplication.update -= VersionCheck;
    }
}