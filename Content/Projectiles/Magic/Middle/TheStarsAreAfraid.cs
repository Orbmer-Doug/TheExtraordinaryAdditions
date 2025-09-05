using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class TheStarsAreAfraid : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheStarsAreAfraid);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1200;
        ProjectileID.Sets.CanHitPastShimmer[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 120;
        Projectile.height = 14;
        Projectile.timeLeft = 400;
        Projectile.penetrate = 7;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 25;
    }
    public ref float Time => ref Projectile.ai[0];
    public ref float TypeOf => ref Projectile.ai[1];
    public bool Released
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float ReleaseTime => ref Projectile.Additions().ExtraAI[0];
    public int ProjIndex
    {
        get => (int)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = value;
    }

    public static readonly float ChargeTime = SecondsToFrames(1.5f);
    public float FadeOut => InverseLerp(0f, 50f, Projectile.timeLeft);
    public float ChargeCompletion => Animators.MakePoly(3).OutFunction(InverseLerp(0f, ChargeTime, Time)) * (1f - InverseLerp(0f, 30f, ReleaseTime));
    public float AmtCompletion => InverseLerp(0f, 4f, TypeOf);
    public override void AI()
    {
        Projectile ownerProj = Main.projectile[ProjIndex] ?? null;
        if (Owner == null || !Owner.active || ((ownerProj == null || !ownerProj.active) && !Released) || !Main.bloodMoon)
        {
            Projectile.Kill();
            return;
        }

        if (tele == null || tele._disposed)
            tele = new((c) => Projectile.height / 4 * ChargeCompletion, (c, pos) => Color.Crimson * MathHelper.SmoothStep(1f, 0f, c.X) * ChargeCompletion, null, 20);
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);

        Projectile.FacingRight();
        Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * 1.8f * Projectile.scale * FadeOut);

        if (Released)
        {
            if (ReleaseTime == 0f)
            {
                Vector2 pos = Projectile.RotHitbox().Left;
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.4f, 1f) * 50f, Vector2.Zero, 40, Color.Crimson, Projectile.rotation);

                for (int i = 0; i < 8; i++)
                    ParticleRegistry.SpawnBloomLineParticle(pos, Projectile.velocity.RotatedByRandom(.35f) * Main.rand.NextFloat(2f, 10f),
                        Main.rand.Next(40, 50), Main.rand.NextFloat(.4f, .6f), Color.Crimson);

                for (int i = 0; i < 14; i++)
                    ParticleRegistry.SpawnBloomPixelParticle(pos, Projectile.velocity.RotatedByRandom(.45f) * Main.rand.NextFloat(4f, 14f),
                        Main.rand.Next(30, 50), Main.rand.NextFloat(.5f, .9f), Color.Crimson, Color.DarkRed, null, 1.2f, 4);

                if (this.RunLocal())
                {
                    Projectile.velocity = Projectile.SafeDirectionTo(ModdedOwner.mouseWorld) * 12f;
                    this.Sync();
                }
                Projectile.extraUpdates = 2;
                this.Sync();
            }

            if (Projectile.numHits > 0)
            {
                NPC target = Main.npc[(int)NPCType];

                if (!target.active)
                {
                    if (Projectile.timeLeft > 5)
                        Projectile.timeLeft = 5;

                    Projectile.velocity = Vector2.Zero;
                }
                else
                {
                    Projectile.position = target.position + Offset;
                    if (Projectile.position != Projectile.oldPosition)
                        this.Sync();
                }
            }

            cache ??= new(30);
            cache.Update(Projectile.RotHitbox().Left);

            Projectile.Opacity = FadeOut;
            ReleaseTime++;
        }
        else
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Projectile.Center.SafeDirectionTo(ModdedOwner.mouseWorld);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true)
                + PolarVector(300f * ChargeCompletion, (MathHelper.TwoPi * AmtCompletion) + ownerProj.Additions().ExtraAI[0]);

            teleCache ??= new(20);
            teleCache.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 500f * ChargeCompletion, 20));

            Projectile.timeLeft = 400;

            Projectile.scale = InverseLerp(0f, 40f, Time);

            if (Time % 3 == 2)
            {
                Vector2 pos = Vector2.Lerp(Projectile.RotHitbox().Left, Projectile.RotHitbox().Right, Main.rand.NextFloat());
                int life = Main.rand.Next(20, 30);
                float scale = Main.rand.NextFloat(20f, 30f) * Projectile.scale;
                float opac = Main.rand.NextFloat(.6f, .8f);
                ParticleRegistry.SpawnCloudParticle(pos, Main.rand.NextVector2Circular(3f, 3f) + Owner.velocity, Color.Crimson, Color.DarkRed, life, scale, opac, 2);
            }
        }

        Time++;
    }
    public override bool? CanHitNPC(NPC target)
    {
        if (Projectile.penetrate <= 1 || !Released)
            return false;
        return null;
    }
    public override bool ShouldUpdatePosition() => Released;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (target.type == ModContent.NPCType<StygainHeart>() || target.type >= NPCID.MoonLordHead && target.type <= NPCID.MoonLordLeechBlob)
        {
            modifiers.FinalDamage *= 1.7895f;
        }
    }
    public ref float NPCType => ref Projectile.Additions().ExtraAI[2];
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.MimicryLand.Play(target.Center, 1.2f, .1f);
        for (int i = 0; i < 6; i++)
        {
            Vector2 pos = Projectile.RotHitbox().Right;
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.35f) * Main.rand.NextFloat(.2f, .8f);
            int life = Main.rand.Next(30, 80);
            float scale = Main.rand.NextFloat(.5f, .8f);
            Color color = Color.DarkRed.Lerp(Color.Crimson, Main.rand.NextFloat(.4f, .6f)) * Main.rand.NextFloat(.75f, 1f);
            ParticleRegistry.SpawnBloodParticle(pos, vel, life, scale, color);
        }

        Projectile.damage = (int)(Projectile.damage * .9f);
        Projectile.velocity *= .9f;

        if (Projectile.numHits <= 0)
        {
            NPCType = target.whoAmI;
            Offset = Projectile.position - target.position;
            Offset -= Projectile.velocity;
        }
        else
            Offset += Projectile.velocity * .9f;

        target.AddBuff(BuffID.Bleeding, 500);
        target.AddBuff(BuffID.Slow, 500);
        target.AddBuff(BuffID.Darkness, 500);
        target.AddBuff(BuffID.Blackout, 500);
    }
    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return Color.Crimson * MathHelper.SmoothStep(1f, 0f, completionRatio.X) * FadeOut;
    }
    internal float WidthFunction(float completionRatio)
    {
        return Projectile.height * MathHelper.SmoothStep(0.6f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true)) * FadeOut;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size(), null, Projectile.scale, 10f);
    }

    public OptimizedPrimitiveTrail trail;
    public OptimizedPrimitiveTrail tele;
    public TrailPoints cache;
    public ManualTrailPoints teleCache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (Released && trail != null && cache != null)
            {
                ManagedShader shader = ShaderRegistry.BloodBeacon;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap1), 1);
                trail.DrawTrail(shader, cache.Points, 40);
            }

            if (tele != null && teleCache != null)
                tele.DrawTrail(ShaderRegistry.StandardPrimitiveShader, teleCache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * 0.5f;

        Projectile.DrawProjectileBackglow(Color.Red, 2f + (Cos01(Main.GlobalTimeWrappedHourly) * 3.5f), 72, 6);
        Main.spriteBatch.Draw(texture, drawPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0f);
        return false;
    }
}
