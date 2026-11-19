using UnityEngine;

namespace MoonShadow
{
    public sealed partial class KyleController2D
    {
        private const float LandingDuration = 0.19f;
        private float airTime, fallSpeed, landTime, landForce;
        private float shotKick;
        private bool shotPending;

        private void LateUpdate()
        {
            if (!demo || !demo.Playing)
                return;

            Vector2 velocity = body.linearVelocity;
            float delta = Time.deltaTime;
            ObserveGround(delta, velocity.y);
            var motion = Dashing ? CastRig.Motion.Dash :
                combat.Attacking ? combat.VisualMotion :
                combat.Parrying ? CastRig.Motion.Guard :
                hurtTime > 0f ? CastRig.Motion.Stun :
                Reloading ? CastRig.Motion.Reload :
                shotPose > 0f ? CastRig.Motion.Aim :
                !grounded ? CastRig.Motion.Air : landTime > 0f ? CastRig.Motion.Land : CastRig.Motion.Travel;
            float progress = motion switch
            {
                CastRig.Motion.Dash => Mathf.InverseLerp(0.20f, 0f, dashTime),
                CastRig.Motion.Reload => ReloadProgress,
                CastRig.Motion.Land => 1f - landTime / LandingDuration,
                _ => combat.Attacking ? combat.VisualProgress : 0f
            };
            float hang = grounded ? float.NaN : velocity.y >= 0f
                ? Mathf.Lerp(0f, 0.49f, Mathf.InverseLerp(jumpSpeed, 0f, velocity.y))
                : Mathf.Lerp(0.52f, 1f, Mathf.InverseLerp(0f, -10f, velocity.y));
            float bladeAngle = combat.Attacking ? combat.VisualBladeAngle : combat.Parrying ? 71f : float.NaN;
            Color tint = combat.Parrying ? new Color(0.46f, 0.91f, 1f) :
                hurtTime > 0f && (int)(Time.time * 19f) % 2 == 0
                    ? new Color(1f, 0.44f, 0.52f, 0.68f)
                    : Color.white;
            Vector2 direction = Reloading ? Vector2.right * Facing : shotDirection;
            if (rig)
                rig.Drive(delta, velocity, Facing, motion, progress, tint,
                    grounded ? hull.bounds.min.y : float.NaN, terrain, bladeAngle, direction, hang, landForce, shotKick);
            SetWeaponPose(motion, direction);
            if (shotPending)
                EmitShot();
            shotKick = Mathf.MoveTowards(shotKick, 0f, delta * 13.3f);
        }

        private void ObserveGround(float dt, float verticalSpeed)
        {
            landTime = Mathf.Max(0f, landTime - dt);
            if (!grounded)
            {
                airTime += dt;
                fallSpeed = Mathf.Max(fallSpeed, -verticalSpeed);
                return;
            }

            if (airTime > 0.09f && fallSpeed > 2.8f && !Dashing && hurtTime <= 0f)
            {
                landTime = LandingDuration;
                landForce = Mathf.Lerp(0.51f, 1f, Mathf.InverseLerp(3f, 13f, fallSpeed));
                if (fallSpeed > 6f)
                {
                    var bounds = hull.bounds;
                    demo.Effects.LandingDust(new Vector2(bounds.center.x, bounds.min.y), Facing, landForce);
                }
            }
            airTime = fallSpeed = 0f;
        }

        private void SetWeaponPose(CastRig.Motion motion, Vector2 direction)
        {
            if (!equippedArt)
                return;
            equippedArt.enabled = (motion is CastRig.Motion.Aim or CastRig.Motion.Reload) && equippedArt.sprite;
            if (!equippedArt.enabled)
                return;

            float length = SidearmCatalog.Get(displayedWeapon).Length;
            float dip = Reloading ? Mathf.Sin(ReloadProgress * Mathf.PI) : 0f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + Facing * (shotKick * 8f - dip * 41f);
            Vector2 barrel = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            equippedArt.flipY = direction.x < -0.025f || (Mathf.Abs(direction.x) <= 0.025f && Facing < 0);
            equippedArt.transform.position = (rig && rig.Visible ? rig.Grip : WeaponPivot) + barrel * (length * 0.24f);
            equippedArt.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void EmitShot()
        {
            shotPending = false;
            var sidearm = displayedWeapon;
            float length = SidearmCatalog.Get(sidearm).Length;
            Vector2 muzzle = equippedArt && equippedArt.enabled
                ? equippedArt.transform.position + equippedArt.transform.right * (length * 0.48f)
                : WeaponPivot + shotDirection * (length * 0.92f);
            var cover = Physics2D.Linecast(Position + Vector2.up * 0.40f, muzzle, terrain);
            if (cover)
            {
                demo.Effects.WeaponSound(sidearm);
                demo.Effects.Burst(cover.point, new Color(1f, 0.72f, 0.41f), 5, 2.6f);
                return;
            }
            if (sidearm is Sidearm.Incinerator or Sidearm.Shotgun)
                demo.Effects.WideShot(muzzle, shotDirection, sidearm);
            else
                demo.Effects.Gunshot(muzzle, shotDirection, sidearm);
        }

        private void ResetPresentation()
        {
            airTime = fallSpeed = landTime = landForce = shotKick = 0f;
            shotPending = false;
            rig?.ResetMotion();
        }
    }
}
