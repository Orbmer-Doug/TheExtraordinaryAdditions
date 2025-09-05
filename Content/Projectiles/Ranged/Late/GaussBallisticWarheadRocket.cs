using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class GaussBallisticWarheadRocket : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GaussBallisticWarheadRocket);
    public override void SetDefaults()
    {
        Projectile.width = 76;
        Projectile.height = 30;
        Projectile.timeLeft = 450;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Maxxed
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Fuel => ref Projectile.ai[2];
    public const float MaxFuel = 2000f;
    public float FuelCompletion => InverseLerp(0f, MaxFuel, Fuel);
    public NPC Target;
    public Player Owner => Main.player[Projectile.owner];
    public Vector2 Back => Projectile.Hitbox.ToRotated(Projectile.rotation).Left;
    private ref float InitVelLength => ref Projectile.Additions().ExtraAI[0];

    public Projectile OwnerProj
    {
        get
        {
            return Main.projectile[(int)Projectile.Additions().ExtraAI[1]];
        }
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            InitVelLength = Projectile.velocity.Length();
            Fuel = MaxFuel;
            this.Sync();
        }

        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);

        Time++;

        Fuel -= Projectile.velocity.Length() / 2;
        if (OwnerProj != null && OwnerProj.active && Fuel > 0f)
        {
            Target = OwnerProj.As<GaussBallisticWarheadHoldout>().Target;
            if (Target.CanHomeInto() && Time > 20f
                && Target.GetGlobalNPC<GaussGlobalNPC>().LockIn >= GaussBallisticWarheadHoldout.LockInTime)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                    Projectile.SafeDirectionTo(Target.Center + Target.velocity * 10f) * InitVelLength, .1f);
            }
        }

        if (Fuel <= 0f)
        {
            Projectile.velocity.X *= .97f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, -10f, 30f);
        }
        Projectile.FacingRight();

        cache.Update(Back + Projectile.velocity);

        if (FuelCompletion > 0f)
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(.16f) * Main.rand.NextFloat(-.65f, .3f);
            Color col = Color.Yellow * 1.6f;
            float size = Main.rand.NextFloat(.8f, 1f);
            ParticleRegistry.SpawnGlowParticle(Back, vel * .8f * FuelCompletion, Main.rand.Next(15, 23), Main.rand.NextFloat(.4f, .6f) * FuelCompletion, col, .7f);
            ParticleRegistry.SpawnSparkParticle(Back, vel * FuelCompletion, Main.rand.Next(30, 40), size * FuelCompletion, col);

            Lighting.AddLight(Back, Color.GreenYellow.ToVector3() * 1.5f * FuelCompletion);
            Lighting.AddLight(Back, Color.Green.ToVector3() * FuelCompletion);
        }
    }

    private float WidthFunction(float completion)
    {
        return MathHelper.SmoothStep(Projectile.height, 0f, completion) * FuelCompletion;
    }
    private Color ColorFunction(SystemVector2 completion, Vector2 position)
    {
        Color startingColor = Color.Lerp(Color.Yellow * 1.6f, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.YellowGreen * .4f, Color.YellowGreen, 0.2f);
        Color endColor = Color.Lerp(Color.YellowGreen * .3f, Color.YellowGreen, 0.67f);
        return MulticolorLerp(completion.X, startingColor, middleColor, endColor) * GetLerpBump(.8f, .2f, 0f, 0.067f, completion.X) * FuelCompletion;
    }

    public TrailPoints cache = new(30);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = ShaderRegistry.SmoothFlame;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 1);
                shader.TrySetParameter("heatInterpolant", 2f);
                trail.DrawTrail(shader, cache.Points, 100, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        if (Maxxed)
            Projectile.DrawProjectileBackglow(Color.GreenYellow * 1.4f, 8f * FuelCompletion, 90, 12);
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
        Main.EntitySpriteDraw(AssetRegistry.GetTexture(AdditionsTexture.GaussBallisticWarheadRocket_Glow), drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Maxxed)
            AdditionsSound.GaussBoom.Play(Projectile.Center, 1.1f, 0f, .1f);
        else
            AdditionsSound.BlackHoleExplosion.Play(Projectile.Center, 1.4f, -.3f);

            Vector2 pos = Projectile.Center;
        if (Maxxed)
        {
            ScreenShakeSystem.New(new(18f, 1.5f, 2400), pos);
            ParticleRegistry.SpawnFlash(pos, 20, 1.8f, 1300f);
            ParticleRegistry.SpawnBlurParticle(pos, 60, 1.2f, 1000f);

            for (int i = 0; i < 90; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(30f, 30f);
                int life = Main.rand.Next(90, 200);
                float scale = Main.rand.NextFloat(1f, 2.3f);
                Color color = MulticolorLerp(Main.rand.NextFloat(), Color.Yellow * 1.5f, Color.YellowGreen, Color.YellowGreen * 1.9f, Color.Yellow * 2f);

                ParticleRegistry.SpawnCloudParticle(pos, vel, color, Color.DarkGreen, life, scale * 140f, Main.rand.NextFloat(.5f, .7f));

                ParticleRegistry.SpawnGlowParticle(pos, vel, life / 2, scale * 90f, color, .8f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, life / 3, scale, color);

                ParticleRegistry.SpawnGlowParticle(pos, vel * .9f, life, scale * 60f, color, 1.2f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, color, true);

                ParticleRegistry.SpawnBloomPixelParticle(pos, vel * 3.5f, life, scale * .8f, color, color * 2f, null, 1.2f, 12);

                for (int j = 0; j < 2; j++)
                    ParticleRegistry.SpawnBloomLineParticle(pos, vel.RotatedByRandom(.8f) * 2.5f, life / 2, scale * Main.rand.NextFloat(.6f, 1f), color * 1.2f);
            }

                Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<GaussShockwave>(),
                    Projectile.damage * 2, Projectile.knockBack * 2, Projectile.owner);
        }
        else
        {
            ScreenShakeSystem.New(new(8f, .5f, 1500), pos);

            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(1.8f) * Main.rand.NextFloat(9f, 18f);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<VolatilePlasmaGlobule>(), Projectile.damage / 5, 2f, Owner.whoAmI);
            }

                Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<GaussBoom>(),
                    Projectile.damage, 0f, Projectile.owner);
        }

        if (Main.netMode != NetmodeID.Server)
        {
            int headGore = Mod.Find<ModGore>($"{Name}Gore{2}").Type;
            int armGore = Mod.Find<ModGore>($"{Name}Gore{1}").Type;
            int legGore = Mod.Find<ModGore>($"{Name}Gore{3}").Type;
            for (int l = 0; l < 3; l++)
            {
                Vector2 spawnPosition2 = Projectile.Center;
                if (!WorldGen.SolidTile((int)spawnPosition2.X / 16, (int)spawnPosition2.Y / 16, false))
                {
                    Gore.NewGorePerfect(Projectile.GetSource_Death(null), spawnPosition2, Main.rand.NextVector2CircularEdge(6f, 6f), headGore, Projectile.scale);
                    Gore.NewGorePerfect(Projectile.GetSource_Death(null), spawnPosition2, Main.rand.NextVector2CircularEdge(9f, 9f), armGore, Projectile.scale);
                    Gore.NewGorePerfect(Projectile.GetSource_Death(null), spawnPosition2, Main.rand.NextVector2CircularEdge(11f, 11f), legGore, Projectile.scale);
                }
            }
        }
    }
}