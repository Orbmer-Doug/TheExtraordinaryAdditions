using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class CrystallineBlast : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.hostile = false;

        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.timeLeft = 300;
        Projectile.penetrate = 3;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.aiStyle = 0;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;

        Projectile.scale = 0f;
    }
    public ref float Timer => ref Projectile.ai[0];
    public Projectile Proj => Main.projectile[(int)Projectile.ai[1]];
    private bool Released
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    private const float maxTime = CrystallineSnapcurve.TotalTime;
    public float Completion => InverseLerp(0f, maxTime, Timer);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public const float IdealScale = .5f;
    public override void AI()
    {
        bool notReady = Timer < maxTime && !Released;
        bool release = !Modded.MouseLeft.Current && notReady;
        if (((Proj is null || Proj.active == false) && notReady) || Owner.dead || Owner.active == false || Projectile.penetrate <= 0 || (Released && Projectile.scale <= .2f))
        {
            Projectile.Kill();
            return;
        }

        if (!Released)
            Projectile.tileCollide = false;
        else
            Projectile.tileCollide = true;

        if (!Released)
        {
            Projectile.Center = Proj.Center + PolarVector(Owner.HeldItem.width * .55f, Proj.velocity.ToRotation());
            Projectile.velocity = Proj.velocity;

            int damage = Proj.damage;
            damage = (int)(damage * Completion);
            if (Timer >= maxTime)
                damage *= 2;
            Projectile.damage = damage;

            Projectile.timeLeft = 300;
            Projectile.scale = Utils.Remap(Timer, 0f, maxTime, 0f, IdealScale);
            Projectile.ExpandHitboxBy((int)(Projectile.scale * 75f));

            if (Timer > 0f && Proj.As<SnapcurveHeld>().LimbRotation == 0f)
            {
                if (Projectile.scale <= .2f)
                {
                    Projectile.Kill();
                    return;
                }

                AdditionsSound.etherealRelease2.Play(Projectile.Center, .7f, 0f, .1f);
                if (this.RunLocal())
                {
                    float vel = 20f + Completion;
                    Projectile.velocity = Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * vel;

                    for (int i = 0; i < 12; i++)
                    {
                        ParticleRegistry.SpawnSparkParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.35f) * Main.rand.NextFloat(.4f, .7f),
                            Main.rand.Next(15, 24), Main.rand.NextFloat(.3f, .6f), Color.Wheat);
                    }

                    Released = true;
                    this.Sync();
                }
            }
        }

        if (Released)
        {
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f) * Projectile.scale * 2f,
                Main.rand.Next(90, 120), Main.rand.NextFloat(.6f, .7f) * Projectile.scale * 2f, Color.Wheat, Color.White);
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Timer++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            ParticleRegistry.SpawnSquishyLightParticle(Projectile.RotHitbox().RandomPoint(), Main.rand.NextVector2Circular(4f, 4f) * Projectile.scale * 2f,
                Main.rand.Next(10, 14), Projectile.scale, Color.White.Lerp(Color.Wheat, Main.rand.NextFloat()));
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.scale -= .1f;
        Projectile.damage = (int)(Projectile.damage * .75f);
        OnHitEffects();
    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        OnHitEffects();
    }

    private void OnHitEffects()
    {
        Vector2 pos = Projectile.RotHitbox().Right;
        float offsetAngle = RandomRotation();
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / count + offsetAngle).ToRotationVector2() * 5f * InverseLerp(0f, .5f, Projectile.scale);
            ParticleRegistry.SpawnSparkParticle(pos, shootVelocity, 30 - (int)Utils.Remap(Projectile.scale, 0f, .5f, 15, 0), Main.rand.NextFloat(.5f, .8f), Color.Cyan);
        }

        AdditionsSound.etherealSmallHit.Play(pos, Projectile.scale * 2f);
    }

    public override bool OnTileCollide(Vector2 lastVelocity)
    {
        Projectile.penetrate--;
        Projectile.scale -= .1f;
        Projectile.damage = (int)(Projectile.damage * .75f);

        if (Projectile.velocity.X != lastVelocity.X && Math.Abs(lastVelocity.X) > 0f)
            Projectile.velocity.X = -lastVelocity.X;
        if (Projectile.velocity.Y != lastVelocity.Y && Math.Abs(lastVelocity.Y) > 0f)
            Projectile.velocity.Y = -lastVelocity.Y;

        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = -lastVelocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.01f, .3f);
            int life = Main.rand.Next(12, 20);
            float size = Main.rand.NextFloat(.3f, .5f);
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, life, size, Color.Lerp(Color.WhiteSmoke, Color.Wheat, Main.rand.NextFloat(.2f, .8f)));
        }

        AdditionsSound.etherealBounceSmall.Play(Projectile.Center, Projectile.scale * 2, 0f, .05f, 10);

        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.UseBlendState(BlendState.Additive);
        Vector2 origin = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall).Size() * 0.5f;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;

        float val = .1f;
        Vector2 baseScale = new Vector2(val) * Projectile.scale;

        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall), drawPosition, null, Color.White, 0f, origin, baseScale, 0, 0f);

        Vector2 flareOrigin = AssetRegistry.GetTexture(AdditionsTexture.BloomFlare).Size() * .5f;
        Color bloomFlareColor = Color.Lerp(Color.Wheat, Color.WhiteSmoke, 0.7f);
        float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.76f;
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), drawPosition, null, bloomFlareColor, -bloomFlareRotation, flareOrigin, baseScale, 0, 0f);

        bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + 0.5f) % 1f, 1f, 0.55f), 0.7f);
        bloomFlareColor = Color.Lerp(bloomFlareColor, Color.LightCyan, 0.63f);
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), drawPosition, null, bloomFlareColor, bloomFlareRotation, flareOrigin, baseScale, 0, 0f);
        Main.spriteBatch.ResetBlendState();
        return false;
    }
    public override bool? CanHitNPC(NPC target) => Released ? null : false;
}