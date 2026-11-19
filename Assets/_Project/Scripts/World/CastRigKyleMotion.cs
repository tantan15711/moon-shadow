using UnityEngine;

namespace MoonShadow
{
    public sealed partial class CastRig
    {
        // X and Y are fractions of Kyle's standing height. The airborne keys keep
        // the leading knee and the trailing ankle on independent paths.
        private readonly struct AirKey
        {
            public readonly float At, Lean, NearPitch, FarPitch;
            public readonly Vector2 Near, Far;

            public AirKey(float at, float lean, Vector2 near, Vector2 far, float nearPitch, float farPitch)
            {
                At = at;
                Lean = lean;
                Near = near;
                Far = far;
                NearPitch = nearPitch;
                FarPitch = farPitch;
            }

            public static AirKey Mix(AirKey a, AirKey b, float t) => new(
                Mathf.Lerp(a.At, b.At, t), Mathf.Lerp(a.Lean, b.Lean, t),
                Vector2.Lerp(a.Near, b.Near, t), Vector2.Lerp(a.Far, b.Far, t),
                Mathf.Lerp(a.NearPitch, b.NearPitch, t), Mathf.Lerp(a.FarPitch, b.FarPitch, t));
        }

        private static readonly AirKey[] JumpKeys =
        {
            new(0.00f, -19f, new(.17f, .145f), new(-.18f, .071f), 14f, -33f),
            new(0.24f, -14f, new(.20f, .33f), new(-.13f, .19f), 16f, -24f),
            new(0.48f,  -3f, new(.14f, .29f), new(-.18f, .22f),  4f, -16f),
            new(0.72f,   4f, new(.13f, .19f), new(-.13f, .15f), -5f, -15f),
            new(1.00f, -10f, new(.12f, .094f), new(-.07f, .093f), 9f, -6f)
        };

        private static readonly AirKey[] DashKeys =
        {
            new(0.00f, -15f, new(.15f, .052f), new(-.16f, .063f),  7f, -17f),
            new(0.26f, -43f, new(.15f, .12f), new(-.26f, .075f), -8f, -41f),
            new(0.63f, -37f, new(.11f, .17f), new(-.24f, .14f), -9f, -27f),
            new(1.00f, -19f, new(.23f, .09f), new(-.13f, .15f),  7f, -21f)
        };

        private float lastVisualX, gaitWeight, bodyPace, torsoLean, bladeWeight, bladeHeading;
        private Vector2 hipOffset;
        private readonly Vector2[] wrists = new Vector2[2];
        private int lastFacing;
        private bool sampledKyle;

