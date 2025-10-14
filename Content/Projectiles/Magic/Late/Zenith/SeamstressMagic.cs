using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;
public class SeamstressMagic : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SeamstressMagic);
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float Counter => ref Projectile.ai[1];
    private const int ChargeTime = 50;
    private const int TimeForShoot = 245;
    public float ChargeupCompletion => Utils.GetLerpValue(0f, TimeForShoot, Time, true);
    public override void Defaults()
    {
        Projectile.width = (int)(727 * .51);
        Projectile.height = (int)(728 * .51);
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 90000;
    }

    public LoopedSoundInstance slot;
    public SlotId InitializeSlot;
    public float Interpolant => Utils.GetLerpValue(0f, ChargeTime, Counter, true);

    public override bool ShouldDie() => false;
    public override void SafeAI()
    {
        // Otherwise smoothly return
        if (base.ShouldDie())
        {
            Counter--;
            if (Counter <= 0f)
            {
                Projectile.Kill();
                return;
            }
        }
        else
        {
            // Portal visuals
            if (Counter < ChargeTime)
                Counter++;
        }
        Owner.itemAnimation = Owner.itemTime = 2;

        // Start the menacing charge up
        if (Time == 15f)
        {
            if (!SoundEngine.TryGetActiveSound(InitializeSlot, out _))
                InitializeSlot = AdditionsSound.RaptureCharge.Play(Projectile.Center);
        }
        if (SoundEngine.TryGetActiveSound(InitializeSlot, out var t) && t.IsPlaying)
            t.Position = Projectile.Center;

        // Perform shoot behaviors
        HandleChargeEffects();

        // Slowly turn to mouse
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse) * Projectile.Size.Length(), 0.2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        // Adjust center
        Vector2 circlePointDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        Projectile.Center = Owner.Center + circlePointDirection * Animators.MakePoly(2f).InFunction.Evaluate(40f, 120f, Interpolant);

        // Adjust visuals
        Projectile.scale = Interpolant * .5f;
        Projectile.Opacity = MathHelper.Clamp(Projectile.scale * 2, 0f, 1f);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.direction);

        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Vector2 pos = Owner.GetFrontHandPositionImproved();
        if (Time % 2 == 1)
        {
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.24f) * Main.rand.NextFloat(3f, 8f);
            float size = Main.rand.NextFloat(.3f, .65f) * ChargeupCompletion;
            int life = Main.rand.Next(12, 25);
            Color col = Color.Lerp(Color.Magenta, Color.DarkViolet, ChargeupCompletion);

            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, life, size, col, ChargeupCompletion, 1.4f);
            ParticleRegistry.SpawnSparkParticle(pos, vel * 2, life, size, Color.Violet);
        }
        if (Time < TimeForShoot)
        {
            if (Time % 2f == 1f)
            {
                Color energyColor = Color.Lerp(Color.DarkSlateBlue, Color.BlueViolet, Main.rand.NextFloat(0.5f));
                Vector2 rand = pos + Main.rand.NextVector2Circular(200f, 200f);
                Vector2 vel = RandomVelocity(2f, 5f, 8f);
                ParticleRegistry.SpawnBloomPixelParticle(rand, vel, 30, Main.rand.NextFloat(.5f, .78f) * (1f - ChargeupCompletion), Color.Magenta, Color.DarkViolet, pos, 1.4f, 10);
            }
        }

        // Update pulse sound
        slot ??= LoopedSoundManager.CreateNew(new(SoundID.DD2_EtherianPortalIdleLoop, () => 4f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        slot?.Update(Projectile.Center);

        Time++;
    }


    public void HandleChargeEffects()
    {
        if (Time >= 280f)
        {
            if (this.RunLocal() && Modded.MouseLeft.Current)
            {
                if (Time == 280f)
                {
                    Shotgun(Main.rand.Next(3, 4));
                }
                else if (Time == 300f)
                {
                    Shotgun(Main.rand.Next(5, 6));
                }
                else if (Time == 320f)
                {
                    Shotgun(Main.rand.Next(6, 8));
                }
                else if (Time == 385f)
                {
                    Magic(36, 1.67f);
                    AdditionsSound.Rapture.Play(Projectile.Center, .7f, 0f, .14f, 10);
                    Vector2 pos = Projectile.Center + Offset * (-30 + 1f * 100);

                    ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.5f, 1f) * 200f, Vector2.Zero,
                        35, Color.Magenta, Projectile.rotation, Color.DarkMagenta, true);
                    Vector2 normalize = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    for (int a = 0; a < 25; a++)
                    {
                        Vector2 vel = normalize.RotatedByRandom(.4f) * Main.rand.NextFloat(20f, 35f);
                        float size = Main.rand.NextFloat(.9f, 1.2f);
                        int life = Main.rand.Next(15, 30);
                        ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * Main.rand.NextFloat(.5f, 1f), life, size, Color.Magenta.Lerp(Color.Cyan, Main.rand.NextFloat(.3f, .9f)), 1f, true);
                        ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.1f) * Main.rand.NextFloat(.25f, .75f), life * 2, size, Color.Violet * .7f);
                    }

                    Projectile.NewProj(pos, normalize * 40f, ModContent.ProjectileType<SewingNeedle>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.identity, 0f, 0f);

                    Time = TimeForShoot;
                }
            }
        }
    }

    public void Shotgun(int amount)
    {
        Magic(amount * 2, 1.1f);
        AdditionsSound.Laser4.Play(Projectile.Center, .7f, -.5f, .1f, 10);

        for (int i = 0; i < amount; i++)
        {
            Vector2 normalize = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 vel = normalize.RotatedByRandom(.6f);
            vel *= 27f - Main.rand.NextFloat(10f);
            Vector2 pos = Projectile.Center + Offset * (-30 + 1f * 10);

            for (int a = 0; a < 20; a++)
            {
                float size = Main.rand.NextFloat(.5f, .75f);
                int life = Main.rand.Next(18, 30);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * Main.rand.NextFloat(.3f, 1f), life, size, Color.Fuchsia, .7f);
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.14f) * Main.rand.NextFloat(.1f, .5f), life * 2, size, Color.Violet);
            }

            int damage = (int)(Projectile.damage / amount * Main.rand.NextFloat(1f, 1.5f));
            Projectile.NewProj(pos, vel, ModContent.ProjectileType<ConcentratedEnergy>(), damage, Projectile.knockBack, Projectile.owner, 0f, 0f, 0f);
        }
    }

    public void Magic(int amount, float velocityMultiplier)
    {
        for (int i = 0; i < amount; i++)
        {
            float dustSpeed = MathHelper.Lerp(3.5f, 8f, ChargeupCompletion) * Main.rand.NextFloat(0.65f, 1f);
            float dustSpawnOffsetFactor = Main.rand.NextFloat(Projectile.width * 0.375f, Projectile.width * 0.485f);
            Vector2 dustVelocity = Projectile.velocity * dustSpeed;
            Vector2 dustSpawnOffset = Main.rand.NextVector2CircularEdge(0.5f, 1f).RotatedBy((double)Projectile.velocity.ToRotation(), default) * dustSpawnOffsetFactor;
            ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center + dustSpawnOffset, dustVelocity * velocityMultiplier, 40, Main.rand.NextFloat(.45f, .55f), Color.Magenta.Lerp(Color.SlateBlue, Main.rand.NextFloat(.5f)), 1f, 4f);
        }
    }

    private static float RangeLerp(float input, float start, float end)
    {
        if (input < start)
            return 0;

        return Animators.BezierEase(InverseLerp(start, end, input));
    }

    public Vector2 Offset => Vector2.UnitX.RotatedBy(Projectile.rotation);
    private void DrawRing(SpriteBatch sb, Vector2 pos, float w, float h, Color end, float prog, float rot)
    {
        Texture2D outerCircleTexture = Projectile.ThisProjectileTexture();
        Color startingColor = Color.Magenta;

        ManagedShader effect = ShaderRegistry.MagicRing;
        effect.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes2), 1, SamplerState.LinearWrap);
        effect.TrySetParameter("firstCol", startingColor.ToVector3());
        effect.TrySetParameter("secondCol", end.ToVector3());
        effect.TrySetParameter("time", rot);
        effect.TrySetParameter("cosine", (float)Math.Cos(rot));
        effect.TrySetParameter("opacity", prog);

        sb.End();
        sb.Begin(default, BlendState.Additive, Main.DefaultSamplerState, default, RasterizerState.CullNone, effect.Effect, Main.GameViewMatrix.TransformationMatrix);

        Rectangle target = ToTarget(pos, (int)(26 * (w + prog)), (int)(60 * (h + prog)));
        sb.Draw(outerCircleTexture, target, null, end * prog, Projectile.velocity.ToRotation(), outerCircleTexture.Size() / 2, 0, 0);

        sb.End();
        sb.Begin(default, BlendState.AlphaBlend, Main.DefaultSamplerState, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        SpriteBatch sb = Main.spriteBatch;
        var color1 = Color.Magenta;
        var color2 = Color.DarkMagenta;
        var color3 = Color.DarkMagenta * .7f;

        float prog1 = RangeLerp(Interpolant, 0, 0.3f) + (float)Math.Sin(Main.GameUpdateCount / 20f) * 0.1f;
        float prog2 = RangeLerp(Interpolant, 0.3f, 0.6f) + (float)Math.Sin(Main.GameUpdateCount / 20f + 1) * 0.1f;
        float prog3 = RangeLerp(Interpolant, 0.6f, 0.9f) + (float)Math.Sin(Main.GameUpdateCount / 20f + 2) * 0.1f;

        DrawRing(sb, Projectile.Center + Offset * (-30 + prog1 * 10), 2.5f, 2.5f, color3, prog1, Main.GameUpdateCount / 40f);
        DrawRing(sb, Projectile.Center + Offset * (-30 + prog2 * 50), 3f, 3f, color2, prog2, -Main.GameUpdateCount / 30f);
        DrawRing(sb, Projectile.Center + Offset * (-30 + prog3 * 100), 4.2f, 4.2f, color1, prog3, Main.GameUpdateCount / 20f);

        return false;
    }
}