using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class Comet : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(1);
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.ignoreWater = Projectile.friendly = Projectile.usesLocalNPCImmunity = Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = 2;
        Projectile.extraUpdates = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 300;
    }

    public Player Owner => Main.player[Projectile.owner];
    public static readonly int FadeTime = SecondsToFrames(.7f);

    public int HitTime
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ref float WidthInterpolant => ref Projectile.ai[1];

    public bool Init
    {
        get => Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float StoredY => ref Projectile.AdditionsInfo().ExtraAI[0];

    public bool HitGround
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((float)Projectile.scale);
        writer.Write((int)Projectile.width);
        writer.Write((int)Projectile.height);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.scale = (float)reader.ReadSingle();
        Projectile.width = (int)reader.ReadInt32();
        Projectile.height = (int)reader.ReadInt32();
    }

    public override void AI()
    {
        if (!Init && this.RunLocal())
        {
            StoredY = Owner.Additions().mouseWorld.Y;
            WidthInterpolant = 1f;
            Projectile.scale = Main.rand.NextFloat(.4f, 1f);
            Projectile.Size = new(50f * Projectile.scale);
            Init = true;
            this.Sync();
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 30);
        points.Update(Projectile.Center + Projectile.velocity);

        if (Projectile.Center.Y > StoredY && !Projectile.tileCollide)
            Projectile.tileCollide = true;

        if (HitGround || Projectile.penetrate <= 0)
        {
            Projectile.velocity *= .94f;
            float lerp = InverseLerp(FadeTime, 0f, HitTime);
            WidthInterpolant = lerp;
            Projectile.Opacity = Animators.MakePoly(3).InFunction(lerp);

            if (lerp <= 0f && points.Points.AllPointsEqual())
                Projectile.Kill();
            HitTime++;
        }
        else
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 rot = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * i);
                Vector2 vel = rot * 0.33f + Projectile.velocity / 4f;
                Vector2 posit = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f + rot;
                ParticleRegistry.SpawnDustParticle(posit, vel * 1.4f, Main.rand.Next(10, 18), Projectile.scale * .7f, Color.Cyan, .1f, false, true, false, false);
            }
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            for (int i = 0; i < 25; i++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f);
                Vector2 vel = -oldVelocity.RotatedByRandom(Main.rand.NextFloat(.4f, .6f)) * Main.rand.NextFloat(.3f, 1f);

                ParticleRegistry.SpawnGlowParticle(pos, vel * Main.rand.NextFloat(1.28f, 1.5f), 20, Main.rand.NextFloat(.9f, 1.4f) * Projectile.scale, Color.Cyan);
                ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(120, 150), Main.rand.NextFloat(.7f, 1.2f), Color.White.Lerp(Color.DeepSkyBlue, Main.rand.NextFloat(.1f, .9f)), true, true);
            }
            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CometBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.scale);
            AdditionsSound.etherealSlam.Play(Projectile.Center, .6f, -Projectile.scale * .18f, .06f, 50);

            Projectile.velocity *= 0f;
            Projectile.extraUpdates = 0;

            HitGround = true;
        }
        return false;
    }

    public override bool? CanDamage() => Projectile.velocity != Vector2.Zero;
    public override bool? CanCutTiles() => Projectile.velocity != Vector2.Zero;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * .5f);
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CometBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.scale);
    }

    public float WidthFunct(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(2f).InFunction(InverseLerp(0.3f, 0f, c)));
        float width = InverseLerp(1f, 0.4f, c) * tipInterpolant;
        return (width * Projectile.width * 4f) * WidthInterpolant;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color col = new Color(20 + (Projectile.identity % byte.MaxValue), 50 + (int)(60 * c.X), 255);
        return col * Projectile.Opacity;
    }

    public TrailPoints points = new(30);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;
            ManagedShader w = ShaderRegistry.PierceTrailShader;
            trail.DrawTrail(w, points.Points, 140);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}