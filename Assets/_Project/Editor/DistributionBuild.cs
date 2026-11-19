using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonShadow.Editor
{
    public static class DistributionBuild
    {
        public static void Windows()
        {
            Prepare(false);
            var output = Path.GetFullPath("Builds/Windows/Moon Shadow.exe");
            Build(output, BuildTarget.StandaloneWindows64);
        }

        public static void Android()
        {
            Prepare(true);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.moonshadow.game");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            Build(Path.GetFullPath("Builds/Android/Moon Shadow.apk"), BuildTarget.Android);
        }

        private static void Prepare(bool android)
        {
            PlayerSettings.productName = "Moon Shadow";
            PlayerSettings.bundleVersion = "0.9.2";
            PlayerSettings.runInBackground = !android;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.gcIncremental = true;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            if (pipeline)
            {
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 1;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
                EditorUtility.SetDirty(pipeline);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer)
                    continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool backdrop = name.StartsWith("PixelVista", StringComparison.Ordinal) || name.StartsWith("Cinema", StringComparison.Ordinal);
                var mobile = importer.GetPlatformTextureSettings("Android");
                int size = backdrop ? 2048 : 1024;
                if (mobile.overridden && mobile.maxTextureSize == size && mobile.format == TextureImporterFormat.ETC2_RGBA8)
                    continue;
                mobile.name = "Android";
                mobile.overridden = true;
                mobile.maxTextureSize = size;
                mobile.format = TextureImporterFormat.ETC2_RGBA8;
                mobile.compressionQuality = 60;
                importer.SetPlatformTextureSettings(mobile);
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }

        private static void Build(string output, BuildTarget target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                target = target,
                locationPathName = output,
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                options = BuildOptions.CompressWithLz4HC
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"No se pudo exportar {target}. Unity registró {report.summary.totalErrors} errores.");
            Debug.Log($"MOON_DISTRIBUTION_READY {target} {output} {report.summary.totalSize} bytes");
        }
    }
}
