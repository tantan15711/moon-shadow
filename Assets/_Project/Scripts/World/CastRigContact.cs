using UnityEngine;

namespace MoonShadow
{
    public sealed partial class CastRig
    {
        private struct Contact
        {
            public bool Planted;
            public Collider2D Support;
            public Vector2 Point;
        }

        private readonly Contact[] contacts = new Contact[2];
        private float stance;
        private float handBlend;

        internal bool FootPlanted(int index) => contacts[index].Planted;
        internal Vector2 FootPosition(int index) =>
            renderer ? renderer.transform.TransformPoint(positions[index == 0 ? NearFoot : FarFoot]) : transform.position;

        internal Vector2 Grip => renderer
            ? renderer.transform.TransformPoint(layeredKyle ? KyleGrip() : Endpoint(3))
            : transform.position;

        internal void ResetContacts()
        {
            contacts[0] = default;
            contacts[1] = default;
        }

        private bool HeavyActor => role is Role.Brute or Role.Orbital or Role.Capture or Role.Vulkano;

        private float StrideSpan(float speed) => HeavyActor ? 1.04f : Mathf.Lerp(0.74f, 1.0f, Mathf.InverseLerp(1f, 5f, speed));

        private void MatchGround(float floor, float velocity, int facing, Motion motion, LayerMask terrain, float dt)
        {
            if (float.IsNaN(floor))
            {
                ResetContacts();
                return;
            }
            float speed = Mathf.Abs(velocity);
            float stanceEnd = HeavyActor ? .67f : Mathf.Lerp(.63f, .50f, Mathf.InverseLerp(1.7f, 4.8f, speed));
            for (int i = 0; i < contacts.Length; i++)
            {
                int upper = i == 0 ? 6 : 8;
                int lower = upper + 1;
                int foot = i == 0 ? NearFoot : FarFoot;
                float step = Mathf.Repeat(phase + (i == 0 ? 0f : 0.5f), 1f);
                bool walking = speed > 0.30f && motion is Motion.Travel or Motion.Aim or Motion.Reload or Motion.Surge;
                float swing = walking ? Mathf.InverseLerp(stanceEnd, 1f, step) : 0f;
                var space = renderer.transform;
                Vector2 ankle = space.TransformPoint(joints[foot]);
                float bodyHeight = modelHeight * Mathf.Abs(transform.lossyScale.y);
                float stepWidth = bodyHeight * StrideSpan(speed) * stanceEnd * 0.46f;
                float direction = Mathf.Sign(velocity);
                float placement = walking ? (swing > 0f ? Mathf.SmoothStep(-stepWidth, stepWidth, swing) :
                    Mathf.Lerp(stepWidth, -stepWidth, step / stanceEnd)) : 0f;
                float x = ankle.x + placement * direction;
                var hit = Physics2D.Raycast(new Vector2(x, floor + 0.42f), Vector2.down, 0.95f, terrain);
                Vector2 sole = hit ? hit.point : new Vector2(Mathf.Lerp(x, ankle.x, 0.68f), floor);
                ref Contact contact = ref contacts[i];
                if (walking && swing <= 0f && hit)
                {
                    if (contact.Planted && contact.Support)
                    {
                        Vector2 held = contact.Support.transform.TransformPoint(contact.Point);
                        if (Mathf.Abs(held.x - ankle.x) < stepWidth * 1.26f &&
                            Mathf.Abs(held.y - floor) < 0.34f)
                            sole = held;
                    }
                    contact.Planted = true;
                    contact.Support = hit.collider;
                    contact.Point = hit.collider.transform.InverseTransformPoint(sole);
                }
                else
                {
                    contact.Planted = !walking && hit;
                    contact.Support = null;
                }
                float soleOffset = role == Role.Kyle ? (i == 0 ? 0.056f : 0.038f) :
                    Mathf.Clamp((joints[foot].y - skin.bounds.min.y) / modelHeight - 0.014f, 0.034f, 0.094f);
                sole.y += bodyHeight * soleOffset;
                if (swing > 0f)
                    sole.y += Mathf.Sin(Mathf.PI * Mathf.Pow(swing, .71f)) * bodyHeight * (HeavyActor ? .105f : .151f);
                Vector2 target = space.InverseTransformPoint(sole);
                float getFoot = dt <= 0f ? 1f : 1f - Mathf.Exp(-dt * 44f);
                SolveChain(upper, lower, target, 1, getFoot);
                positions[foot] = Endpoint(lower);
                float tilt = hit ? Mathf.Atan2(-hit.normal.x * facing, hit.normal.y) : 0f;
                tilt += swing > 0f ? Mathf.Sin(swing * Mathf.PI * 2f) * 0.17f : 0f;
                SetDirection(foot, tilt);
            }
        }

