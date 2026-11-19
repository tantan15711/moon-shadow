using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace MoonShadow
{
    public sealed partial class MoonDemoSmoke
    {
        private readonly List<float> frameSamples = new(16000);
        private long highestMemory;

        private void LateUpdate()
        {
            if (!demo || !demo.Playing || Time.unscaledDeltaTime > .15f || Time.realtimeSinceStartup < 3f)
                return;
            frameSamples.Add(Time.unscaledDeltaTime * 1000f);
            highestMemory = Math.Max(highestMemory, Profiler.GetTotalAllocatedMemoryLong());
        }

        private void ReportPerformance()
        {
            if (frameSamples.Count < 20)
                return;
            frameSamples.Sort();
            float total = 0f;
            foreach (float frame in frameSamples)
                total += frame;
            float mean = total / frameSamples.Count;
            float p95 = frameSamples[Math.Min(frameSamples.Count - 1, Mathf.FloorToInt(frameSamples.Count * .95f))];
            Debug.Log($"MOON_PERFORMANCE samples={frameSamples.Count} fps={1000f / mean:F1} frame_ms={mean:F2} p95_ms={p95:F2} peak_unity_mb={highestMemory / 1048576f:F1} resolution={Screen.width}x{Screen.height} gpu={SystemInfo.graphicsDeviceName}");
        }
    }
}
