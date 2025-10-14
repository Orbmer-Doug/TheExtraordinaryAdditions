using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class EtherealSwing : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EtherealClaymore);
    public ref float Rotation => ref Projectile.ai[0];
    public ref float Spinup => ref Projectile.ai[1];
    public ref float FadeTimer => ref Projectile.AdditionsInfo().ExtraAI[0];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 7;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.extraUpdates = 3;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.noEnchantmentVisuals = true;
        Projectile.netImportant = true;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    private const int SwingTime = 100;
    private const int FadeTime = 60;
    private const int X = 252;
    private const int Y = 60;

    public float FadeCompletion => InverseLerp(FadeTime, 0f, FadeTimer);
    public float GetRotation() => Rotation % SwingTime / SwingTime * MathHelper.TwoPi;
    public Vector2 GetOffset(float xOff = 1f, float yOff = 1f) => GetPointOnRotatedEllipse(xOff * 2f, yOff * 2.2f, Projectile.rotation, -GetRotation());
    public Quaternion Get3DRotation() => EulerAnglesConversion(1, -GetRotation(), 1.2f);

    public int Dir => (MathF.Cos(Projectile.rotation) > 0f).ToDirectionInt();
    public float ZInfluence => GetCircularSectionValue(Rotation % SwingTime / SwingTime * MathHelper.TwoPi, Dir == -1 ? 2f : .5f, 1f, Dir == -1 ? .5f : 2f);

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 start = Projectile.Center;
        Vector2 end = start + GetOffset(X + 20, Y + 20);
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end);
    }

    public override void CutTiles()
    {
        Vector2 start = Projectile.Center;
        Vector2 end = start + GetOffset(X + 20, Y + 20);
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(start, end, start.Distance(end) / 2, DelegateMethods.CutTiles);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((float)Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.rotation = (float)reader.ReadSingle();
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 50);

        float attackSpeedMult = Owner.GetAttackSpeed<MeleeDamageClass>();
        float maxSpinup = 300f * attackSpeedMult;

        if (Spinup < maxSpinup)
            Spinup += attackSpeedMult;

        if (Spinup > maxSpinup)
            Spinup = maxSpinup;

        Rotation += 0.4f + Spinup * 0.0025f * FadeCompletion;

        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

        if (this.RunLocal())
        {
            float target = Owner.MountedCenter.AngleTo(Modded.mouseWorld);
            Projectile.rotation = Projectile.rotation.AngleLerp(target, .005f);
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }
        Projectile.Opacity = FadeCompletion;
        bool active = Owner.HeldItem.type == ModContent.ItemType<EtherealClaymore>() && !Owner.noItems;

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && active && FadeTimer <= 0)
        {
            Projectile.timeLeft = FadeTime + 10;
        }
        else
        {
            FadeTimer++;

            if (FadeTimer >= FadeTime)
            {
                Projectile.Kill();
            }
        }

        // visuals
        float rot = GetRotation();
        Vector2 off = GetOffset(X, Y);
        cache ??= new(50);
        cache.Update(Projectile.Center + off);

        // Draw behind the player when halfway swinging
        if (rot > MathHelper.Pi)
            Owner.heldProj = Projectile.whoAmI;
        if (!this.RunLocal())
            Owner.heldProj = Projectile.whoAmI;

        Owner.SetFrontHandBetter(0, Projectile.Center.AngleTo(Projectile.Center + off));
        Owner.ChangeDir((Owner.Center.X + off.X > Owner.Center.X).ToDirectionInt());

        if (Projectile.soundDelay == 0)
        {
            SoundEngine.PlaySound(SoundID.Item7 with { PitchVariance = .2f, Volume = 1.4f }, Projectile.Center);
            Projectile.soundDelay = 120 - (int)(Spinup * .2f);
        }

        if (Main.rand.NextBool(3))
        {
            Vector2 pos = Owner.Center + off;
            Vector2 vel = off * Main.rand.NextFloat(0.04f);
            int life = Main.rand.Next(40, 50);
            float scale = Main.rand.NextFloat(.2f, .5f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale * ZInfluence, Color.AliceBlue, Color.DeepSkyBlue, null, 1.8f, 5);
        }

        if (Main.rand.NextBool((int)Math.Round(25.0 / attackSpeedMult)))
        {
            Vector2 pos = Owner.Center + off;
            Vector2 vel = off * Main.rand.NextFloat(0.1f);
            Color col = Color.CornflowerBlue;
            int life = Main.rand.Next(15, 30);
            float scale = Main.rand.NextFloat(.4f, .6f);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, life, scale * ZInfluence, col, Color.DeepSkyBlue, 1.4f);
        }

        Lighting.AddLight(Projectile.Center + off, new Color(122, 173, 255).ToVector3() * 1.1f * ZInfluence);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 dir = cache.Points[1].SafeDirectionTo(cache.Points[0]);
        target.velocity += dir * 12 * target.knockBackResist;

        Vector2 pos = target.Center;

        AdditionsSound.etherealHit4.Play(pos, 1.4f, 0f, .15f);

        for (int i = 0; i < 33; i++)
        {
            ParticleRegistry.SpawnGlowParticle(pos, dir.RotatedByRandom(.17f) * Main.rand.NextFloat(5f, 14f), Main.rand.Next(24, 36), Main.rand.NextFloat(32f, 40.8f), Color.AliceBlue, 2f);
        }
    }

    public override bool ShouldUpdatePosition() => false;
    private float WidthFunct(float c) => OptimizedPrimitiveTrail.HemisphereWidthFunct(c, MathHelper.SmoothStep(1f, 0f, c) * 40f * ZInfluence);
    private Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        if (c.X == 0f)
            return Color.Transparent;

        return new Color(140, 90 + (int)(100 * (1f - c.X)), 255) * (1f - c.X) * FadeCompletion;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null || cache.Points.IsEmpty || !this.RunLocal())
                return;

            ManagedShader effect = ShaderRegistry.EnlightenedBeam;
            effect.TrySetParameter("repeats", 4f);
            effect.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
            trail.DrawTrail(effect, cache.Points, 350, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D tex = Projectile.ThisProjectileTexture();
        DrawTextureIn3D(tex, Projectile.Center, Get3DRotation(), 1.2f, Projectile.rotation, Color.White * Projectile.Opacity);
        return false;
    }
}