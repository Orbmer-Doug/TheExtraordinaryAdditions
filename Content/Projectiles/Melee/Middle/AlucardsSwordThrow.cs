using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class AlucardsSwordThrow : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AlucardsSwordThrow);
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 48;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12;
        Projectile.tileCollide = false;
    }
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(c => 18f * MathHelper.SmoothStep(1f, 0f, c), (c, pos) => Color.DarkRed.Lerp(Color.Black, c.X) * MathHelper.SmoothStep(1f, 0f, c.X), null, 15);
        cache ??= new(15);
        cache.Update(Projectile.RotHitbox().Right);

        Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Time, true);
        if (Time % 16f == 0f)
        {
            float lightVelocityArc = MathHelper.PiOver2;
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Unit() * Projectile.width * 1.5f * Main.rand.NextFloat(0.75f, 0.96f);
                Vector2 vel = (pos - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(lightVelocityArc) * Main.rand.NextFloat(1f, 3f);
                float size = Main.rand.NextFloat(0.24f, 0.51f);
                int lifetime = Main.rand.Next(21, 34);
                ParticleRegistry.SpawnGlowParticle(pos, vel, lifetime, size, Color.DarkRed);
            }
        }

        float direction = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true).SafeDirectionTo(Projectile.Center).ToRotation();
        Owner.SetFrontHandBetter(0, direction);
        Owner.SetBackHandBetter(0, direction);

        Owner.ChangeDir((Projectile.Center.X > Owner.Center.X).ToDirectionInt());

        if (Owner.channel)
        {
            if (this.RunLocal())
            {
                Vector2 mouse = Owner.Additions().mouseWorld;
                float dist = Projectile.Distance(mouse);
                float ratio = (1.45f - InverseLerp(0f, 1000f, dist)) * .25f;
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(mouse) * MathHelper.Clamp(dist, MathHelper.Min(dist, 5f), 40f), ratio);
            }
            Projectile.rotation += 0.25f / Projectile.MaxUpdates;
        }
        else
        {
            if (this.RunLocal())
                Projectile.velocity = Projectile.SafeDirectionTo(Owner.Center) * 22f;

            if (Projectile.RotHitbox().Intersects(Owner.RotHitbox()))
            {
                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnGlowParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * Main.rand.NextFloat(.1f, .2f),
                        Main.rand.Next(10, 20), Main.rand.NextFloat(.4f, .5f), Color.DarkRed);
                }
                Projectile.Kill();
            }
            float idealAngle = Projectile.AngleTo(Owner.Center) - MathHelper.PiOver4;
            Projectile.rotation = Projectile.rotation.AngleLerp(idealAngle, 0.1f);
            Projectile.rotation = Projectile.rotation.AngleTowards(idealAngle, 0.25f);
        }
        if (Projectile.velocity != Projectile.oldVelocity)
            this.Sync();

        Owner.itemTime = Owner.itemAnimation = 2;

        Lighting.AddLight(Projectile.RotHitbox().Right, Color.Red.ToVector3() * Projectile.Opacity * .5f);
        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.FinalDamage *= InverseLerp(10f, 40f, Projectile.velocity.Length()) * 2f;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.RotHitbox().Left, Projectile.RotHitbox().Right);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos;

        if (CheckLinearCollision(Projectile.RotHitbox().Right, Projectile.RotHitbox().Left, target.Hitbox, out Vector2 start, out Vector2 end))
            pos = start;
        else
            pos = Projectile.Center;

        int life = Main.rand.Next(15, 20);
        float scale = Main.rand.NextFloat(1.1f, 1.7f);
        Vector2 vel = Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.4f, .6f);
        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, life, scale, Color.White, Color.Red, Main.rand.NextFloat(1.4f, 1.6f));

        // fun fact these are the sounds of me hitting a tungsten cube with solid glass
        AdditionsSound.MetalHit3.Play(Projectile.Center, 1f, .1f, .1f, 5);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Texture2D glowmask = AssetRegistry.GetTexture(AdditionsTexture.AlucardsSwordThrow_Glow);
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Streak), 1);
            trail.DrawTrail(shader, cache.Points, 120);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        // Draw the base sprite and glowmask.
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
        Main.EntitySpriteDraw(glowmask, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);

        return false;
    }

}
