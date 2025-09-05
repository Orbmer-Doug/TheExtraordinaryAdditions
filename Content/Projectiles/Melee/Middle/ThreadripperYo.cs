using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
public class ThreadripperYo : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Threadripper);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public ref float Timer => ref Projectile.Additions().ExtraAI[4];
    public ref float Shred => ref Projectile.Additions().ExtraAI[5];
    public ref float Wait => ref Projectile.Additions().ExtraAI[6];
    public ref int Counter => ref Modded.RipperCounter;
    public const int Max = 25;
    public static readonly int ShredTime = SecondsToFrames(6.5f);
    public float Interpol => Shred > 0 ? InverseLerp(0f, ShredTime, Shred) : InverseLerp(0f, Max, Counter);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = -1f;
        ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 395f;
        ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 14f;
    }

    public override void SetDefaults()
    {
        Projectile.aiStyle = 99;
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = -1;
        Projectile.MaxUpdates = 3;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 18;
    }

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, 0, 245));

        if (Projectile.FinalExtraUpdate())
        {
            Projectile.rotation += .5f;
            Vector2 distance = Projectile.position - Main.player[Projectile.owner].position;
            if (distance.Length() > 3200f)
            {
                Projectile.Kill();
            }

            if (Shred > 0)
            {
                if (Timer % 6 == 5)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = Projectile.Center;
                        Vector2 vel = Main.rand.NextVector2CircularEdge(20, 20);
                        int life = Main.rand.Next(30, 80);
                        float speedReduce = Main.rand.NextFloat(.5f, .7f);
                        float scale = Main.rand.NextFloat(.5f, 1.2f);
                        Color color = Color.Lerp(Color.DarkRed, Color.Crimson, Main.rand.NextFloat(.4f, .6f)) * 0.75f;
                        ParticleRegistry.SpawnBloodParticle(pos, vel * speedReduce * MathHelper.Clamp(Interpol, .2f, 1f), life, scale * Interpol, color);
                    }
                }

                Shred--;
            }

            if (Counter > 0 || Shred > 0)
            {
                if (Main.rand.NextBool(Max - Counter) || Shred > 0)
                {
                    Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(Projectile.height, Projectile.height);
                    Vector2 vel = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * (4f + Projectile.velocity.Length() * Main.rand.NextFloat(.2f, .5f));
                    int life = Main.rand.Next(23, 38);
                    float scale = Main.rand.NextFloat(12f, 30f) * Interpol;
                    ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale, Color.Red, Main.rand.NextFloat(.7f, 1.3f));
                }
            }

            if (Wait > 0)
                Wait--;
            Timer++;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Shred > 0)
        {
            modifiers.ScalingArmorPenetration += .5f;
            modifiers.FinalDamage *= 2;
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Vector2 orig = tex.Size() / 2f;
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Vector2.One * Projectile.height * 4f), null, Color.DarkRed * .6f * Interpol, 0f, orig);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Vector2.One * Projectile.height * 3.5f), null, Color.Red * .85f * Interpol, 0f, orig);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Vector2.One * Projectile.height * 3f), null, Color.Crimson * Interpol, 0f, orig);
        }

        Texture2D tex = Projectile.ThisProjectileTexture();
        Point p = Projectile.Center.ToTileCoordinates();
        float light = Lighting.Brightness(p.X, p.Y);
        Color col = Color.LightGray.Lerp(Color.DarkRed, Interpol);
        after.DrawFancyAfterimages(tex, [col * .76f], light, 1f, 0f, false, true);
        Projectile.DrawBaseProjectile(col * light);
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // sawblade vs. ground = friction
        if (Timer % 4f == 3f)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity * 2, Projectile.width, Projectile.height);

            Vector2 splatterDirection = Projectile.SafeDirectionTo(Owner.Center) * 8f + Projectile.velocity * 1.2f;

            int life = Main.rand.Next(55, 70);
            float scale = Utils.NextFloat(Main.rand, 1.7f, Utils.NextFloat(Main.rand, 1.3f, 2.2f)) * 0.85f;
            Color col = Color.Lerp(Color.DarkOrange, Color.Orange * 1.2f, Utils.NextFloat(Main.rand, 0.7f));
            col = Color.Lerp(col, Color.OrangeRed, Utils.NextFloat(Main.rand));
            Vector2 vel = Utils.RotatedByRandom(splatterDirection, 0.9) * Utils.NextFloat(Main.rand, .5f, 1.2f);

            ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel, life, scale, col, true, true);
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, vel * 1.4f, life, scale * 1.4f, col * 1.6f, col * 2.4f, 4, true, true);
        }
        Projectile.velocity *= .34f;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Wait <= 0 && Shred <= 0)
        {
            Owner.Additions().RipperCounter += 1;
            Wait = 30;
        }
        if (Owner.Additions().RipperCounter >= Max)
        {
            Shred = ShredTime;
            Owner.Additions().RipperCounter = 0;
        }

        Vector2 splatterDirection = Projectile.velocity * .8f;
        for (int i = 0; i < 3; i++)
        {
            int life = Main.rand.Next(65, 80);
            float scale = Utils.NextFloat(Main.rand, 1.7f, Utils.NextFloat(Main.rand, 1.3f, 2.2f)) * 0.85f;
            Color color = Color.Lerp(Color.DarkRed, Color.Crimson, Main.rand.NextFloat(.4f, .6f)) * 0.75f;
            Vector2 vel = Utils.RotatedByRandom(splatterDirection, 0.699) * Utils.NextFloat(Main.rand, .5f, 1.2f);
            ParticleRegistry.SpawnBloodParticle(target.Center, vel, life, scale, color);
        }
    }
}