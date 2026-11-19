using System.Collections.Generic;
using UnityEngine;

namespace MoonShadow
{
    public sealed partial class CastRig
    {
        private readonly struct Band
        {
            public readonly float Row, Left, Right;

            public Band(float row, float left, float right)
            {
                Row = row;
                Left = left;
                Right = right;
            }
        }

        // The silhouette is split along seams in PixelKyleRestored. Overlapping bands at
        // the shoulder and belt cover the joints as the arms and legs pass each other.
        private void BindKyleSkin(Vector2 bottom, Vector2 size)
        {
            const float width = 285f, height = 416f;
            Vector2 texel = new(size.x / width, size.y / height);
            Vector2 Pixel(float x, float y) => bottom + Vector2.Scale(new Vector2(x, height - y), texel);
            void Bone(int bone, float x, float y, float tx, float ty)
            {
                joints[bone] = Pixel(x, y);
                tips[bone] = Pixel(tx, ty);
            }

            Bone(0, 181, 199, 177, 77);
            Bone(1, 178, 75, 181, 19);
            Bone(2, 128, 97, 109, 158);
            Bone(3, 109, 158, 112, 202);
            Bone(NearHand, 112, 202, 107, 234);
            const float farArmShift = 71f;
            Bone(4, 128 + farArmShift, 97, 109 + farArmShift, 158);
            Bone(5, 109 + farArmShift, 158, 112 + farArmShift, 202);
            Bone(FarHand, 112 + farArmShift, 202, 107 + farArmShift, 234);
            Bone(6, 154, 206, 134, 296);
            Bone(7, 134, 296, 121, 373);
            Bone(NearFoot, 121, 373, 146, 405);
            Bone(8, 196, 201, 196, 300);
            Bone(9, 196, 300, 195, 371);
            Bone(FarFoot, 195, 371, 247, 400);
            Bone(Blade, 106, 238, 26, 330);

            var verts = new List<Vector3>(1300);
            var uv = new List<Vector2>(1300);
            var shades = new List<Color32>(1300);
            var binding = new List<SkinWeight>(1300);
            var tris = new List<int>(5500);
            var white = new Color32(255, 255, 255, 255);
            var farSide = new Color32(155, 169, 190, 255);
            var farLeg = new Color32(193, 201, 218, 255);
            var arm = new Band[]
            {
                new(79, 108, 148), new(94, 105, 157), new(119, 97, 146),
                new(139, 90, 139), new(165, 93, 134), new(177, 97, 131),
                new(202, 98, 125), new(222, 91, 130), new(238, 94, 125), new(246, 99, 120)
            };

            Strip(Blade, Blade, Blade, white, 0f,
                new(229, 90, 122), new(242, 85, 113), new(273, 62, 91),
                new(299, 38, 67), new(326, 19, 47), new(343, 20, 39));
            Strip(4, 5, FarHand, farSide, farArmShift, arm);
            Strip(8, 9, FarFoot, farLeg, 0f,
                new(180, 174, 211), new(215, 173, 218), new(249, 172, 224),
                new(276, 175, 222), new(307, 173, 226), new(337, 167, 224),
                new(368, 171, 227), new(388, 171, 255), new(415, 173, 261));
            Strip(6, 7, NearFoot, white, 0f,
                new(187, 134, 180), new(210, 131, 189), new(239, 119, 186),
                new(262, 116, 180), new(282, 110, 171), new(308, 104, 159),
                new(333, 97, 150), new(361, 94, 144), new(384, 93, 151), new(415, 94, 158));
            Strip(0, 0, 0, white, 0f,
                new(62, 149, 199), new(82, 134, 214), new(100, 136, 216),
                new(122, 143, 216), new(148, 135, 222), new(174, 127, 224),
                new(189, 129, 219), new(206, 137, 214), new(223, 154, 199));
            Strip(1, 1, 1, white, 0f,
                new(0, 120, 237), new(53, 132, 222), new(68, 146, 205), new(82, 153, 197));
            Strip(2, 3, NearHand, white, 0f, arm);

            original = verts.ToArray();
            deformed = verts.ToArray();
            weights = binding.ToArray();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            mesh.SetColors(shades);
            mesh.SetTriangles(tris, 0);
            return;

            void Strip(int top, int middle, int end, Color32 tint, float shift, params Band[] rows)
            {
                const int across = 7;
                int first = verts.Count;
                AddRow(rows[0]);
                for (int r = 1; r < rows.Length; r++)
                {
                    Band from = rows[r - 1], to = rows[r];
                    int divisions = Mathf.CeilToInt((to.Row - from.Row) / 7f);
                    for (int i = 1; i <= divisions; i++)
                    {
                        float t = (float)i / divisions;
                        AddRow(new Band(Mathf.Lerp(from.Row, to.Row, t),
                            Mathf.Lerp(from.Left, to.Left, t), Mathf.Lerp(from.Right, to.Right, t)));
                    }
                }

                for (int row = first; row < verts.Count - across; row += across)
                for (int column = 0; column < across - 1; column++)
                {
                    int v = row + column;
                    tris.Add(v); tris.Add(v + across); tris.Add(v + 1);
                    tris.Add(v + 1); tris.Add(v + across); tris.Add(v + across + 1);
                }

                void AddRow(Band row)
                {
                    for (int x = 0; x < across; x++)
                    {
                        float sourceX = Mathf.Lerp(row.Left, row.Right, x / (across - 1f));
                        Vector2 p = Pixel(sourceX + shift, row.Row);
                        var coordinate = skin.rect.position + new Vector2(sourceX, height - row.Row);
                        verts.Add(p);
                        uv.Add(new Vector2(coordinate.x / skin.texture.width, coordinate.y / skin.texture.height));
                        shades.Add(tint);
                        binding.Add(RibbonWeight(p, top, middle, end));
                    }
                }
            }
        }

        private SkinWeight RibbonWeight(Vector2 vertex, int top, int middle, int end)
        {
            if (top == end)
                return new SkinWeight(top, top, 0f);
            float zone = modelHeight * 0.042f;
            float wrist = joints[end].y;
            if (vertex.y < wrist + zone)
                return new SkinWeight(middle, end, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wrist + zone, wrist - zone, vertex.y)));
            float hinge = joints[middle].y;
            return new SkinWeight(top, middle, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hinge + zone, hinge - zone, vertex.y)));
        }
    }
}
