using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class GaussBallisticWarheadHoldout : BaseIdleHoldoutProjectile
{
    public override int IntendedProjectileType => ModContent.ProjectileType<GaussBallisticWarheadHoldout>();
    public override int AssociatedItemID => ModContent.ItemType<GaussBallisticWarheadLauncher>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GaussBallisticWarheadLauncher);

    public NPC Target;
    public const int LockInTime = 35;
    public ref float Time => ref Projectile.ai[0];
    public ref float ChargeTime => ref Projectile.ai[1];
    public bool Maxxed
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float Recoil => ref Projectile.Additions().ExtraAI[0];
    public ref float ReticleRot => ref Projectile.Additions().ExtraAI[1];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void Defaults()
    {
        Projectile.width = 162;
        Projectile.height = 42;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public static readonly float ChargeNeeded = SecondsToFrames(5f);
    public float ChargeCompletion => InverseLerp(0f, ChargeNeeded, ChargeTime);
    public float ShootWait => Maxxed ? SecondsToFrames(2.5f) : SecondsToFrames(1.5f);
    public Vector2 Right => Projectile.Center + PolarVector(73f, Projectile.rotation) + PolarVector(10f * Projectile.direction * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
    public ref bool Lock => ref Owner.GetModPlayer<GaussGlobalPlayer>().Lock;
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse) * Projectile.Size.Length(), 0.2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.spriteDirection = Projectile.direction;

        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);

        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.15f, .02f), 0f, 30f);
        Projectile.Center = Center + PolarVector(58f - Recoil, Projectile.rotation) + PolarVector(8f * Projectile.direction * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

        Projectile.SetAnimation(4, 12);
        if ((this.RunLocal() && Modded.SafeMouseRight.Current) && ChargeTime < ChargeNeeded && !Maxxed)
        {
            Vector2 pos = Right + Main.rand.NextVector2CircularLimited(200f, 200f, .5f, 1f);
            Vector2 vel = Main.rand.NextVector2CircularEdge(9f, 9f);

            if (Main.rand.NextBool())
            {
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(90, 150), Main.rand.NextFloat(.5f, .9f),
                    Color.GreenYellow * 1.5f, Color.Yellow * 1.8f, Right, 1.5f, 5);
            }

            pos = Vector2.Lerp(Projectile.Center + PolarVector(65f, Projectile.rotation), Projectile.direction == -1 ? Projectile.BaseRotHitbox().BottomRight : Projectile.BaseRotHitbox().TopRight, Main.rand.NextFloat());
            vel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 12f) * ChargeCompletion;
            ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(30, 60),
                Main.rand.NextFloat(.7f, 1.5f) * ChargeCompletion, Color.Yellow * 2f);

            ChargeTime++;
            this.Sync();
        }
        else if (!Maxxed)
            ChargeTime = MathHelper.Clamp(ChargeTime - 3f, 0f, ChargeNeeded);

        if (ChargeTime == ChargeNeeded - 1f)
        {
            ParticleRegistry.SpawnPulseRingParticle(Right, Projectile.velocity.SafeNormalize(Vector2.Zero),
                50, Projectile.rotation, new(.4f, 1f), 0f, 210f, Color.GreenYellow * 1.4f, true);

            AdditionsSound.etherealSmallExplode.Play(Right);
        }
        if (ChargeTime >= ChargeNeeded)
        {
            Maxxed = true;
            this.Sync();
        }

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Time < ShootWait)
            Time++;
        else
            Time = MathHelper.SmoothStep(Time, 0f, .3f);

        if (!Target.CanHomeInto() && Lock)
        {
            Lock = false;
            this.Sync();
        }

        if (!Lock)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Modded.mouseWorld, 800), out NPC target))
                Target = target;
            ReticleRot = (ReticleRot + .05f) % MathHelper.TwoPi;
        }

        if (Lock)
        {
            ReticleRot = ReticleRot.SmoothAngleLerp(0f, .5f, .2f);
        }

        bool dont = false;
        if ((this.RunLocal() && Modded.SafeMouseMiddle.JustPressed) && Lock)
        {
            Lock = false;
            dont = true;
            this.Sync();
        }

        if (Target != null && Target.active && Target.TryGetGlobalNPC(out GaussGlobalNPC global))
        {
            global.BeingTargeted = true;
            ref int lockin = ref global.LockIn;
            if (lockin < LockInTime)
                lockin++;
            if (lockin == LockInTime - 1)
                AdditionsSound.LaserShift.Play(Target.Center, 1.6f, 0f, .1f);
            if (lockin >= LockInTime && Modded.SafeMouseMiddle.JustPressed && !Lock && dont == false && this.RunLocal())
            {
                Lock = true;
                this.Sync();
            }
        }

        if (Time >= ShootWait)
        {
            Owner.PickAmmo(Owner.HeldItem, out int num, out float speed,
                out int damage, out float knockback, out int usedItemAmmoId, false);

            if (this.RunLocal())
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero) * speed * (Maxxed ? 1.5f : 1f);
                int proj = ModContent.ProjectileType<GaussBallisticWarheadRocket>();
                Projectile rocket = Main.projectile[Projectile.NewProj(Right, vel, proj, damage * (Maxxed ? 2 : 1), knockback, Owner.whoAmI, 0f, Maxxed.ToInt())];
                rocket.Additions().ExtraAI[1] = Projectile.whoAmI;
            }
            for (int j = 0; j <= 30; j++)
            {
                Vector2 vel = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(6f, 19f) * (Maxxed ? 1.8f : 1f);

                Color random = Main.rand.Next(4) switch
                {
                    0 => Color.Yellow * 1.6f,
                    1 => Color.YellowGreen,
                    2 => Color.LimeGreen,
                    _ => Color.Yellow * 1.8f,
                };

                int life = Main.rand.Next(20, 30);
                float size = Main.rand.NextFloat(.8f, 1.3f);

                ParticleRegistry.SpawnSquishyLightParticle(Right, vel, life * 2, size, random, 1.2f, 1.6f, 4f);
                ParticleRegistry.SpawnSparkParticle(Right, vel * 2, life, size * 1.3f, Color.AntiqueWhite, true);
                ParticleRegistry.SpawnMistParticle(Right, vel * 2, Main.rand.NextFloat(.3f, 1f), random * 1.5f, Color.Transparent, Main.rand.NextByte(150, 240));
                if (Maxxed)
                    ParticleRegistry.SpawnGlowParticle(Right, vel, life * 2, size, random, .9f);
            }
            AdditionsSound.LargeWeaponFire.Play(Projectile.Center, Maxxed ? 2.4f : 1.5f, Maxxed ? -.45f : -.3f, .05f);

            Time = ChargeTime = 0f;
            Recoil = 30f;

            Maxxed = false;
            this.Sync();
        }
    }

    public ref float AuraRot => ref Projectile.localAI[1];
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
        Texture2D reticle = AssetRegistry.GetTexture(AdditionsTexture.GaussReticle);
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        Vector2 position = Projectile.Center;
        float rotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
        Vector2 origin = frame.Size() * 0.5f;
        SpriteEffects effects = Projectile.direction.ToSpriteDirection();
        float shake = Utils.Remap(Time, 0f, ShootWait, 0f, Maxxed ? 11f : 8f, true);
        position += Main.rand.NextVector2Circular(shake, shake);
        Color col = Projectile.GetAlpha(lightColor);

        Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
        Vector2 drawStartOuter = offsets + Projectile.Center;
        Vector2 spinPoint = -Vector2.UnitY * 10f * (1f - InverseLerp(0f, ShootWait, Time)) * ChargeCompletion;
        AuraRot = (AuraRot + .03f) % MathHelper.TwoPi;
        float opacity = .7f * ChargeCompletion;
        for (int i = 0; i < 6; i++)
        {
            Vector2 spinStart = drawStartOuter + spinPoint.RotatedBy((double)(AuraRot - MathHelper.Pi * i / 3f), default);
            Color glowAlpha = Color.Yellow with { A = 0 } * 1.8f * ChargeCompletion;
            Main.spriteBatch.Draw(tex, spinStart, frame, glowAlpha * opacity,
                rotation, origin, Projectile.scale * 1.14f, effects, 0f);
        }

        Main.spriteBatch.DrawBetter(tex, position, frame, col, rotation, origin, Projectile.scale, effects);

        RotatedRectangle hitRot = Projectile.BaseRotHitbox();
        Vector2 pos = hitRot.Right - Projectile.rotation.ToRotationVector2() * 8f
            - (Projectile.rotation + MathHelper.PiOver2 * -Projectile.direction).ToRotationVector2() * -10f - Main.screenPosition;

        if (Maxxed)
        {
            void shine()
            {
                float completion = 1.8f * InverseLerp(0f, 40f, Time);
                Vector2 lensScale = new Vector2(.6f, 1f) * (1f - InverseLerp(0f, ShootWait, Time));
                Main.spriteBatch.Draw(star, pos, null, Color.Yellow * completion * .8f, Projectile.rotation, star.Size() / 2, lensScale, 0, 0f);
                Main.spriteBatch.Draw(star, pos, null, Color.White * completion, Projectile.rotation, star.Size() / 2, lensScale * .3f, 0, 0f);
            }
            PixelationSystem.QueueTextureRenderAction(shine, PixelationLayer.OverPlayers, BlendState.Additive);
        }

        if (this.RunLocal())
        {
            void draw()
            {
                if (Target != null && Target.active && Target.TryGetGlobalNPC(out GaussGlobalNPC npc))
                {
                    float completion = InverseLerp(0f, LockInTime, npc.LockIn);
                    Main.spriteBatch.Draw(reticle, Target.Center - Main.screenPosition, null, Color.YellowGreen * 1.5f * completion, ReticleRot, reticle.Size() / 2, Projectile.scale * completion + (MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * .05f + .1f), 0, 0f);
                }
            }
            LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.OverNPCs);
        }
        return false;
    }
}

public class GaussGlobalPlayer : ModPlayer
{
    public bool Lock;
}

public class GaussGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool BeingTargeted;
    public int LockIn;
    public override void ResetEffects(NPC npc)
    {
        if (!BeingTargeted)
            LockIn = 0;
        if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<GaussBallisticWarheadHoldout>()))
        {
            if (p.As<GaussBallisticWarheadHoldout>().Target != npc)
                BeingTargeted = false;
        }
    }
}