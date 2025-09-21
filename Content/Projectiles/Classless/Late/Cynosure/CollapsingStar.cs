using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class CollapsingStar : ModProjectile, ILocalizedModType, IModType, IHasScreenShader
{
    public override string Texture => AssetRegistry.Invis;

    public const float IdealScale = 750f;

    // Stars collapse really fast, in fact (outer wilds you lied to me)
    public static readonly int CollapseTime = SecondsToFrames(.45f);
    public static readonly float GrowTime = SecondsToFrames(5f);

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.alpha = 255;
        Projectile.penetrate = -1;
        Projectile.scale = 0f;
        Projectile.timeLeft = 90000;
        Projectile.tileCollide = false;
        Projectile.netImportant = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 7;
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2500;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.scale);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.scale = reader.ReadSingle();
    }

    public ref float GrowTimer => ref Projectile.ai[0];
    public const int FadeTime = 55;
    public ref float FadeTimer => ref Projectile.ai[1];
    public ref float OverallTimer => ref Projectile.Additions().ExtraAI[0];
    private bool IsCollapsing
    {
        get => Projectile.Additions().ExtraAI[1] == 1f;
        set => Projectile.Additions().ExtraAI[1] = value.ToInt();
    }
    public ref float OldScale => ref Projectile.Additions().ExtraAI[2];
    public Projectile Genedies => Main.projectile[(int)Projectile.Additions().ExtraAI[3]];

    public LoopedSoundInstance slot;
    public override void AI()
    {
        slot ??= LoopedSoundManager.CreateNew(new(AdditionsSound.sunAura, () => Utils.Remap(GrowTimer, 0f, GrowTime, 0f, 1.1f)), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        slot?.Update(Projectile.Center);

        TheExingendies gen = Genedies.As<TheExingendies>();
        bool available = gen != null && gen.Projectile.active && Genedies.active && Genedies.owner == Projectile.owner;
        if ((!available || gen?.Phase == TheExingendies.States.ActiveGalacticNucleus || ModdedOwner.MouseRight.JustPressed) && FadeTimer <= 0f)
        {
            Projectile.timeLeft = FadeTime;
            FadeTimer++;
        }

        if (IsCollapsing)
        {
            Projectile.scale = Utils.Remap(Projectile.timeLeft, 0f, CollapseTime, 0f, IdealScale);
        }
        else if (FadeTimer > 0f)
        {
            float comp = InverseLerp(0f, FadeTime, FadeTimer);
            if (available)
            {
                Vector2 dest = Genedies.Center;
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, dest, Animators.CubicBezier(.59f, .03f, .03f, .95f)(comp));
            }
            else
                Projectile.velocity = Vector2.Zero;

            Projectile.scale = Animators.MakePoly(2f).OutFunction.Evaluate(OldScale, 0f, comp);

            FadeTimer++;
        }
        else
        {
            if (this.RunLocal() && AdditionsKeybinds.MiscHotKey.Current && AdditionsKeybinds.SetBonusHotKey.Current && Projectile.scale >= IdealScale)
            {
                Projectile.timeLeft = CollapseTime;
                IsCollapsing = true;
                this.Sync();
            }

            if (this.RunLocal())
            {
                Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter);
                Vector2 dest = center + PolarVector(Projectile.width * .45f, center.AngleTo(ModdedOwner.mouseWorld));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(dest) * MathF.Min(Projectile.Distance(dest), 20f), 0.07f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }

            Projectile.scale = Utils.Remap(GrowTimer, 0f, GrowTime, 0f, IdealScale);
            OldScale = Projectile.scale;
        }

        Projectile.ExpandHitboxBy((int)Projectile.scale);

        UpdateFlares();

        if (GrowTimer < GrowTime)
            GrowTimer++;

        OverallTimer++;
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return base.PreKill(timeLeft);
    }

    public override void OnKill(int timeLeft)
    {
        // KABLOOEY
        if (IsCollapsing == true)
        {
            AdditionsSound.MomentOfCreation.Play(Projectile.Center, 4.6f, -.2f);

            ScreenShakeSystem.New(new(20f, 6.5f, KilonovaShockwave.MaxRadius), Projectile.Center);
            ParticleRegistry.SpawnFlash(Projectile.Center, 12, 5.4f, KilonovaShockwave.MaxRadius);
            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, 50, .4f, KilonovaShockwave.MaxRadius / 2);
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, 120, .6f, 1200f);

            Vector2 pos = Projectile.Center;
            int dmg = Projectile.damage * 4;
            float kb = 0f;
            int type = ModContent.ProjectileType<BlackHole>();
            int owner = Projectile.owner;
            Projectile.NewProj(pos, Vector2.Zero, type, dmg, kb, owner);

            const int amt = 500;
            const int blastAmt = 30;
            const int maxRad = KilonovaShockwave.MaxRadius;
            float off = RandomRotation();
            for (int i = 1; i <= amt; i++)
            {
                float lerp = InverseLerp(0f, amt, i);
                Color col = MulticolorLerp(lerp, Color.White, Color.White.Lerp(Color.Cyan, .5f), Color.Cyan, Color.SkyBlue, Color.DeepSkyBlue); ;
                Color col2 = col * 1.8f;

                Vector2 vel = (MathHelper.TwoPi * lerp + off).ToRotationVector2() * Main.rand.NextFloat(5f, 35f);

                if (i < blastAmt)
                {
                    float blastComp = InverseLerp(0f, blastAmt, i);
                    int blastLife = 40 + (i * 5);
                    float blastScale = Utils.MultiLerp(blastComp, maxRad / 4, maxRad / 3, maxRad / 2, maxRad);
                    ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new(blastScale), Vector2.Zero, blastLife, col * .6f, null, null, false);
                }

                ParticleRegistry.SpawnGlowParticle(pos, Main.rand.NextVector2Circular(100f, 100f), Main.rand.Next(90, 120), Main.rand.NextFloat(90f, 120f), col, Main.rand.NextFloat(1f, 2f), false);
                ParticleRegistry.SpawnGlowParticle(pos, Main.rand.NextVector2Circular(60f, 60f), Main.rand.Next(150, 180), Main.rand.NextFloat(90f, 100f), col, Main.rand.NextFloat(2f, 4f), false);
                ParticleRegistry.SpawnSquishyLightParticle(pos, vel, Main.rand.Next(40, 60), Main.rand.NextFloat(.6f, 1.4f), col2, 2f, 1.4f);

                ParticleRegistry.SpawnHeavySmokeParticle(pos, Main.rand.NextVector2Circular(20f, 20f), Main.rand.Next(40, 50), Main.rand.NextFloat(2f, 5f), col2.Lerp(col, .5f), 1.4f, true, Main.rand.NextFloat(-.1f, .1f));

                ParticleRegistry.SpawnCloudParticle(pos, vel * Main.rand.NextFloat(.5f, .9f), col, Color.Transparent, Main.rand.Next(120, 230), Main.rand.NextFloat(.8f, 1.5f), Main.rand.NextFloat(.8f, 1.6f), Main.rand.NextByte(0, 2));
                ParticleRegistry.SpawnCloudParticle(pos, vel * Main.rand.NextFloat(1.2f, 1.5f), col, Color.Transparent, Main.rand.Next(120, 230), Main.rand.NextFloat(.8f, 1.5f), Main.rand.NextFloat(.8f, 1.6f), Main.rand.NextByte(0, 2));

                ParticleRegistry.SpawnSparkParticle(pos, vel * 5f, Main.rand.Next(30, 40), Main.rand.NextFloat(5f, 8f), col2, false, false, null);
                ParticleRegistry.SpawnBloomLineParticle(pos, vel * 4f, Main.rand.Next(20, 30), Main.rand.NextFloat(1.8f, 5f), col2);
            }
            ParticleRegistry.SpawnShockwaveParticle(pos, 50, .8f, maxRad * 1.4f, 80f, .7f);

            Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<KilonovaShockwave>(), (int)Owner.GetTotalDamage(DamageClass.Generic).ApplyTo(150000), 50f, Projectile.owner);
            Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<Kilonova>(), 0, 0f, owner);
        }
    }

    public override bool? CanHitNPC(NPC target) => IsCollapsing ? false : null;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (SolarFlare flare in Flares)
        {
            if (flare == null || flare.Points == null || flare.Trail == null)
                continue;

            if (Utility.CollisionFromPoints(targetHitbox, flare.Points.Points, c => flare.Trail._widthFunction(c)))
                return null;
        }
        return base.Colliding(projHitbox, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!HasShader)
            InitializeShader();

        UpdateShader();

        DrawSun();
        DrawFlares();
        return false;
    }

    public Color SunColor()
    {
        float interpolant = InverseLerp(0f, GrowTime, GrowTimer);

        Color color = MulticolorLerp(interpolant, Color.IndianRed.Lerp(Color.Black, .5f), Color.Red,
            Color.Yellow.Lerp(Color.White, .5f), Color.Yellow,
            Color.Yellow.Lerp(Color.OrangeRed, .5f), Color.OrangeRed,
            Color.OrangeRed,
            Color.OrangeRed.Lerp(Color.DarkRed, .4f));

        return color;
    }

    public Color FlareColor()
    {
        float interpolant = InverseLerp(0f, GrowTime, GrowTimer);

        Color color = MulticolorLerp(interpolant, Color.IndianRed.Lerp(Color.Black, .5f), Color.Red,
            Color.Yellow.Lerp(Color.White, .5f), Color.Yellow,
            Color.Yellow.Lerp(Color.OrangeRed, .5f), Color.OrangeRed,
            Color.OrangeRed);

        return color;
    }

    public static Color SupernovaColor(float inc)
    {
        Color color = MulticolorLerp(inc, Color.LightBlue, Color.Cyan, Color.Yellow.Lerp(Color.Orange, .5f), Color.Red);
        return color;
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("HeatDistortionFilter");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Shader.TrySetParameter("intensity", InverseLerp(0f, GrowTime, GrowTimer));
        Shader.TrySetParameter("screenPos", GetTransformedScreenCoords(Projectile.Center));
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("radius", (1.8f * Projectile.scale) / Main.screenWidth);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("HeatDistortionFilter", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }
    public bool IsEntityActive() => Projectile.active;

    private void DrawSun()
    {
        ManagedShader shader = AssetRegistry.GetShader("StarShader");
        shader.TrySetParameter("globalTime", OverallTimer);
        shader.TrySetParameter("coreColor", SunColor());
        shader.TrySetParameter("coronaColor", SunColor() * 1.6f);
        shader.TrySetParameter("coolnessInterpolant", InverseLerp(0f, GrowTime / 4, GrowTimer));
        shader.TrySetParameter("redness", Utils.Remap(GrowTimer, GrowTime - 40, GrowTime, 1f, 1.5f));

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, shader.Effect, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise);
        shader.Render("AutoloadPass", false, false);
        Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(Projectile.scale)), null, Color.White, 0f, tex.Size() / 2f);
        Main.spriteBatch.ExitShaderRegion();
    }

    public List<SolarFlare> Flares = [];

    /// <summary>
    /// Defines a solar flare
    /// </summary>
    public sealed class SolarFlare(float offset, Vector2 dir, int lifetime, float distance)
    {
        /// <summary>
        /// The offset around the circle of the star
        /// </summary>
        public float Offset = offset;

        /// <summary>
        /// The first control point
        /// </summary>
        public Vector2 A;

        /// <summary>
        /// The central, expanding control point
        /// </summary>
        public Vector2 B;

        /// <summary>
        /// The last control point
        /// </summary>
        public Vector2 C;

        /// <summary>
        /// The direction to expand
        /// </summary>
        public Vector2 Direction = dir;

        /// <summary>
        /// The incrementing value
        /// </summary>
        public int Time = 0;

        /// <summary>
        /// The maximum time this flare should last for
        /// </summary>
        public int Lifetime = lifetime;

        /// <summary>
        /// How far to expand
        /// </summary>
        public float Distance = distance;

        /// <summary>
        /// The points for drawing
        /// </summary>
        public ManualTrailPoints Points;

        /// <summary>
        /// Created once during updates to optimize not having to create one every frame in drawing
        /// </summary>
        public OptimizedPrimitiveTrail Trail;
    }

    private void DrawFlares()
    {
        if (Flares.Count != 0)
        {
            foreach (SolarFlare flare in Flares)
            {
                void draw()
                {
                    ManagedShader prim = ShaderRegistry.FlameTrail;
                    prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
                    flare.Trail.DrawTrail(prim, flare.Points.Points, 100, true);
                }
                PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
            }
        }
    }

    public void UpdateFlares()
    {
        if (OverallTimer % 35 == 34 && !IsCollapsing && FadeTimer <= 0f)
        {
            float rot = RandomRotation();
            if (Main.rand.NextBool(9)) // Arcade structure
            {
                float rand = Main.rand.NextFloat(.75f, 1.15f);
                int life = Main.rand.Next(50, 80);
                float scale = Main.rand.NextFloat(520f, 850f);
                for (float i = .75f; i < 1.5f; i += .25f)
                {
                    Flares.Add(new(rand, PolarVector(1f, rot).RotatedByRandom(.2f), (int)(life * i), scale * i));
                }
            }
            else
            {
                float rand = Main.rand.NextFloat(.25f, .95f);
                Flares.Add(new(rand, PolarVector(1f, rot).RotatedByRandom(.7f), Main.rand.Next(50, 80), Main.rand.NextFloat(320f, 850f)));
            }
        }

        if (Flares.Count != 0)
        {
            for (int i = 0; i < Flares.Count; i++)
            {
                SolarFlare flare = Flares[i];
                float interpolant = InverseLerp(0f, flare.Lifetime, flare.Time);

                flare.Points ??= new(50);
                if (flare.Trail == null || flare.Trail._disposed)
                {
                    flare.Trail = new(c =>
                    {
                        float comp = 1f - InverseLerp(0f, flare.Lifetime, flare.Time);
                        return 25f * comp;
                    },
                    (c, pos) =>
                    {
                        float inter = InverseLerp(0f, flare.Lifetime, flare.Time);
                        return (FlareColor().Lerp(Color.DarkOrange, inter) * Animators.MakePoly(3f).OutFunction(1f - inter)
                        * GetLerpBump(0f, .1f, 1f, .9f, c.X) * InverseLerp(0f, .1f, inter)) * .4f;
                    },
                    null, 50);
                }

                flare.Time++;

                float rad = Projectile.width * .25f;
                float rot = flare.Direction.ToRotation();
                float rand = flare.Offset;
                flare.A = PolarVector(rad, rot - rand);
                flare.B = PolarVector(rad, rot) + flare.Direction * Animators.MakePoly(2.6f).OutFunction.Evaluate(0f, flare.Distance, interpolant);
                flare.C = PolarVector(rad, rot + rand);

                List<Vector2> points = [];
                for (int j = 0; j < 50; j++)
                {
                    Vector2 bezier = QuadraticBezier(flare.A, flare.B, flare.C, InverseLerp(0, 50, j));
                    points.Add(Projectile.Center + bezier);
                }
                flare.Points.SetPoints(points);
            }
        }

        Flares.RemoveAll(c => c.Time >= c.Lifetime);
    }
}
