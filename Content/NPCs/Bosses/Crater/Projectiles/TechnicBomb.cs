using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TechnicBomb : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.Size = new(60f);
        Projectile.timeLeft = 350;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public float Size => 190f * Projectile.scale;
    public ref float Time => ref Projectile.ai[0];
    public bool Hit
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToDirectionInt();
    }
    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 10);
        if (trail2 == null || trail2.Disposed)
            trail2 = new(WidthFunct2, ColorFunct2, null, 10);

        Projectile.velocity *= .98f;
        Projectile.Opacity = InverseLerp(0f, 20f, Time) * InverseLerp(0f, 30f, Projectile.timeLeft);
        Projectile.scale = Animators.MakePoly(3f).InFunction(InverseLerp(0f, 30f, Time) * InverseLerp(0f, 30f, Projectile.timeLeft));

        if (Main.rand.NextBool(7))
            ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f) + Main.rand.NextVector2Circular(7f, 7f), Main.rand.Next(20, 40), Main.rand.NextFloat(.6f, 1.2f), new(18, 193, 255), Main.rand.NextFloat(.7f, 1.2f), Main.rand.NextFloat(1f, 1.7f));
        if (Main.rand.NextBool(6))
            ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(200f, 200f, .5f, 1f), Main.rand.Next(10, 12), Main.rand.NextFloat(.4f, .6f), Color.Cyan);

        float rad = Size;
        for (int i = 0; i < 10; i++)
        {
            float add = i / 10f;
            points.Update(Projectile.Center + GetPointOnRotatedEllipse(rad, rad / 3f, Time * .005f, Main.GameUpdateCount * 0.15f + add));
            points2.Update(Projectile.Center + GetPointOnRotatedEllipse(rad, rad / 3f, MathHelper.PiOver2 - Time * .01f, Main.GameUpdateCount * 0.15f + MathHelper.Pi + add));
        }

        if (Projectile.Center.WithinRange(Owner.Center, 200f))
            Projectile.velocity += Owner.Center.SafeDirectionTo(Projectile.Center) * .4f;

        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.type != ModContent.ProjectileType<TheLightripBullet>() || !p.Hitbox.Intersects(Projectile.Center.ToRectangle(100, 100)) || Hit)
                continue;

            p.As<TheLightripBullet>().HitEffects();
            AdditionsSound.ElectricalPowBoom.Play(Projectile.Center, 1f, 0f, .14f);
            if (this.RunServer())
            {
                float rand = RandomRotation();
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * InverseLerp(0f, 3, i) + rand).ToRotationVector2().RotatedByRandom(.3f) * 3f;
                    SpawnProjectile(Projectile.Center, vel, ModContent.ProjectileType<OverchargedLaser>(), Asterlin.LightAttackDamage, 0f);
                }
                SpawnProjectile(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TechnicBlast>(), Asterlin.MediumAttackDamage, 0f);
            }

            Hit = true;
            Projectile.Kill();
        }

        Time++;
    }
    public override bool? CanDamage() => false;
    public float WidthFunct(float c) => 40f * MathHelper.SmoothStep(1f, 0f, c) * GetCircularSectionValue(Main.GameUpdateCount * 0.15f, .3f, .5f, 1f);
    public float WidthFunct2(float c) => 40f * MathHelper.SmoothStep(1f, 0f, c) * GetCircularSectionValue(Main.GameUpdateCount * 0.15f + MathHelper.Pi, .3f, .5f, 1f, MathHelper.PiOver2);
    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return new Color(100, 220, 255) * c.X * GetCircularSectionValue(Main.GameUpdateCount * 0.15f, .5f, .8f, 1.8f) * Projectile.Opacity;
    }
    public Color ColorFunct2(SystemVector2 c, Vector2 pos)
    {
        return new Color(100, 220, 255) * c.X * GetCircularSectionValue(Main.GameUpdateCount * 0.15f + MathHelper.Pi, .5f, .8f, 1.8f, MathHelper.PiOver2) * Projectile.Opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(10);
    public OptimizedPrimitiveTrail trail2;
    public TrailPoints points2 = new(10);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.FireTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseZoomedOut), 1, SamplerState.AnisotropicWrap);
            if (trail != null && points != null)
                trail.DrawTrail(shader, points.Points, 50);

            if (trail2 != null && points2 != null)
                trail2.DrawTrail(shader, points2.Points, 50);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowSoft);
            Color col = new Color(87, 211, 255);

            Vector2 size = new(Size);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size * .5f), null, col.Lerp(Color.White, .6f), 0f, tex.Size() / 2f);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size * .68f), null, col.Lerp(Color.White, .3f) * .9f, 0f, tex.Size() / 2f);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size * .82f), null, col * .8f, 0f, tex.Size() / 2f);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, size * 1.5f), null, col * .6f, 0f, tex.Size() / 2f);

            Texture2D circ = AssetRegistry.GetTexture(AdditionsTexture.HollowCircleSoftEdge);
            float anim = (Time * .22f) % 10 / 10;
            float radii = Animators.MakePoly(3f).OutFunction.Evaluate(0f, 140f, anim);
            Color circCol = Color.Cyan * Animators.MakePoly(2f).OutFunction.Evaluate(1f, 0f, anim);
            Main.spriteBatch.DrawBetterRect(circ, ToTarget(Projectile.Center, new(radii)), null, circCol, 0f, circ.Size() / 2);
        }
        LayeredDrawSystem.QueueDrawAction(glow, PixelationLayer.OverProjectiles, BlendState.Additive);
        return false;
    }
}

public class TechnicBlast : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override bool IgnoreOwnerActivity => true;
    public static int MaxRadius => 100;
    public static int MaxTime => 30;
    public override void SetDefaults()
    {
        Projectile.Size = new(20);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = MaxTime;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Radius => ref Projectile.ai[1];
    public override void SafeAI()
    {
        if (Time == 0)
        {
            for (int i = 0; i < 50; i++)
                ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f) + Main.rand.NextVector2CircularLimited(10f, 10f, .5f, 1f), Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), new(112, 218, 255));

            for (int i = 0; i < 20; i++)
                ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(120f, 120f, .7f, 1f), 12, Main.rand.NextFloat(.2f, .3f), new(66, 206, 255));

            for (int i = 0; i < 30; i++)
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(30, 40), Main.rand.NextFloat(.9f, 1.3f), Color.Cyan);
        }
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 40);

        Radius = Animators.MakePoly(4f).OutFunction.Evaluate(Time, 0f, MaxTime, 0f, MaxRadius);

        for (int i = 0; i < 40; i++)
            points.SetPoint(i, Projectile.Center + Vector2.One.RotatedBy(i / 19f * MathHelper.TwoPi) * Radius);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utility.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);
    }

    public float WidthFunct(float c) => 30f * Utils.Remap(Time, 0f, MaxTime, 1f, 0f);
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(InverseLerp(0f, MaxTime, Time), Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan);

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null || trail.Disposed)
                return;

            ManagedShader shader = ShaderRegistry.PierceTrailShader;
            trail.DrawTrail(shader, points.Points, 300, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}