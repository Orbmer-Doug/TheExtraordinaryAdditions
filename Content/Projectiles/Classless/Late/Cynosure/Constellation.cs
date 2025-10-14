using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class Constellation : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public const int Lifetime = 150;
    public const int FadeTime = 20;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = Projectile.hostile = false;
        Projectile.ignoreWater = Projectile.friendly = Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool MainProj
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public List<Projectile> Others = [];
    public ref float NextProjIndex => ref Projectile.ai[2];

    public ref float InitOpac => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float InitScale => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float Spin => ref Projectile.AdditionsInfo().ExtraAI[2];
    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.Opacity = 0f;
            if (!MainProj)
            {
                const float MinDistance = 30f;
                const int MaxAttempts = 10;

                Others.Add(Projectile);

                int count = Main.rand.Next(5, 10);

                for (int i = 0; i < count; i++)
                {
                    Vector2 newPosition = Vector2.Zero;
                    bool validPosition = false;
                    int attempts = 0;

                    // Attempt to not spawn too close together
                    while (attempts < MaxAttempts && !validPosition)
                    {
                        // Make the position
                        newPosition = Projectile.Center + PolarVector(Main.rand.NextFloat(100f, 400f), RandomRotation());
                        validPosition = true;

                        // Check distance to all the other stars
                        foreach (Projectile other in Others)
                        {
                            if (Vector2.Distance(newPosition, other.Center) < MinDistance)
                            {
                                validPosition = false;
                                break;
                            }
                        }
                        attempts++;
                    }

                    if (this.RunLocal() && validPosition)
                    {
                        int projIndex = Projectile.NewProj(newPosition, Main.rand.NextVector2Circular(.4f, .4f), Type, Projectile.damage, Projectile.knockBack, Main.myPlayer, 0f, 1f);
                        Projectile proj = Main.projectile[projIndex];
                        proj.As<Constellation>().Others = Others;
                        Others.Add(proj);
                    }
                }

                for (int i = 0; i < Others.Count; i++)
                {
                    // Connect each star to the next one in the list
                    if (i < Others.Count - 1)
                        Others[i].As<Constellation>().NextProjIndex = Others[i + 1].whoAmI;
                    else
                        // Last star connects to the first to form a loop
                        Others[i].As<Constellation>().NextProjIndex = Others[0].whoAmI;
                }
            }

            InitOpac = Main.rand.NextFloat(.8f, 1.2f);
            InitScale = Main.rand.NextFloat(.67f, 1.1f);
            Spin = Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(.05f, .01f);
            Projectile.netUpdate = true;
        }

        Projectile.Opacity = Animators.MakePoly(2f).OutFunction.Evaluate(Time, Lifetime, Lifetime - FadeTime, 0f, InitOpac) * InverseLerp(0f, 10f, Time);
        Projectile.scale = Animators.MakePoly(4f).InOutFunction.Evaluate(Time, Lifetime, Lifetime - FadeTime, 0f, InitScale);

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Others != null && NextProjIndex != 0)
        {
            Projectile nextProj = Main.projectile[(int)NextProjIndex];
            if (nextProj.active && nextProj.type == Type)
            {
                Vector2 pos = ClosestPointOnLineSegment(target.Center, Projectile.Center, nextProj.Center);
                for (int i = 0; i < 12; i++)
                {
                    ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 20, Main.rand.NextFloat(60f, 90f), Color.White, 1.2f);
                    ParticleRegistry.SpawnBloomPixelParticle(pos, Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(30, 50), Main.rand.NextFloat(.6f, .9f), Color.White, Color.AntiqueWhite);
                }
            }
        }
        Owner.Heal(Main.rand.Next(3, 7));
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Others != null && NextProjIndex != 0)
        {
            Projectile nextProj = Main.projectile[(int)NextProjIndex];
            if (nextProj.active && nextProj.type == Type)
                return targetHitbox.LineCollision(Projectile.Center, nextProj.Center, 8f);
        }

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (Others != null && NextProjIndex != 0)
            {
                Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);

                Vector2 start = Projectile.Center;

                // Draw a line to the next projectile in the chain
                Projectile nextProj = Main.projectile[(int)NextProjIndex];
                if (nextProj.active && nextProj.type == Type)
                {
                    Vector2 end = nextProj.Center;
                    Vector2 tangent = start.SafeDirectionTo(end) * start.Distance(end);
                    float rotation = tangent.ToRotation();
                    const float ImageThickness = 8;
                    float thicknessScale = 1f / ImageThickness;
                    Vector2 middleOrigin = new(0, horiz.Height / 2f);
                    Vector2 middleScale = new(start.Distance(end) / horiz.Width, thicknessScale);
                    Color col = Color.White * Animators.MakePoly(2f).OutFunction.Evaluate(Time, Lifetime, Lifetime - FadeTime, 0f, 1f) * InverseLerp(0f, 10f, Time);
                    Main.spriteBatch.DrawBetter(horiz, start, null, col, rotation, middleOrigin, middleScale, SpriteEffects.None);
                }
            }

            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.Sparkle);
            Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

            Main.spriteBatch.DrawBetterRect(star, ToTarget(Projectile.Center, Vector2.One * 50f * Projectile.scale), null, Color.White * Projectile.Opacity, Time * Spin, star.Size() / 2f);
            Main.spriteBatch.DrawBetterRect(bloom, ToTarget(Projectile.Center, Vector2.One * 80f * Projectile.scale), null, Color.White * Projectile.Opacity * .5f, 0f, bloom.Size() / 2f);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
        return false;
    }
}