using CalamityMod.Buffs.StatBuffs;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
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

    public float HitCount
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public bool Stolen
    {
        get => Projectile.Additions().ExtraAI[0] == 1;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

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
        flame ??= LoopedSoundManager.CreateNew(new(AdditionsSound.BraveMediumFireLoop, () => 1.2f, () => -.2f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => Projectile.active);
        flame.Update(Projectile.Center);

        if (Time < 60f)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Target.velocity, 0.9f);
            Projectile.scale = Animators.MakePoly(3f).OutFunction.Evaluate(Time, 0f, 60f, 0f, MaxScale);
        }

        if (!Stolen)
        {
            if (HitCount < 0f || HitCount == 1f)
            {
                Projectile.velocity = Projectile.DirectionTo(Target.Center).SafeNormalize(Vector2.Zero) * 36f;
                Cooldown = 0;
            }
            else
            {
                if (Main.rand.NextBool(5))
                {
                    Projectile.velocity += Main.rand.NextVector2Circular(3f, 3f);
                }
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(Owner.Center).SafeNormalize(Vector2.Zero) * 3f, 0.03f) * InverseLerp(500f, 470f, Time);
                Projectile.velocity += Projectile.DirectionTo(Owner.Center).SafeNormalize(Vector2.Zero) * (0.3f + InverseLerp(500f, 800f, Projectile.Distance(Owner.Center)));
            }
        }
        else
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(Owner.Center).SafeNormalize(Vector2.Zero) * MathHelper.Min(Projectile.Distance(Owner.Center), 10f), 0.7f);
        }

        Projectile.ProjAntiClump(.05f, false);

        if (Cooldown <= 0f && !Stolen)
        {
            if (Projectile.Distance(Owner.Center) < 84f && Time > 60f )
            {
                Stolen = true;
                Boss.UnveilingZenith_CurrentAmount++;
                this.Sync();
            }

            using IEnumerator<Player> enumerator = Main.player.Where((Player n) => n.active && !n.dead && n.Distance(Projectile.Center) < 64f).GetEnumerator();
            if (enumerator.MoveNext())
            {
                Player player = enumerator.Current;
                if (HitCount < 0f || HitCount == 1f)
                {
                    HitCount += 1f;
                    Cooldown += 15f;
                    Projectile.velocity = -Vector2.UnitY * 10f;
                }
                else
                {
                    Projectile.velocity = Projectile.DirectionFrom(player.Center).SafeNormalize(Vector2.Zero) * (14f + Projectile.velocity.Length() + player.velocity.Length());
                }
                Cooldown += 15f;
                if (Time > 40f)
                    SoundID.Item56.Play(Projectile.Center, 2f, -.4f, .2f);

                for (int i = 0; i < 40; i++)
                {
                    Color glowColor2 = Main.hslToRgb(Projectile.localAI[0] * 0.01f % 1f, 1f, 0.5f, 0);
                    glowColor2.A /= 2;
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(36f, 36f), 261, Main.rand.NextVector2Circular(15f, 15f) + Projectile.velocity, 0, glowColor2, 1f + Main.rand.NextFloat(2f)).noGravity = true;
                    if (Main.rand.NextBool(3))
                    {

                    }
                }
            }
        }
        if (Cooldown > 0f)
        {
            Cooldown--;
        }

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
        shader.TrySetParameter("opacity", Animators.MakePoly(3f).OutFunction(Utils.Remap(Projectile.scale, 0f, MaxScale, 0f, 1f)));

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
                return;
            player.KillMe(PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI), Projectile.damage, (Projectile.Center.X > player.Center.X).ToDirectionInt(), false);
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
        ManagedShader fireball = AssetRegistry.GetShader("FireballExplosion");
        fireball.TrySetParameter("scale", Projectile.scale);

        Main.spriteBatch.EnterShaderRegion(null, fireball.Effect);
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise);
        fireball.Render();
        Main.spriteBatch.DrawBetterRect(noise, ToTarget(Projectile.Center, new Vector2(MaxRadius)), null, Color.Goldenrod, 0f, noise.Size() / 2f);
        Main.spriteBatch.ExitShaderRegion();
        return false;
    }
}