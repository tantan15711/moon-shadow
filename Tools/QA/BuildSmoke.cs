using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoonShadow.Editor
{
    public static class BuildSmoke
    {
        public static void Build()
        {
            string path = Path.GetFullPath("_Logs/QAPlayer/Moon Shadow QA.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string product = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = "Moon Shadow QA";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = path,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("QA build failed");
                Debug.Log("MOON_QA_BUILT");
            }
            finally
            {
                PlayerSettings.productName = product;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