        private void MoveKyle(float dt, Vector2 velocity, int facing, Motion motion, float progress, float floor,
            LayerMask terrain, float bladeAngle, Vector2 aim, float hang, float impact, float recoil)
        {
            var space = renderer.transform;
            float worldHeight = modelHeight * Mathf.Abs(transform.lossyScale.y);
            float speed = Mathf.Abs(velocity.x);
            bool airborne = motion is Motion.Air or Motion.Dive or Motion.Vault || (float.IsNaN(floor) && !float.IsNaN(hang));
            bool supported = !airborne && !float.IsNaN(floor);
            bool stride = supported && (motion is Motion.Travel or Motion.Aim or Motion.Reload or Motion.Land);
            float blend = dt <= 0f || !sampledKyle ? 1f : 1f - Mathf.Exp(-dt * 24f);
            float travelBlend = dt <= 0f || !sampledKyle ? 1f : 1f - Mathf.Exp(-dt * 13f);
            if (sampledKyle && facing != lastFacing)
                ResetContacts();
            float displacement = sampledKyle ? Mathf.Abs(space.position.x - lastVisualX) : 0f;
            displacement = Mathf.Min(displacement, speed * dt * 1.36f + worldHeight * 0.009f);
            lastVisualX = space.position.x;
            lastFacing = facing;

            float pace = stride ? speed : 0f;
            float run = Ease(2.65f, 5.20f, pace);
            float strideLength = Mathf.Lerp(.87f, 1.32f, run);
            gaitWeight = Mathf.Lerp(gaitWeight, Mathf.InverseLerp(0.12f, 1.75f, pace), travelBlend);
            if (stride && speed > 0.12f)
                phase = Mathf.Repeat(phase + displacement / (worldHeight * strideLength), 1f);

            float forward = speed < 0.12f ? 1f : Mathf.Sign(velocity.x) * facing;
            float strideAngle = phase * 2f * Mathf.PI;
            float step = Mathf.Cos(strideAngle) * gaitWeight * forward;
            float shift = (pace - bodyPace) * forward;
            bodyPace = Mathf.Lerp(bodyPace, pace, travelBlend);
            float breath = Mathf.Sin(clock * 3.3f);
            float swing = Mathf.Lerp(0.068f, 0.137f, run);
            float wristHeight = Mathf.Lerp(-0.240f, -0.149f, run);
            float armForward = Mathf.Lerp(0.012f, 0.06f, run);
            Vector2 neutralNear = (joints[NearHand] - joints[2]) / modelHeight;
            Vector2 neutralFar = new(.069f, -.241f);
            Vector2 near = Vector2.Lerp(neutralNear, new Vector2(armForward - step * swing,
                wristHeight - step * 0.029f), gaitWeight);
            Vector2 far = Vector2.Lerp(neutralFar, new Vector2(armForward + step * swing,
                wristHeight + step * 0.029f), gaitWeight);

            float load = (1f - Mathf.Cos(strideAngle * 2f - 0.52f)) * 0.5f;
            Vector2 hips = new(gaitWeight * (0.018f + run * 0.043f) * forward + Mathf.Clamp(shift * 0.008f, -0.036f, 0.039f),
                gaitWeight * (-0.038f - run * 0.054f - load * Mathf.Lerp(.017f, .028f, run)) + breath * 0.004f);
            float lean = gaitWeight * (-3f - run * 15f) * forward - Mathf.Clamp(shift * 2.1f, -10f, 10f);
            float head = -lean * 0.37f + breath * 1.1f;
            AirKey air = SampleAir(JumpKeys, float.IsNaN(hang) ? progress : hang);
            if (airborne)
            {
                hips = new Vector2(0.029f * forward, -0.028f);
                lean = air.Lean * (0.58f + Mathf.InverseLerp(0f, 5f, speed) * 0.42f) * forward;
                head = -lean * 0.29f;
                float falling = Ease(.42f, .96f, air.At);
                near = Vector2.Lerp(new Vector2(.105f, -.10f), new Vector2(.16f, -.17f), falling);
                far = Vector2.Lerp(new Vector2(-.107f, -.123f), new Vector2(-.14f, -.195f), falling);
            }

            bool attack = motion is Motion.Windup or Motion.Strike or Motion.FollowThrough or Motion.Dive;
            bool aiming = motion is Motion.Aim or Motion.Reload;
            float handAngle = float.IsNaN(bladeAngle) ? 0f : bladeAngle;
            switch (motion)
            {
                case Motion.Dash:
                    air = SampleAir(DashKeys, progress);
                    hips = new Vector2(0.095f, airborne ? -0.058f : -0.17f);
                    lean = air.Lean;
                    head = -lean * 0.40f;
                    near = new Vector2(-.135f, -.175f);
                    far = new Vector2(.107f, -.067f);
                    break;
                case Motion.Land:
                    float absorb = Ease(0f, .22f, progress) * (1f - Ease(.22f, 1f, progress)) * impact;
                    hips += new Vector2(.039f, -.13f) * absorb;
                    lean -= 23f * absorb;
                    near += new Vector2(.08f, .081f) * absorb;
                    far += new Vector2(-.018f, .10f) * absorb;
                    break;
                case Motion.Windup:
                    hips += new Vector2(-.047f, -.07f);
                    lean = handAngle < 0f ? -20f : 13f;
                    far = new Vector2(.08f, -.12f);
                    break;
                case Motion.Strike:
                case Motion.FollowThrough:
                    float follow = motion == Motion.FollowThrough ? Ease(.10f, .99f, progress) : 0f;
                    float rising = Mathf.Sin(handAngle * Mathf.Deg2Rad);
                    hips += new Vector2(.106f, -.085f) * (1f - follow);
                    lean = Mathf.Lerp(-22f - Mathf.Max(0f, -rising) * 14f, -4f, follow);
                    head = -lean * 0.44f;
                    far = Vector2.Lerp(new Vector2(-.13f, -.10f), neutralFar, follow);
                    break;
                case Motion.Dive:
                    lean = -23f;
                    hips.y -= .035f;
                    air = new AirKey(0f, lean, new(.08f, .20f), new(-.13f, .27f), -31f, -46f);
                    far = new Vector2(-.10f, -.087f);
                    break;
                case Motion.Guard:
                    hips += new Vector2(-.036f, -.072f);
                    lean = 12f;
                    far = new Vector2(.14f, -.075f);
                    break;
                case Motion.Aim:
                    hips += new Vector2(-.018f - recoil * .022f, -.025f - recoil * .018f);
                    lean = Mathf.Lerp(lean, -7f + recoil * 18f, .75f);
                    break;
                case Motion.Reload:
                    hips += new Vector2(-.02f, -.03f);
                    lean = Mathf.Lerp(lean, -10f, .8f);
                    head = -15f * Mathf.Sin(progress * Mathf.PI);
                    break;
                case Motion.Stun:
                case Motion.Recover:
                    float wince = motion == Motion.Stun ? 1f : 1f - Ease(0f, 1f, progress);
                    hips += new Vector2(-.061f, -.092f) * wince;
                    lean += 27f * wince;
                    near = Vector2.Lerp(near, new Vector2(-.161f, -.123f), wince);
                    far = Vector2.Lerp(far, new Vector2(.089f, -.094f), wince);
                    break;
                case Motion.Reach:
                case Motion.Assist:
                    float reach = motion == Motion.Assist ? Mathf.Sin(progress * Mathf.PI) : progress;
                    hips += new Vector2(.048f, -.135f) * reach;
                    lean -= 29f * reach;
                    near = Vector2.Lerp(near, new Vector2(.156f, -.151f), reach);
                    far = Vector2.Lerp(far, new Vector2(.139f, -.103f), reach);
                    head += 4f * reach;
                    break;
                case Motion.Gesture:
                case Motion.Listen:
                    float gesture = Mathf.Sin(progress * Mathf.PI);
                    lean += 4f * gesture;
                    head += (motion == Motion.Gesture ? -8f : 5f) * gesture;
                    if (motion == Motion.Gesture)
                        far = Vector2.Lerp(far, new Vector2(.153f, -.13f), gesture * .84f);
                    break;
            }

            hipOffset = Vector2.Lerp(hipOffset, hips * modelHeight, blend);
            torsoLean = Mathf.Lerp(torsoLean, lean, blend);
            positions[0] = joints[0] + hipOffset;
            SetDirection(0, torsoLean * Mathf.Deg2Rad);
            positions[1] = KyleJoint(1);
            SetDirection(1, (torsoLean + head) * Mathf.Deg2Rad);
            positions[2] = KyleJoint(2);
            positions[4] = KyleJoint(4);
            positions[6] = KyleJoint(6);
            positions[8] = KyleJoint(8);

            if (airborne)
            {
                ResetContacts();
                AirKyleLeg(6, NearFoot, air.Near, air.NearPitch, blend);
                AirKyleLeg(8, FarFoot, air.Far, air.FarPitch, blend);
            }
            else
            {
                GroundKyleFeet(motion, air, progress, strideLength, run, forward, worldHeight, floor, terrain);
            }

            float nearHandAngle = -94f + step * 23f;
            float farHandAngle = -88f - step * 18f;
            if (aiming)
            {
                float angle = Mathf.Atan2(aim.y, aim.x * facing) * Mathf.Rad2Deg;
                float reload = motion == Motion.Reload ? Mathf.Sin(progress * Mathf.PI) : 0f;
                angle -= reload * 41f;
                Vector2 line = Unit(angle);
                Vector2 brace = new(-line.y, line.x);
                near = line * (.237f - recoil * .028f - reload * .087f) + brace * (-.019f - reload * .012f);
                Vector2 support = positions[2] / modelHeight + near + line * (.037f + reload * .086f) - brace * .035f;
                far = support - positions[4] / modelHeight;
                nearHandAngle = angle + recoil * 11f - 13f;
                farHandAngle = angle - 33f + reload * 31f;
            }
            else if (attack || motion == Motion.Guard)
            {
                float follow = motion == Motion.FollowThrough ? Ease(.08f, .96f, progress) : 0f;
                float guard = motion == Motion.Guard || motion == Motion.Windup ? 1f : 0f;
                Vector2 cut = Unit(handAngle);
                near = Vector2.Lerp(cut * (.244f - guard * .102f) + new Vector2(.035f, -.042f), neutralNear, follow * .72f);
                nearHandAngle = Mathf.LerpAngle(handAngle - 15f, -94f, follow);
            }

            float armBlend = attack ? Mathf.Lerp(blend, 1f, .61f) : Mathf.Lerp(blend, 1f, aiming ? .40f : .19f);
            KyleArm(0, 2, NearHand, near * modelHeight, nearHandAngle, armBlend);
            KyleArm(1, 4, FarHand, far * modelHeight, farHandAngle, armBlend);

            bool drawn = attack || motion == Motion.Guard || (!aiming && !airborne && motion != Motion.Dash && run < .28f);
            bladeWeight = Mathf.Lerp(bladeWeight, drawn ? 1f : 0f, attack ? 1f : blend);
            Vector2 carry = positions[0] + Rotate(new Vector2(-.137f, -.036f) * modelHeight, cos[0], sin[0]);
            positions[Blade] = Vector2.Lerp(carry, KyleGrip(), bladeWeight);
            float sword = attack || motion == Motion.Guard ? handAngle : Mathf.Lerp(-159f, -128f + step * 9f, bladeWeight);
            bladeHeading = sampledKyle ? Mathf.LerpAngle(bladeHeading, sword, attack ? armBlend : blend) : sword;
            SetDirection(Blade, bladeHeading * Mathf.Deg2Rad - RestDirection(Blade));
            sampledKyle = true;
        }

