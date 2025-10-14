using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;

public class StellarKunaiProj : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StellarKunai);

    public Player Owner => Main.player[Projectile.owner];

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(offset);
    public override void ReceiveExtraAI(BinaryReader reader) => offset = reader.ReadVector2();
    
    public const int ChargeupTime = 30;
    public const int Lifetime = 200;
    public float ChargeProgress => InverseLerp(0f, ChargeupTime, Timer);

    public bool Returning
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float EnemyID => ref Projectile.ai[1];
    public bool Stuck
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float Timer => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float ReturningTimer => ref Projectile.AdditionsInfo().ExtraAI[2];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    private const int RealWidth = 48;
    public override void SetDefaults()
    {
        Projectile.width = 18;
        Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = Lifetime + ChargeupTime;
        Projectile.aiStyle = -1;

        Projectile.DamageType = DamageClass.SummonMeleeSpeed;
        Projectile.ignoreWater = true;
    }

    public override bool ShouldUpdatePosition() => ChargeProgress >= 1f;
    public override bool? CanDamage() => ChargeProgress >= 1f ? null : false;

    public float ArmAnticipationMovement()
    {
        return Projectile.velocity.ToRotation() + (MathHelper.Pi * .7f * new PiecewiseCurve()
            .Add(0f, -1f, .4f, Sine.OutFunction)
            .Add(-1f, .15f, 1f, MakePoly(4).InFunction)
            .Evaluate(ChargeProgress) * Projectile.velocity.X.NonZeroSign());
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (Projectile.localAI[0] == 0f)
        {
            Returning = false;
            EnemyID = -1;
            this.Sync();
            Projectile.localAI[0] = 1f;
        }

        Timer++;

        // Reel back and throw the kunai
        if (ChargeProgress < 1f)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Owner.Additions().mouseWorld), (1f - ChargeProgress) * .5f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
            float rotation = ArmAnticipationMovement();
            Projectile.Center = center + PolarVector(40f, rotation);
            Projectile.rotation = rotation + MathHelper.PiOver4;
            Owner.SetCompositeArmFront(true, 0, rotation - MathHelper.PiOver2);
            return;
        }
        if (Timer == ChargeupTime)
        {
            SoundID.Item1.Play(Projectile.Center, Main.rand.NextFloat(.9f, 1.2f), 0f, .1f, null, 1, Name);
            Projectile.velocity *= 13.5f;
            Projectile.tileCollide = true;
        }

        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

        if (Owner.Additions().SafeMouseRight.JustPressed && this.RunLocal())
        {
            Returning = true;
            this.Sync();
        }
        if (Returning)
        {
            Vector2 handPos = Owner.GetFrontHandPositionImproved();
            float jitter = 10f * (1f - ReelInterpolant);
            Vector2 backPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * (RealWidth * .3f) + Main.rand.NextVector2Circular(jitter, jitter);

            line.SetPoints(handPos.GetLaserControlPoints(backPos, 30));

            // Hold hand out to projectile, cause reeling
            float direction = Owner.AngleTo(Projectile.Center) - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, 0, direction);

            // Face the projectile
            int dir = Owner.direction = (!(Projectile.Center.X < Owner.Center.X)) ? 1 : (-1);
            Owner.ChangeDir(dir);

            if (ReturningTimer == ChargeupTime)
            {
                Projectile.penetrate++;
                Projectile.friendly = true;
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = .8f, PitchVariance = .1f }, Projectile.Center);
            }
            if (ReturningTimer > ChargeupTime)
            {
                Stuck = false;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * 20f, .8f);
                Projectile.extraUpdates = 5;
                if (Projectile.Hitbox.Intersects(Owner.Hitbox))
                {
                    Projectile.Kill();
                }
                this.Sync();
            }
            ReturningTimer++;
        }

        if (Stuck)
        {
            // Stick to the target
            NPC target = Main.npc[(int)MathHelper.Clamp((int)EnemyID, 0, Main.maxNPCs)];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 120;
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
        }

        if (!Stuck)
        {
            if (Projectile.velocity.Length() > 5f)
                Projectile.velocity *= .985f;

            // Create a side trail while out
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * RealWidth;
                Vector2 vel = -Projectile.velocity.RotatedBy(.45f * i) * Main.rand.NextFloat(.4f, .7f);

                ParticleRegistry.SpawnSparkParticle(pos, vel, 12, Main.rand.NextFloat(.1f, .3f), Color.BlueViolet);
                ParticleRegistry.SpawnSparkleParticle(pos, vel, 20, Main.rand.NextFloat(.4f, .6f), Color.Blue, Color.Cyan, 1.1f);
            }

            // Play some flying sounds
            if (Projectile.soundDelay <= 0)
            {
                SoundEngine.PlaySound(SoundID.Item7, (Vector2?)Projectile.Center, null);
                Projectile.soundDelay = 8;
            }

            Projectile.Opacity = InverseLerp(0f, 30f, Projectile.timeLeft);

            // Set the rotation
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Lead knockback the other way when returning
        if (Stuck)
            modifiers.HitDirectionOverride = -modifiers.HitDirection;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Target a striked npc
        Owner.MinionAttackTargetNPC = target.whoAmI;
        target.AddBuff(ModContent.BuffType<KunaiTag>(), 120);

        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * RealWidth * .5f;
        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, Main.rand.Next(15, 17), Main.rand.NextFloat(2.2f, 2.5f), Color.White, Color.FloralWhite, 1f, .1f);
        for (int i = 0; i < Main.rand.Next(6, 12); i++)
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(.35f) * Main.rand.NextFloat(.8f, 1.4f) * (Stuck ? -1 : 1);
            ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(45, 60), Main.rand.NextFloat(.4f, .6f), Color.AntiqueWhite);
        }

        // Set the sticking variables
        if (Stuck == false && target.life > 0)
        {
            Projectile.penetrate++;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            Stuck = true;
            this.Sync();
        }

        // Small penalty
        Projectile.damage = (int)(Projectile.damage * .9f);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = Projectile.oldVelocity.Length() * 0.3f * Utils.SafeNormalize(Owner.MountedCenter - Projectile.Center, Vector2.One);
        Returning = true;
        Projectile.tileCollide = false;
        return false;
    }

    private float ReelInterpolant => InverseLerp(0f, ChargeupTime, ReturningTimer);

    private float WidthFunction(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - MathF.Pow(Utils.GetLerpValue(0.2f, 0f, c, true), 2f));
        float width = Utils.GetLerpValue(1f, 0.4f, c, true) * tipInterpolant * Projectile.scale;
        return width * RealWidth * .5f * ReelInterpolant * 1.8f;
    }

    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        Color col = new Color(8, 10 + (int)(95 * c.X), 145) * c.X;
        return col * ReelInterpolant;
    }

    public TrailPoints line = new(30);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || line == null)
                return;

            if (Returning == true)
            {
                ManagedShader prim = ShaderRegistry.EnlightenedBeam;
                prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Streak), 1, SamplerState.LinearWrap);
                prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.BigWavyBlobNoise), 2, SamplerState.LinearWrap);
                trail.DrawTrail(prim, line.Points, 100);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }
}