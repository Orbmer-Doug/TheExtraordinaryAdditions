using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class LivingStarFlareMinion : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public NPC Target => NPCTargeting.MinionHoming(new(Projectile.Center, 1050, false, true), Owner);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 12000;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.minionSlots = 1f;
        Projectile.penetrate = -1;
        Projectile.width =
        Projectile.height = 32;
        Projectile.scale = 0;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.netImportant = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.minion = true;
    }

    public ref float OrbitOffsetAngle => ref Projectile.ai[0];
    public ref float OrbitSquish => ref Projectile.ai[1];
    public ref float OrbitRadius => ref Projectile.ai[2];
    public int Time
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }

    public override void AI()
    {
        if (!Owner.Available() && this.RunLocal())
        {
            Modded.Flare = false;
            return;
        }
        Owner.AddBuff(ModContent.BuffType<LittleStar>(), 3600);
        if (Modded.Flare)
            Projectile.timeLeft = 2;

        if (Time == 0 && this.RunLocal())
        {
            Projectile.AI_GetMyGroupIndex(out int index, out int group);
            float indexInterpol = InverseLerp(0f, group, index);
            float radiusInterpolant = indexInterpol * 0.85f + MathF.Sqrt(Main.rand.NextFloat()) * 0.15f;
            OrbitOffsetAngle = MathHelper.TwoPi * index / (.01f * group);
            OrbitRadius = MathHelper.Lerp(120f, 120f * MathF.Max((index % 6), 1), radiusInterpolant);
            OrbitSquish = Main.rand.NextFloat(0.45f, 1f);
            this.Sync();
        }

        Vector2 orbitDestination = Owner.Center + OrbitOffsetAngle.ToRotationVector2() * OrbitRadius * new Vector2(1f, OrbitSquish);
        Projectile.SmoothFlyNear(orbitDestination, Projectile.Opacity * 0.04f, 1f - Projectile.Opacity * 0.13f);
        OrbitOffsetAngle += MathHelper.TwoPi / OrbitRadius * InverseLerp(0f, 90f, Time);

        Projectile.scale = InverseLerp(0f, 50f, Time);
        Projectile.Opacity = Projectile.scale;

        if (Target != null)
        {
            int wait = LivingStarBeam.BeamTime + LivingStarBeam.FadeTime + 90;
            if (Time % wait == (wait - 1))
            {
                AdditionsSound.HeavyLaserBlast.Play(Projectile.Center, .6f, -.1f, 0f, 20);
                if (this.RunLocal())
                    Projectile.NewProj(Projectile.Center, Projectile.SafeDirectionTo(Target.Center),
                        ModContent.ProjectileType<LivingStarBeam>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, Projectile.whoAmI, Target.whoAmI);
            }
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.FlameMap1);
        ManagedShader fireball = ShaderRegistry.FireballShader;
        fireball.SetTexture(noise, 1, SamplerState.AnisotropicWrap);
        fireball.TrySetParameter("mainColor", Color.Lerp(Color.Goldenrod, Color.Gold, 0.3f).ToVector3());
        fireball.TrySetParameter("resolution", new Vector2(Projectile.scale * 100f));
        fireball.TrySetParameter("time", Main.GlobalTimeWrappedHourly * (0.04f + 0.32f));
        fireball.TrySetParameter("opacity", Projectile.Opacity);

        Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, fireball.Effect);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        Texture2D invis = AssetRegistry.GetTexture(AdditionsTexture.Invisible);
        fireball.Render();
        Main.spriteBatch.Draw(invis, drawPos, null, Color.White * Projectile.Opacity, 0f, invis.Size() * 0.5f, Projectile.scale * 100f, SpriteEffects.None, 0f);
        Main.spriteBatch.ExitShaderRegion();
        return false;
    }
}

public class LivingStarBeam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
    }

    public enum BeamState
    {
        Vaporizing,
        Fading,
    }

    public static readonly int BeamTime = 26;
    public static readonly int FadeTime = 30;
    public const int MaxLength = 1400;

    public int OwnerIndex
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int TargetIndex
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public int Time
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public BeamState CurrentState
    {
        get => (BeamState)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = (int)value;
    }
    public Vector2 TargetOffset
    {
        get => new Vector2(Projectile.AdditionsInfo().ExtraAI[1], Projectile.AdditionsInfo().ExtraAI[2]);
        set
        {
            Projectile.AdditionsInfo().ExtraAI[1] = value.X;
            Projectile.AdditionsInfo().ExtraAI[2] = value.Y;
        }
    }
    public bool Init
    {
        get => Projectile.AdditionsInfo().ExtraAI[3] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[3] = value.ToInt();
    }

    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 80);

        Projectile owner = Main.projectile?[OwnerIndex] ?? null;
        if (owner != null && owner.active)
            Projectile.Center = owner.Center;
        else
        {
            Projectile.Kill();
            return;
        }
        NPC target = Main.npc?[TargetIndex] ?? null;

        if (!Init)
        {
            TargetOffset = new Vector2(Main.rand.NextFloat(0f, target.width), Main.rand.NextFloat(0f, target.height));
            Init = true;
        }

        if (target != null && target.active)
        {
            float comp = InverseLerp(0f, 40f, Time);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.position + TargetOffset), comp);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        switch (CurrentState)
        {
            case BeamState.Vaporizing:
                float vaporComp = InverseLerp(0f, BeamTime, Time);

                Projectile.scale = Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 20f, Time));

                if (vaporComp >= 1f)
                {
                    Time = 0;
                    CurrentState = BeamState.Fading;
                    this.Sync();
                }
                break;
            case BeamState.Fading:
                float fadeComp = InverseLerp(0f, FadeTime, Time);
                Projectile.scale = Animators.Sine.InOutFunction.Evaluate(1f, 0f, fadeComp);

                if (fadeComp >= 1f)
                {
                    Projectile.Kill();
                    return;
                }
                break;
        }
        points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxLength, 80));

        Time++;
    }

    public override bool? CanDamage() => CurrentState == BeamState.Vaporizing;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c)
    {
        return MathHelper.Lerp(Projectile.width * .65f, Projectile.width, c) * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return MulticolorLerp(c.X, Color.Goldenrod, Color.Orange, Color.DarkOrange);
    }

    public TrailPoints points = new(80);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (points == null || trail == null)
                return;

            ManagedShader shader = AssetRegistry.GetShader("DisintegrationBeamShader");
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1, SamplerState.AnisotropicWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes), 2, SamplerState.AnisotropicWrap);
            trail.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}