        private void GroundKyleFeet(Motion motion, AirKey pose, float progress, float strideLength, float run, float forward,
            float worldHeight, float floor, LayerMask terrain)
        {
            float stanceEnd = Mathf.Lerp(.59f, .46f, run);
            for (int i = 0; i < 2; i++)
            {
                int thigh = i == 0 ? 6 : 8;
                int boot = i == 0 ? NearFoot : FarFoot;
                float footPhase = Mathf.Repeat(phase + i * .5f, 1f);
                float swing = Mathf.InverseLerp(stanceEnd, 1f, footPhase);
                float stance = Mathf.Clamp01(footPhase / stanceEnd);
                float reach = strideLength * stanceEnd * .5f;
                float center = joints[thigh].x / modelHeight + .028f * run;
                float x, lift, pitch;
                bool step = gaitWeight > .16f && (motion is Motion.Travel or Motion.Aim or Motion.Reload or Motion.Land);
                bool planted = !step || swing <= 0f;
                if (step)
                {
                    float push = Ease(.67f, 1f, stance);
                    float roll = Mathf.Lerp(16f, 10f, run) * (1f - Ease(0f, .22f, stance)) - push * Mathf.Lerp(29f, 42f, run);
                    float passing = Ease(.13f, .81f, swing);
                    x = Mathf.Lerp(reach - stance * 2f * reach, -reach + passing * 2f * reach, swing > 0f ? 1f : 0f);
                    x = Mathf.Lerp(joints[boot].x / modelHeight, center + x * forward, gaitWeight);
                    lift = Mathf.Sin(Mathf.Pow(swing, .70f) * Mathf.PI) * Mathf.Lerp(.088f, .226f, run) * gaitWeight;
                    pitch = swing <= 0f ? roll : Mathf.Lerp(-41f, 14f, Ease(.21f, .82f, swing));
                    pitch *= gaitWeight * forward;
                }
                else
                {
                    float brace = motion is Motion.Strike or Motion.Windup or Motion.FollowThrough or Motion.Guard ? .075f :
                        motion == Motion.Land ? .048f * Mathf.Sin(progress * Mathf.PI) : 0f;
                    x = joints[boot].x / modelHeight + (i == 0 ? brace : -brace) + hipOffset.x / modelHeight * .35f;
                    lift = pitch = 0f;
                    if (motion == Motion.Dash)
                    {
                        Vector2 ankle = i == 0 ? pose.Near : pose.Far;
                        x = ankle.x;
                        lift = Mathf.Max(0f, ankle.y - (i == 0 ? .087f : .079f));
                        pitch = i == 0 ? pose.NearPitch : pose.FarPitch;
                        planted = i == 0 && (progress < .27f || progress > .71f);
                        if (planted)
                            lift = 0f;
                    }
                }

                var space = renderer.transform;
                Vector2 location = space.TransformPoint(Vector2.right * (x * modelHeight));
                Vector2 point = new(location.x, float.IsNaN(floor) ? location.y : floor);
                if (terrain.value != 0 && !float.IsNaN(floor))
                {
                    var hit = Physics2D.Raycast(point + Vector2.up * .25f, Vector2.down, .62f, terrain);
                    bool support = hit && Mathf.Abs(hit.point.y - floor) < .23f;
                    if (support)
                        point = hit.point;
                    ref Contact contact = ref contacts[i];
                    if (step && planted && contact.Planted && contact.Support)
                    {
                        var saved = (Vector2)contact.Support.transform.TransformPoint(contact.Point);
                        if (Mathf.Abs(saved.y - floor) < .24f && Mathf.Abs(saved.x - point.x) < worldHeight * .25f)
                            point = saved;
                    }
                    contact.Planted = planted && support;
                    contact.Support = contact.Planted ? hit.collider : null;
                    if (contact.Support)
                        contact.Point = contact.Support.transform.InverseTransformPoint(point);
                }
                else
                {
                    contacts[i] = default;
                }
                // Roll the rigid heel or toe around the contact point instead of
                // tipping the entire ankle through the ground.
                Vector2 pivot = i == 0 ? new(pitch < 0f ? .066f : -.041f, -.078f) :
                    new(pitch < 0f ? .109f : -.025f, -.071f);
                float radians = pitch * Mathf.Deg2Rad;
                Vector2 arc = Rotate(pivot, Mathf.Cos(radians), Mathf.Sin(radians));
                Vector2 target = (Vector2)space.InverseTransformPoint(point) +
                    new Vector2(pivot.x - arc.x, lift - arc.y) * modelHeight;
                KyleLeg(thigh, boot, target, pitch);
            }
        }

