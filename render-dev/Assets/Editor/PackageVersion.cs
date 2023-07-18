using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class PackageVersion
{
    private static ListRequest _request;
    private static int _progressId;

    /// <summary>
    /// Package check will start on Editor start
    /// </summary>
    [InitializeOnLoadMethod]
    public static void GetPackageList()
    {
        // List packages installed for the project
        _request = Client.List();
        _progressId = Progress.Start("Package version check");
        //Injection update loop
        EditorApplication.update += VersionCheck;
    }

    /// <summary>
    /// Make sure the collection version conforms to the specification
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Error in Status</exception>
    private static void VersionCheck()
    {
        switch (_request.Status)
        {
            case StatusCode.Success:
            {
                Progress.Report(_progressId, 2, 3, "Checking the version");
                //version detection operation
                foreach (var info in _request.Result)
                    if (info.name == "com.unity.collections")
                        Debug.Log(info.version);

                Progress.Report(_progressId, 3, 3);
                break;
            }
            case StatusCode.Failure:
            {
                Debug.LogWarning(_request.Error.message);
                break;
            }
            case StatusCode.InProgress:
            {
                Progress.Report(_progressId, 1, 3, "Waiting package list");
                return;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        Progress.Remove(_progressId);
        EditorApplication.update -= VersionCheck;
    }
}