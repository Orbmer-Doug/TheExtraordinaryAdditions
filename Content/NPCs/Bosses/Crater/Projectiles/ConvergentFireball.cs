using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class ConvergentFireball : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;

    public float Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public float Cooldown
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public bool Stolen
    {
        get => Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public static readonly int ScaleUpTime = SecondsToFrames(1f);
    public const int MaxScale = 200;
    public LoopedSoundInstance flame;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = MaxScale;
        Projectile.hostile = true;
        Projectile.scale = 0f;
        Projectile.penetrate = -1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 7000;
        Projectile.netImportant = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        flame ??= LoopedSoundManager.CreateNew(new(AdditionsSound.BraveSmallFireLoop, () => .6f,
            () => -.2f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => Projectile.active);
        flame?.Update(Projectile.Center);

        Player playerTarget = null;
        PlayerTargeting.FindNearestPlayer(Projectile.Center, out playerTarget);

        if (Time < ScaleUpTime)
        {
            if (playerTarget != null)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, playerTarget.velocity, 0.6f);
            Projectile.scale = Animators.MakePoly(3f).OutFunction.Evaluate(Time, 0f, ScaleUpTime, 0f, MaxScale);
        }

        float fade = InverseLerp(Asterlin.UnveilingZenith_TotalTime, Asterlin.UnveilingZenith_TotalTime - 80f, ModOwner.AITimer);
        Vector2 target = Vector2.Lerp(ModOwner.LeftVentPosition, ModOwner.RightVentPosition, .5f);
        if (!Stolen)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target).SafeNormalize(Vector2.Zero) * 2f, 0.03f * fade);
            Projectile.velocity += Projectile.DirectionTo(target).SafeNormalize(Vector2.Zero) * (0.1f + InverseLerp(900f, 1600f, Projectile.Distance(target)) * fade);
        }
        else
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target).SafeNormalize(Vector2.Zero) * MathHelper.Min(Projectile.Distance(target), 10f), 0.7f);
        }

        Projectile.ProjAntiClump(.1f, false);

        if (Cooldown <= 0f && !Stolen && fade == 1)
        {
            if (Projectile.Distance(target) < 84f && Time > ScaleUpTime)
            {
                Stolen = true;
                ModOwner.UnveilingZenith_CurrentAmount++;
                ModOwner.Sync();
                this.Sync();
            }

            foreach (Player player in Main.ActivePlayers)
            {
                if (player.DeadOrGhost || player.Distance(Projectile.Center) > 64f)
                    continue;

                Projectile.velocity = Projectile.DirectionFrom(player.Center).SafeNormalize(Vector2.Zero) * (14f + Projectile.velocity.Length() + player.velocity.Length());
                Cooldown += 15f;
                if (Time > 40f)
                    SoundID.Item56.Play(Projectile.Center, 5f, -.4f, .2f);

                for (int i = 0; i < 40; i++)
                {
                    Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(36f, 36f);
                    Vector2 vel = Main.rand.NextVector2Circular(7f, 7f) + Projectile.velocity * Main.rand.NextFloat(.5f, .8f);
                    int life = Main.rand.Next(30, 45);
                    ParticleRegistry.SpawnGlowParticle(pos, vel * .1f, life / 2, Main.rand.NextFloat(40f, 140f), Color.Orange, .9f);
                    ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 2, Main.rand.NextFloat(.8f, 2.4f), Color.OrangeRed, Color.Goldenrod, 4);
                    ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, Main.rand.NextFloat(.6f, 1.1f), Color.OrangeRed);
                }

                this.Sync();
            }
        }

        if (Cooldown > 0f)
            Cooldown--;

        Projectile.scale = MathHelper.Lerp(0f, MaxScale, 1f - InverseLerp(0f, Asterlin.UnveilingZenith_StarCollapseTime, ModOwner.UnveilingZenith_CollapseTimer));
        Projectile.Opacity = Animators.BezierEase(InverseLerp(0f, ScaleUpTime, Time)) * Animators.MakePoly(2f).InFunction(fade);
        if (fade != 1)
        {
            for (int i = 0; i < 7; i++)
            {
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(10f, 10f) + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(1.4f, 2.4f) * Projectile.Opacity,
                    Color.OrangeRed.Lerp(Color.Gold, Main.rand.NextFloat(0f, .4f)), Projectile.Opacity + .2f);
                ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(14f, 14f) + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.Next(40, 60), Main.rand.NextFloat(2.5f, 3.5f) * Projectile.Opacity, Color.OrangeRed, Color.Gold, 4, false, false, Main.rand.NextFloat(-.2f, .2f));
            }
        }
        if (fade <= 0)
            Projectile.Kill();

        Projectile.timeLeft = 7000;
        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(oldVelocity.X - Projectile.velocity.X) > 1f)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(oldVelocity.Y - Projectile.velocity.Y) > 1f)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float rad = Projectile.scale;
        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(rad)), null, Color.White, 0f, tex.Size() / 2, 0);
        }

        ManagedShader shader = AssetRegistry.GetShader("IntenseFireball");
        shader.TrySetParameter("time", Time * .01f);
        shader.TrySetParameter("resolution", new Vector2(rad));
        shader.TrySetParameter("opacity", Projectile.Opacity);

        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderPlayers, null, shader);
        return false;
    }
}

public class DisintegrationNova : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override bool IgnoreOwnerActivity => true;
    public static int Lifetime = SecondsToFrames(1.4f);
    public static int MaxRadius = 11000;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 14000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(1);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        // As it is intended to be punishing, disallow players to just tank the hit with other buffs
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.creativeGodMode)
                continue;

            player.KillMe(PlayerDeathReason.ByCustomReason(GetNetworkText($"Status.Death." + (Main.rand.NextBool() ? "AsterlinDeath2" : "AsterlinDeath1"), player.name)),
                Projectile.damage, (Projectile.Center.X > player.Center.X).ToDirectionInt(), false);
            player.RemoveAllIFrames();
        }

        if (Time == 0)
        {
            ScreenShakeSystem.New(new(2f, 2.2f, 9000f), Projectile.Center);
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, Lifetime / 2, 1.5f, MaxRadius * 2);
            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, Lifetime / 2, .5f, MaxRadius * 2);
            ParticleRegistry.SpawnFlash(Projectile.Center, 20, .2f, MaxRadius * 2);
            AdditionsSound.MomentOfCreation.Play(Projectile.Center, 1.5f, -1f);
        }
        Projectile.scale = InverseLerp(0f, Lifetime, Time);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, MaxRadius * Projectile.scale, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader fireball = AssetRegistry.GetShader("FireballExplosion");
            fireball.TrySetParameter("scale", Projectile.scale);

            Main.spriteBatch.EnterShaderRegion(null, fireball.Effect);
            Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise);
            fireball.Render();
            Main.spriteBatch.DrawBetterRect(noise, ToTarget(Projectile.Center, new Vector2(MaxRadius)), null, Color.Goldenrod, 0f, noise.Size() / 2f);
            Main.spriteBatch.ExitShaderRegion();
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.Dusts);
        return false;
    }
}