        private void PoseHands(Motion motion, float progress, int facing, float bladeAngle, Vector2 aim, float smooth)
        {
            bool armed = motion is Motion.Aim or Motion.Reload;
            bool holdingBlade = role == Role.Kyle && !float.IsNaN(bladeAngle);
            bool hands = motion is Motion.Claw or Motion.Grab or Motion.Pound;
            handBlend = Mathf.Lerp(handBlend, armed || holdingBlade || hands ? 1f : 0f, smooth);
            if (armed || holdingBlade || hands)
            {
                float angle;
                if (armed)
                    angle = Mathf.Atan2(aim.y, aim.x * facing) + (motion == Motion.Reload ? 0.60f * Mathf.Sin(progress * Mathf.PI) : 0f);
                else if (holdingBlade)
                    angle = bladeAngle * Mathf.Deg2Rad;
                else
                {
                    float sweep = Ease(.18f, .76f, progress);
                    angle = (motion == Motion.Pound ? Mathf.Lerp(111f, -48f, sweep) :
                        Mathf.Lerp(62f, motion == Motion.Grab ? 3f : -28f, sweep)) * Mathf.Deg2Rad;
                }

                Vector2 reach = new(Mathf.Cos(angle), Mathf.Sin(angle));
                float extension = hands ? Mathf.Lerp(.57f, .97f, Ease(.23f, .73f, progress)) :
                    motion == Motion.Windup ? 0.52f : motion == Motion.Guard ? 0.68f : 0.93f;
                PointHand(2, 3, reach, extension, handBlend, -1);
                if ((hands && motion != Motion.Claw) || (armed && role == Role.Kyle))
                    PointHand(4, 5, reach, extension * 0.89f, handBlend, -1);
                if (holdingBlade)
                {
                    positions[Blade] = Endpoint(3);
                    SetDirection(Blade, angle - RestDirection(Blade));
                    return;
                }
            }
            if (armed && role == Role.Kyle)
            {
                positions[Blade] = positions[0] + Rotate(joints[Blade] - joints[0], cos[0], sin[0]);
                SetDirection(Blade, -0.065f);
            }
            else
            {
                positions[Blade] = Endpoint(3);
            }
        }

        private void PointHand(int upper, int lower, Vector2 direction, float extension, float blend, int hinge)
        {
            float length = ((tips[upper] - joints[upper]).magnitude + (tips[lower] - joints[lower]).magnitude) * extension;
            Vector2 target = Vector2.Lerp(Endpoint(lower), positions[upper] + direction * length, blend);
            SolveChain(upper, lower, target, hinge, 1f);
        }

        private void SolveChain(int upper, int lower, Vector2 destination, int hinge, float blend)
        {
            Vector2 root = positions[upper];
            Vector2 offset = Vector2.Lerp(Endpoint(lower), destination, blend) - root;
            float a = (tips[upper] - joints[upper]).magnitude;
            float b = (tips[lower] - joints[lower]).magnitude;
            float d = Mathf.Clamp(offset.magnitude, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
            Vector2 direction = offset.sqrMagnitude <= 0.00001f ? Vector2.down : offset.normalized;
            float directionAngle = Mathf.Atan2(direction.y, direction.x);
            float jointAngle = directionAngle + hinge * Mathf.Acos(Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f));
            SetDirection(upper, jointAngle - RestDirection(upper));
            positions[lower] = root + new Vector2(Mathf.Cos(jointAngle), Mathf.Sin(jointAngle)) * a;
            Vector2 end = root + direction * d - positions[lower];
            SetDirection(lower, Mathf.Atan2(end.y, end.x) - RestDirection(lower));
        }

        private float RestDirection(int bone) =>
            Mathf.Atan2(tips[bone].y - joints[bone].y, tips[bone].x - joints[bone].x);

        private Vector2 Endpoint(int bone) =>
            positions[bone] + Rotate(tips[bone] - joints[bone], cos[bone], sin[bone]);

        private void SetDirection(int bone, float angle)
        {
            cos[bone] = Mathf.Cos(angle);
            sin[bone] = Mathf.Sin(angle);
        }
    }
}
