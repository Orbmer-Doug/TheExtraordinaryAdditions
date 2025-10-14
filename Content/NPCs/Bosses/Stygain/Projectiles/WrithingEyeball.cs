using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class WrithingEyeball : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WrithingEyeball);
    public ref float Time => ref Projectile.ai[0];
    public ref float Dir => ref Projectile.ai[1];
    public bool Free
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float State => ref Projectile.AdditionsInfo().ExtraAI[1];
    public override void SetStaticDefaults()
    {
        // Ensure the telegraph can be seen
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = TelegraphWidth;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.hostile = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 240;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public const int TelegraphTime = 45;
    public const int TelegraphWidth = 2000;
    public override bool ShouldUpdatePosition()
    {
        return Free || Time > TelegraphTime;
    }

    public override bool CanHitPlayer(Player target) => ShouldUpdatePosition();
    public override void SafeAI()
    {
        after ??= new(6, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 0f, null, false, -.1f));
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, OffsetFunction, 60);

        // Increasing power of light
        Lighting.AddLight(Projectile.Center, Color.DarkRed.ToVector3() * .6f * (!Free ? TeleCompletion : 1.5f));

        if (Projectile.timeLeft > 20 && Time > 15f)
        {
            foreach (Player player in Main.ActivePlayers)
            {
                if (player == null || player.dead)
                    continue;
                if (player.Hitbox.Intersects(Projectile.Hitbox) && State != 1f)
                {
                    State = 1f;
                    Time = 0f;
                    this.Sync();
                }
            }
        }

        if (State == 1f)
        {
            if (Time == 0f)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularLimited(8f, 8f, .5f, 2f);

                    ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, vel, Main.rand.Next(16, 20), Main.rand.NextFloat(.6f, 1.4f), Color.Crimson);
                    ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel * .7f, 20, .7f, Color.DarkRed);

                    ParticleRegistry.SpawnBloodParticle(Projectile.Center, vel * 1.4f, Main.rand.Next(30, 50), Main.rand.NextFloat(.7f, 1.2f), Color.DarkRed);
                }

                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 108f, Vector2.Zero, 35, Color.Crimson, 0f, Color.DarkRed, true);

                SoundEngine.PlaySound(SoundID.NPCDeath23 with { Pitch = -.35f, Volume = .7f, PitchVariance = .1f, MaxInstances = 20 }, Projectile.Center);

                Projectile.timeLeft = 14;
                Projectile.Resize(108, 108);
                Projectile.Damage();
            }

            Projectile.Opacity = Projectile.scale = 0f;
        }
        else
        {
            Projectile.FacingUp();
            if (!Free && ShouldUpdatePosition() && Projectile.velocity.Length() < 30f)
                Projectile.velocity *= 1.135f;

            Projectile.scale = (MathF.Sin(Time * .14f + Projectile.identity % 16) * .2f + .8f) * InverseLerp(0f, 15f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);

            if (!Free)
            {
                // Choose a direction to rotate
                if (Time == 0f && this.RunServer())
                {
                    Dir = Main.rand.NextFromList(1, -1);
                    this.Sync();
                }

                // Stick to stygain while beaming the telegraph
                if (Time < TelegraphTime)
                    Projectile.Center = Owner.Center;

                // Rotate the eyes slightly
                if (Time < TelegraphTime / 2)
                {
                    float rotAmt = DifficultyBasedValue(.01f, .02f, .025f, .03f, .035f, .04f);
                    float interpolant = Convert01To010(InverseLerp(0f, TelegraphTime / 2, Time));
                    Projectile.velocity = Projectile.velocity.RotatedBy(rotAmt * Dir * interpolant);
                }

                Vector2 start = Projectile.Center;
                Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.Zero) * TelegraphWidth * InverseLerp(0f, 15f, Time);
                cache ??= new(60);
                cache.SetPoints(start.GetLaserControlPoints(end, 60));
            }

            if (ShouldUpdatePosition())
            {
                if (Time % 5f == 4f)
                    ParticleRegistry.SpawnBloomLineParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.4f, .65f),
                        Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), Color.Crimson);

                Dust.NewDustPerfect(Projectile.RotHitbox().Left, DustID.Blood, -Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.2f, .4f),
                    0, default, Main.rand.NextFloat(.5f, 1.1f));
            }
        }
        Time++;
    }

    public float TeleCompletion => InverseLerp(0f, TelegraphTime, Time);
    public float WidthFunction(float c)
    {
        return Projectile.width * MathHelper.SmoothStep(0.6f, 1f, InverseLerp(0f, 0.8f, c)) * (1f - TeleCompletion);
    }
    public Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        Color col = MulticolorLerp(c.X + Main.GlobalTimeWrappedHourly * 2f, Color.Crimson, Color.DarkRed, Color.DarkRed * 1.4f);
        col *= 1f - TeleCompletion;
        col *= .5f;
        return col * Projectile.Opacity;
    }
    public SystemVector2 OffsetFunction(float completionRatio)
    {
        return SystemVector2.One * MathF.Sin(completionRatio * MathHelper.Pi + Time / 11f) * 8f;
    }

    public TrailPoints cache;
    public FancyAfterimages after;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        if (Time <= TelegraphTime && !Free)
        {
            void draw()
            {
                if (trail == null || trail.Disposed || cache == null)
                    return;
                ManagedShader prim = ShaderRegistry.SideStreakTrail;
                prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 1);
                trail.DrawTrail(prim, cache.Points, 90);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }

        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        after?.DrawFancyAfterimages(texture, [Color.Crimson, Color.DarkRed, Color.IndianRed], TeleCompletion);

        Projectile.DrawProjectileBackglow(Color.DarkRed * TeleCompletion, Free ? 10f : 6f, 72, 6);

        // Draw the base sprite and glowmask.
        Color col = Free ? Projectile.GetAlpha(Color.White) : Projectile.GetAlpha(Color.White * TeleCompletion);
        Main.EntitySpriteDraw(texture, drawPosition, frame, col, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);

        return false;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(this, target, info.Damage);

        if (State != 1f)
        {
            Time = 0f;
            State = 1f;
            this.Sync();
        }
    }
}