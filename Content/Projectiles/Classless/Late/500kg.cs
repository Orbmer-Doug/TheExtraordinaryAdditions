using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class _500kg : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture._500kg);
    public override void SetDefaults()
    {
        Projectile.width = 20;//106;
        Projectile.height = 20;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hide = true;
        Projectile.DamageType = DamageClass.Generic;
    }

    public bool HitGround
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float Timer => ref Projectile.ai[1];
    public const int TimeBeforeBoom = 120;
    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override void AI()
    {
        cache ??= new(20);
        cache.Update(Projectile.Center);
        if (trail == null || trail._disposed)
            trail = new(c => Projectile.height, (c, pos) => Color.WhiteSmoke * MathHelper.SmoothStep(1f, 0f, c.X), null, 20);

        if (HitGround)
        {
            Timer++;
        }
        else
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -50f, 50f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.timeLeft = TimeBeforeBoom;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            AdditionsSound.LegStomp.Play(Projectile.Center, 1.5f, -.3f, .1f);
            HitGround = true;
            Projectile.netUpdate = true;
        }

        Projectile.velocity *= .5f;
        if (Projectile.velocity.Length() < 4f)
            Projectile.velocity = Vector2.Zero;

        return false;
    }

    public override bool? CanDamage()
    {
        return Projectile.timeLeft < 2;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);
    }

    public const int Size = 1000;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        float fallOff = Utils.Remap(Size - target.Distance(Projectile.Center) * 2, 0f, Size, 0.05f, 1f);
        target.velocity += Projectile.Center.SafeDirectionTo(target.Center) * Projectile.knockBack * fallOff * (target.knockBackResist * .9f);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.Knockback *= 0f;
        float fallOff = Utils.Remap(Size - target.Distance(Projectile.Center) * 2, 0f, Size, 0.05f, 1f);
        modifiers.FinalDamage *= fallOff;
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.Resize(Size, Size);
            Projectile.Damage();
        }

        Vector2 pos = Projectile.Center;
        for (int i = 0; i < 400; i++)
        {
            Vector2 vel = -Vector2.UnitY.RotatedByRandom(1.8f) * Main.rand.NextFloat(0f, 30f);
            int life = Main.rand.Next(120, 220);
            float scale = Main.rand.NextFloat(1.2f, 2.4f);
            Color color = Color.OrangeRed.Lerp(Color.Chocolate, Main.rand.NextFloat(.3f, .6f));

            ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale * 150f, color);
            ParticleRegistry.SpawnGlowParticle(pos, vel * 1.2f, life + 20, scale * 100f, color, Main.rand.NextFloat(.6f, 1.1f), true);

            ParticleRegistry.SpawnCloudParticle(pos, vel, color, Color.Transparent, life, scale, Main.rand.NextFloat(.7f, 1.5f));
            ParticleRegistry.SpawnCloudParticle(pos, vel * 1.6f, color, Color.Transparent, life - 10, scale - .1f, Main.rand.NextFloat(.7f, 1.2f));

            ParticleRegistry.SpawnSquishyLightParticle(pos, vel * 4f, life / 2, scale, color * 1.4f);
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel * 6f, life / 3, scale * 1.3f, color * 1.8f, Main.rand.NextFloat(.5f, 1f), 1.2f);

            ParticleRegistry.SpawnDustParticle(pos, vel * 5f, life / 2, scale * 1.2f, color, .2f, true, true);

            Dust.NewDustPerfect(Projectile.Center, DustID.Stone, vel * 2.1f, 0, default, Main.rand.NextFloat(.9f, 1.5f));
            Dust.NewDustPerfect(Projectile.Center, DustID.Dirt, vel * 2.2f, 0, default, Main.rand.NextFloat(.9f, 1.5f));
        }

        ScreenShakeSystem.New(new(3f, 2.3f), Projectile.Center);
        
        ParticleRegistry.SpawnFlash(Projectile.Center, 22, 1.6f, Size * 1.5f);
        ParticleRegistry.SpawnChromaticAberration(Projectile.Center, 142, .8f, Size * 2f);

        AdditionsSound.GaussBoom.Play(Projectile.Center, 1.4f, -.2f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        trail?.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 30);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}
