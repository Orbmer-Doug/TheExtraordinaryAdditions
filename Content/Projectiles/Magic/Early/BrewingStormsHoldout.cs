using System.Collections.Generic;
using System.IO;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using Utils = Terraria.Utils;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class BrewingStormsHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.BrewingStorms.Path;
    public override int AssociatedItemID => ModContent.ItemType<BrewingStorms>();
    public override int IntendedProjectileType => ModContent.ProjectileType<BrewingStormsHoldout>();

    public override void Defaults()
    {
        Projectile.width = 34;
        Projectile.height = 38;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
    }

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int SwingTime
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public static readonly int MaxSwingTime = 50;

    public ref float InitAngle => ref Projectile.ai[2];

    public ref int Charge => ref Owner.GetModPlayer<BrewingStormsPlayer>().Counter;
    public int ChargeNeeded = 50;
    public float Completion => Utils.GetLerpValue(0f, ChargeNeeded, Charge, true);

    public bool JustCompleted
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[0] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public ref float InitMaxDist => ref Projectile.AdditionsInfo().ExtraAI[1];

    public override void SafeAI()
    {
        if (SwingTime <= 0)
        {
            if (this.RunLocal())
            {
                float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Modded.MouseWorld), true);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.MouseWorld),
                    interpolant);
                if (Projectile.oldVelocity != Projectile.velocity)
                    this.Sync();

                if (Time % Item.useTime == Item.useTime - 1 && Modded.SafeMouseLeft.Current &&
                    Item.CheckManaBetter(Owner, 3, true))
                {
                    SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { PitchVariance = .1f }, Projectile.Center);
                    for (int i = 0; i <= 1; i++)
                    {
                        Vector2 vel =
                            Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.ToRadians(16)) *
                            Item.shootSpeed;
                        ParticleRegistry.SpawnLightningArcParticle(Projectile.Center,
                            vel.RotatedByRandom(.25f).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(80f, 120f),
                            Main.rand.Next(6, 8), Main.rand.NextFloat(.3f, .4f), Color.LightPink);

                        Projectile.NewProj(Projectile.Center + vel, vel,
                            ModContent.ProjectileType<LightningNimbusSparks>(),
                            Item.damage, Item.knockBack, Owner.whoAmI);
                    }
                }
            }

            Projectile.Center = Center + Projectile.velocity * Projectile.width / 2f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        else
        {
            const float swingAngle = MathHelper.PiOver2;
            float comp = InverseLerp(MaxSwingTime, 0, SwingTime);
            float anim = new PiecewiseCurve()
                .Add(0f, -1f, .4f, MakePoly(3f).InOutFunction)
                .Add(-1f, 0f, 1f, Expo(2.2f).OutFunction)
                .Evaluate(comp);

            Projectile.rotation = InitAngle + swingAngle * anim * Projectile.velocity.X.NonZeroSign();

            Vector2? hit = RaycastTiles(Center, Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * InitMaxDist);
            float maxDist = MathHelper.Clamp(hit?.Distance(Center) ?? InitMaxDist, 0f, 200f);
            float dist = new PiecewiseCurve()
                .AddStall(0f, .4f)
                .Add(0f, maxDist, .8f, MakePoly(3f).OutFunction)
                .Add(maxDist, 0f, 1f, MakePoly(3f).InFunction)
                .Evaluate(comp);
            Projectile.Center = Center + PolarVector(Projectile.width / 2f + dist, Projectile.rotation);

            if (SwingTime == MaxSwingTime / 2)
                AssetRegistry.GennedSounds.ElectricCast.Play(Projectile.Center, .6f, .1f, .3f);
        }

        Owner.ChangeDir(Projectile.direction);

        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);


        if (Main.rand.NextBool(2 + (int) Completion * 5))
        {
            Vector2 vel = Vector2.UnitY * Completion * -Main.rand.NextFloat(3f, 9f);
            ParticleRegistry.SpawnSparkParticle(Projectile.RandAreaInEntity(), vel, Main.rand.Next(18, 24),
                Main.rand.NextFloat(.4f, .8f) * Completion, Color.LightPink);
        }

        if (!JustCompleted && (int) Completion == 1)
        {
            for (int i = 0; i < 26; i++)
                ParticleRegistry.SpawnSparkParticle(Projectile.RandAreaInEntity(), RandomVelocity(1f, 2f, 5f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .9f), Color.MediumPurple);
            AssetRegistry.GennedSounds.FireWhoosh1.Play(Projectile.Center, .9f, .3f, .1f);
            JustCompleted = true;
            this.Sync();
        }

        if (this.RunLocal() && Modded.MouseRight.JustPressed && SwingTime == 0 && Charge >= ChargeNeeded)
        {
            SwingTime = MaxSwingTime;
            InitAngle = Projectile.rotation;
            InitMaxDist = Center.Distance(Mouse);
            this.Sync();
        }

        if (SwingTime > 0)
        {
            SwingTime--;
            if (this.RunLocal() && Projectile.numHits == 0)
                Charge = (int) Utils.Remap(SwingTime, MaxSwingTime, 0f, 0f, ChargeNeeded);
            if (SwingTime == 0)
            {
                if (Projectile.numHits > 0)
                {
                    JustCompleted = false;
                    Charge = 0;
                }

                SoundID.Item1.Play(Projectile.Center, .7f, .9f, .2f);
                Projectile.numHits = 0;
                Projectile.ResetLocalNPCHitImmunity();
                this.Sync();
            }
        }

        if (this.RunLocal() && Charge > 0)
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * Completion);

        Time++;
    }

    public override void WriteExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.numHits);
    }

    public override void GetExtraAI(BinaryReader reader)
    {
        Projectile.numHits = reader.ReadInt32();
    }

    public override bool? CanDamage()
    {
        float comp = InverseLerp(MaxSwingTime, 0, SwingTime);
        if (comp > .4f && comp < .7f)
            return null;
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return RaycastTiles(Center, Projectile.Center) == null &&
               Projectile.RotHitbox(Vector2.One / 2f).Intersects(targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Charge > 0)
        {
            if (this.RunLocal())
                Projectile.NewProj(target.BaseRotHitbox().GetClosestPoint(Projectile.Center), Vector2.Zero,
                    ModContent.ProjectileType<LightningBlast>(),
                    (int) (Projectile.damage * 3.5f), 6f, Owner.whoAmI, ai2: 200f);
            AssetRegistry.GennedSounds.IkeSpecial4.Play(Owner.Center, .6f, .3f, .15f);
            Charge = 0;
        }
    }

    internal float WidthFunction(float completionRatio)
    {
        return 2f * Projectile.scale;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return Color.MediumPurple * .7f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();

        if (SwingTime > 0)
        {
            void draw()
            {
                Vector2 destination = Owner.GetFrontHandPositionImproved();
                Vector2 start = Projectile.Center;
                Vector2 end = start + Projectile.SafeDirectionTo(destination) *
                    start.Distance(destination);
                List<Line> lightning = CreateBolt(start, end, 1f, 20f);
                TrailPoints final = new(lightning.Count * 2);
                List<Vector2> ends = [];
                for (int i = 0; i < lightning.Count; i++)
                {
                    Line line = lightning[i];
                    ends.Add(line.A);
                    ends.Add(line.B);
                }

                final.SetPoints(ends);

                ManagedShader shader = AssetRegistry.GennedShaders.EnlightenedBeam;
                shader.SetTexture(AssetRegistry.GennedTextures.CrackedNoise, 1);

                Trail trail = new(WidthFunction, ColorFunction, null, final.Count + 10);
                trail.DrawTrail(shader, final.Points, 30);
            }

            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);
        }

        if (Charge > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 off = PolarVector(Utils.Remap(Charge, 0, ChargeNeeded, 0, 8f),
                    MathHelper.TwoPi * InverseLerp(0, 4, i) + Time * .05f * Owner.direction);
                SpriteBatch.DrawAltPixelated(PixelationLayer.HeldProjectiles, BlendState.Additive, tex,
                    Projectile.Center + off, null, Color.Purple, Projectile.rotation,
                    tex.Size() / 2f, 1f, FixedDirection());
            }
        }

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Projectile.GetAlpha(lightColor), Projectile.rotation,
            tex.Size() / 2f, 1f, FixedDirection());
        return false;
    }
}

