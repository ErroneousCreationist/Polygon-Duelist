using UnityEditor;
using UnityEngine;
using System.IO;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build Asset Bundles")]
    static void BuildAllAssetBundles()
    {
        string assetbundledirectory = "Assets/StreamingAssets";
        if(!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(assetbundledirectory);
        }
        BuildPipeline.BuildAssetBundles(assetbundledirectory, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}