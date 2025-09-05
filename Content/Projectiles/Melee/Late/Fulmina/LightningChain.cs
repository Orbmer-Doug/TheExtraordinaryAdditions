using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Fulmina;

public class LightningChain : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int Life = 30;
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];
    public bool NotPrimary
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public int Current
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }

    public int MaxChains => (int)MathF.Ceiling(Utils.Remap(Power, CondereFulminaHoldout.TotalReelTime / 2, CondereFulminaHoldout.TotalReelTime, 2, 8));
    public float Width => Utils.Remap(Power, CondereFulminaHoldout.TotalReelTime / 2, CondereFulminaHoldout.TotalReelTime, 8f, 50f);
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public float Completion => Animators.MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));
    public override bool ShouldUpdatePosition() => false;

    public List<NPC> PreviousNPCs = [null];
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            if (!NotPrimary)
            {
                Start = Projectile.Center;
                NPC close = NPCTargeting.GetClosestNPC(new(Start, 1000, true));
                if (!close.CanHomeInto())
                {
                    Projectile.Kill();
                    return;
                }
                End = close.RandAreaInEntity();
                for (float i = .2f; i < 1f; i += .1f)
                    ParticleRegistry.SpawnGlowParticle(End, Vector2.Zero, 30, Width * i, ColorFunct(SystemVector2.Zero, Vector2.Zero));
            }

            points = new(100);
            points.SetPoints(GetBoltPoints(Start, End, 10f, 10f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
            Projectile.Kill();

        Time++;
    }

    public override bool? CanDamage() => Projectile.numHits <= 0;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.friendly = false;
        Time = 1f;
        PreviousNPCs.Add(target);

        if (Current < MaxChains)
        {
            NPC close = NPCTargeting.GetClosestNPC(new(Projectile.Center, 1000, true, false, PreviousNPCs));
            if (close.CanHomeInto())
            {
                Vector2 end = close.RandAreaInEntity();
                LightningChain chain = Main.projectile[Projectile.NewProj(end, Vector2.Zero, ModContent.ProjectileType<LightningChain>(), Projectile.damage, Projectile.knockBack, Projectile.owner)].As<LightningChain>();
                chain.NotPrimary = true;
                chain.Start = End;
                chain.End = end;
                chain.Current = Current + 1;
                chain.Power = Power;
                chain.PreviousNPCs = new List<NPC>(PreviousNPCs) { close };
                for (float i = .2f; i < 1f; i += .1f)
                    ParticleRegistry.SpawnGlowParticle(end, Vector2.Zero, 30, Width * i, ColorFunct(SystemVector2.Zero, Vector2.Zero));
            }
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utils.CenteredRectangle(End, Vector2.One * WidthFunct(.5f)).Intersects(targetHitbox);
    }

    public float WidthFunct(float c) => Width * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan) * Projectile.Opacity;
    public ManualTrailPoints points;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}