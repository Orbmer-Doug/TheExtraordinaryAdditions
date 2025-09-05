using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;

public class MeteorSpawn : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MeteorSpawn);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
    }

    public Color FireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red.Lerp(Color.Orange, .5f), Color.OrangeRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
    public override void SetDefaults()
    {
        Projectile.width = 36;
        Projectile.height = 36;
        Projectile.netImportant = true;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.timeLeft = 18000;
        Projectile.timeLeft *= 5;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public ref float OffsetAngle => ref Projectile.ai[0];

    public ref float Time => ref Projectile.ai[1];
    public ref float FireTimer => ref Projectile.ai[2];
    public bool Fire
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Projectile.frame = Main.rand.Next(0, 4);

            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            int projectileCount = 20;
            float projectileSpread = projectileCount;
            for (int i = 0; i < projectileCount; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / projectileSpread + offsetAngle).ToRotationVector2() * 3f * Main.rand.NextFloat(0.83f);
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, shootVelocity, 50, Main.rand.NextFloat(.6f, 1.1f), FireColor, .45f, true);
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, shootVelocity * .4f, 40, Main.rand.NextFloat(20f, 40f), FireColor, 1.4f);
            }
            Projectile.localAI[0] += 1f;
        }

        points ??= new(5);
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 5);

        if (FireTimer == 0f && Modded.SafeMouseRight.Current)
        {
            Fire = true;
        }

        if (Fire == true)
        {
            FireTimer++;

            // Reel back
            if (FireTimer.BetweenNum(1f, 10f))
            {
                if (this.RunLocal())
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.mouseWorld) * -11f, .25f);
                this.Sync();
            }

            // Fire
            if (FireTimer.BetweenNum(10f, 12f))
            {
                if (this.RunLocal())
                    Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld) * 20f;
                this.Sync();
            }
            points.Update(Projectile.Center + Projectile.velocity);

            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            if (Projectile.timeLeft > 60)
                Projectile.timeLeft = 60;
        }

        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 1.4f);

        Time++;
        if (Fire == false)
        {
            Hover();
            OffsetAngle += MathHelper.ToRadians(1f);
        }

        Projectile.rotation += MathHelper.ToRadians(3f);
    }

    private void Hover()
    {
        Projectile.AI_GetMyGroupIndex(out var index, out var total);

        float time = SecondsToFrames(11f);
        float cycle = Modded.GlobalTimer % time / time * MathF.Tau;
        float offset = MathF.Tau * InverseLerp(0f, total, index);
        Vector2 dest = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + GetPointOnRotatedEllipse(200f, 120f, offset + cycle, cycle * 4f);
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(dest) * MathHelper.Min(Projectile.Center.Distance(dest), 20f), .22f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire, SecondsToFrames(Main.rand.NextFloat(2f, 3f)));
        Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Item89.Play(Projectile.Center, .9f, 0f, .1f);
        for (int i = 0; i < 38; i++)
        {
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, RandomVelocity(2.2f, 1.2f, 4f),
                40, 1f, Color.Lerp(Color.OrangeRed, Color.DarkOrange, Main.rand.NextFloat(.5f, .7f)));
        }

        Projectile.ExpandHitboxBy(120);
        Projectile.penetrate = -1;
        Projectile.Damage();
    }

    public override bool? CanDamage() => Fire == true;
    internal Color ColorFunction(SystemVector2 c, Vector2 pos)
    {
        return Color.OrangeRed * InverseLerp(1f, 0f, c.X) * InverseLerp(0f, 5f, FireTimer) * InverseLerp(0f, 14f, Projectile.timeLeft);
    }

    internal float WidthFunction(float c)
    {
        return Projectile.width;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && !trail._disposed && points != null)
            {
                ManagedShader shader = ShaderRegistry.StandardPrimitiveShader;
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, 3, 0, 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        Projectile.DrawProjectileBackglow(Color.OrangeRed, 6f, 0, 6, direction, frame);
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);

        return false;
    }
}
