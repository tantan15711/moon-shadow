using System;
using UnityEngine;

namespace MoonShadow
{
    internal static class WindowsDisplay
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartFullscreen()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer || Application.isBatchMode ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "-screen-fullscreen") >= 0)
                return;

            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }
    }
}