public sealed class BrewingStormsPlayer : ModPlayer
{
    public int Counter;
    public override void UpdateDead() => Counter = 0;
}

public class LightningNimbusSparks : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.LightningNimbusSparks.Path;
    public Player Owner => Main.player[Projectile.owner];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3;
    }

    public override void SetDefaults()
    {
        Projectile.width = 28;
        Projectile.height = 14;
        Projectile.timeLeft = 70;
        Projectile.penetrate = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }


    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);
        Vector2 orig = frame.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color color = Color.White;

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Pink, Color.Violet, Color.DarkViolet],
            Projectile.Opacity);
        Main.spriteBatch.Draw(tex, pos, frame, color, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        return false;
    }

    private ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, new(1f, .75f), Projectile.Opacity, Projectile.rotation, 0,
            90, 0, 0f, Projectile.ThisProjectileTexture().Frame(1, 3, 0, Projectile.frame), false, -.1f));

        Projectile.Opacity = InverseLerp(0f, 9f, Time);
        Projectile.SetAnimation(3, 6);
        Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * .5f * Projectile.Opacity);

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity *= .975f;
        if (Projectile.numHits > 0)
        {
            float scaleDown = (1f - InverseLerp(0f, 15f, Projectile.ai[1]++)) / 2 + .2f;
            Projectile.scale = scaleDown;
            Projectile.Resize((int) (28 * scaleDown), (int) (14 * scaleDown));
        }

        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.numHits > 0)
            modifiers.FinalDamage /= 2;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ref int counter = ref Owner.GetModPlayer<BrewingStormsPlayer>().Counter;
        counter += Projectile.numHits > 0 ? 1 : 3;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 8; i++)
            Dust.NewDustPerfect(Projectile.Center, DustID.WitherLightning, Main.rand.NextVector2Circular(4f, 4f), 0,
                default, Main.rand.NextFloat(.7f, 1.2f)).noGravity = true;

        SoundID.NPCHit53.Play(Projectile.Center, .6f);
    }
}

