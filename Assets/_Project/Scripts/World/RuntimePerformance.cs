using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoonShadow
{
    internal static class RuntimePerformance
    {
        internal static readonly bool Mobile = Application.platform == RuntimePlatform.Android ||
            Array.IndexOf(Environment.GetCommandLineArgs(), "-moonMobileTest") >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Application.targetFrameRate = Mobile ? 60 : 120;
            QualitySettings.vSyncCount = Mobile ? 0 : 1;
            Time.maximumDeltaTime = .10f;
            Application.runInBackground = !Mobile;
            if (Mobile)
            {
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                if (Screen.width > 1600)
                    Screen.SetResolution(1600, Mathf.RoundToInt(Screen.height * (1600f / Screen.width)), true);
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.msaaSampleCount = 1;
                pipeline.supportsHDR = false;
                pipeline.supportsCameraDepthTexture = false;
                pipeline.supportsCameraOpaqueTexture = false;
            }
        }
    }
}