        private Vector2 KyleJoint(int joint) => positions[0] + Rotate(joints[joint] - joints[0], cos[0], sin[0]);

        private void AirKyleLeg(int thigh, int boot, Vector2 target, float pitch, float blend)
        {
            float previous = Mathf.Atan2(sin[boot], cos[boot]) * Mathf.Rad2Deg;
            KyleLeg(thigh, boot, Vector2.Lerp(positions[boot], target * modelHeight, blend),
                Mathf.LerpAngle(previous, pitch, blend));
        }

        private void KyleLeg(int thigh, int boot, Vector2 target, float pitch)
        {
            SolveChain(thigh, thigh + 1, target, 1, 1f);
            positions[boot] = Endpoint(thigh + 1);
            SetDirection(boot, pitch * Mathf.Deg2Rad);
        }

        private void KyleArm(int index, int shoulder, int hand, Vector2 offset, float angle, float blend)
        {
            wrists[index] = Vector2.Lerp(wrists[index], offset, blend);
            SolveChain(shoulder, shoulder + 1, positions[shoulder] + wrists[index], -1, 1f);
            positions[hand] = Endpoint(shoulder + 1);
            SetDirection(hand, angle * Mathf.Deg2Rad - RestDirection(hand));
        }

        private Vector2 KyleGrip() => Vector2.Lerp(positions[NearHand], Endpoint(NearHand), .73f);

        private static Vector2 Unit(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static float Ease(float start, float end, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));

        private static AirKey SampleAir(AirKey[] keys, float progress)
        {
            for (int i = 1; i < keys.Length; i++)
                if (progress <= keys[i].At)
                    return AirKey.Mix(keys[i - 1], keys[i], Ease(keys[i - 1].At, keys[i].At, progress));
            return keys[^1];
        }

        internal void ResetMotion()
        {
            ResetContacts();
            phase = 0f;
            gaitWeight = bodyPace = 0f;
            sampledKyle = false;
        }
    }
}