public class LightningBlast : ModProjectile
{
    public static readonly float Lifetime = SecondsToFrames(.4f);

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ref float Radius => ref Projectile.ai[1];
    public ref float MaxRadius => ref Projectile.ai[2];

    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1200;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = (int) Lifetime;
        Projectile.scale = 0.001f;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void AI()
    {
        Radius = MathHelper.Lerp(Radius, MaxRadius, MakePoly(3f).OutFunction(InverseLerp(0f, Lifetime, Time)));
        Projectile.scale = MathHelper.Lerp(.4f, .7f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Projectile.Opacity = InverseLerp(2f, 15f, Projectile.timeLeft);

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.5f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End(out SpriteBatchSnapshot ss);
        Main.spriteBatch.Begin(ss with { SortMode = SpriteSortMode.Immediate });
        Texture2D tex = AssetRegistry.GennedTextures.Perlin;

        ManagedShader shader = AssetRegistry.GennedShaders.LightShockwave;
        shader.SetTexture(AssetRegistry.GennedTextures.CrackedNoise, 1, SamplerState.LinearWrap);
        Color col = Color.Lerp(Color.MediumPurple, Color.DarkSlateBlue, 0.24f) with { A = 70 };
        shader.TrySetParameter("mainColor", col.ToVector3());
        shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
        shader.TrySetParameter("projPosition", Projectile.Center - Main.screenPosition);
        shader.TrySetParameter("shockwaveOpacity", Projectile.Opacity * .3f);
        shader.Render();
        Main.spriteBatch.Draw(tex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
            Color.White * Projectile.Opacity);

        Main.spriteBatch.Restart(ss);
        return false;
    }
}
