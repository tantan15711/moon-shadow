using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    internal sealed class BossBanter
    {
        private readonly List<MoonStory.Line> beats = new();
        private bool started;
        private int page = -1;
        private float delay;
        private float age;
        public bool Active => started && page < beats.Count;

        public bool Speaking => page >= 0 && page < beats.Count;
        public bool IsKyle => Speaking && beats[page].Speaker == "Kyle";
        public float Gesture => Mathf.Repeat(age * 0.91f, 1f);
        public MoonStory.Line Line => beats[page];
        public int VisibleCharacters => Speaking ? Mathf.Min(Line.Text.Length, (int)(age * 66f)) : 0;

        public void Schedule(MoonStory.Line[] script)
        {
            if (started)
                return;
            started = true;
            delay = 0.34f;
            foreach (var line in script)
            {
                string left = line.Text;
                while (left.Length > 124)
                {
                    int split = left.LastIndexOfAny(new[] { '.', '?', '!', ',' }, 123, 64);
                    if (split >= 60)
                        split++;
                    else
                        split = left.LastIndexOf(' ', 123);
                    if (split <= 0)
                        split = 124;
                    beats.Add(new MoonStory.Line(line.Speaker, left[..split].Trim()));
                    left = left[split..].TrimStart();
                }
                beats.Add(new MoonStory.Line(line.Speaker, left));
            }
        }

        public void Tick(float delta, bool advance)
        {
            if (!started)
                return;
            if (page < 0)
            {
                delay -= delta;
                if (delay <= 0f)
                    page = 0;
                return;
            }
            if (!Speaking)
                return;
            age += delta;
            if (!advance)
                return;
            if (VisibleCharacters < Line.Text.Length)
            {
                age = Line.Text.Length / 66f + 0.01f;
                return;
            }
            page++;
            age = 0f;
        }

        public void Stop()
        {
            if (started)
                page = beats.Count;
        }
    }
}
