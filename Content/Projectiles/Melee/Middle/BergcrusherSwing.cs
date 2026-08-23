using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class BergcrusherSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GennedTextures.Bergcrusher.Path;

    public RotatedRectangle BladeRect()
    {
        Vector2 start = Rect().Bottom + PolarVector(53f, Projectile.rotation) +
                        PolarVector(47f, Projectile.rotation - PiOver2);
        Vector2 end = start + PolarVector(80f, Projectile.rotation) + PolarVector(62f, Projectile.rotation - PiOver2);
        return new RotatedRectangle(66f, start, end);
    }

    public override void Defaults()
    {
        Projectile.ownerHitCheck = true;
    }

    public override float SwingAngle => PiOver2;
    public override int MaxUpdates => 5;

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(1f, 1.6f, .4f, MakePoly(3f).OutFunction) // back
            .Add(1.6f, -1f, 1f, Expo(2.2f).InOutFunction)
            .Evaluate(SwingCompletion);
    }

    public override int StopTimeFrames => 2;
    public override int SwingTime => SwingDir == SwingDirection.Down ? 60 : 50;

    public override bool? CanDamage() => SwingCompletion > .1f && SwingCompletion < .9f ? null : false;

    public override void OnSpawn(IEntitySource source)
    {
        SwingDir = SwingDirection.Down;
        Projectile.netUpdate = true;
    }

    public override void SafeInitialize()
    {
        points.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.rotation = SwingOffset();
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        // swoosh
        float anim = Animation();
        if (anim > .5f && anim < .6f && !PlayedSound)
        {
            AssetRegistry.GennedSounds.BraveIceSlash.Play(Projectile.Center, .9f, -.3f, .1f);
            PlayedSound = true;
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 15 * MaxUpdates);

        // Update trails
        if (TimeStop <= 0f)
        {
            points.Update(BladeRect().Center + Owner.velocity - Center);
        }

        float scaleUp = MeleeScale * 1.15f;
        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * scaleUp;
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, scaleUp, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                Initialized = false;
            }
            else
            {
                VanishTime++;
            }

            this.Sync();
        }

        AxeMist();
        UpdateBerg();
    }

    public void AxeMist()
    {
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .03f || Time < 5f || Time % 2 == 0)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = BladeRect().RandomPoint();
            Vector2 vel = -SwordDir * Main.rand.NextFloat(2f, 4f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue,
                Color.CornflowerBlue, Color.Violet);

            ParticleRegistry.SpawnDustParticle(pos, vel, life, scale, color);
            Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(30f, 30f), DustID.SilverCoin, vel, 0, default,
                Main.rand.NextFloat(.7f, .9f));
        }

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(1f, 5f);
            int life = Main.rand.Next(20, 32);
            float scale = Main.rand.NextFloat(.8f, 1.2f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue,
                Color.CornflowerBlue, Color.Lerp(Color.Violet, Color.Blue, .5f), Color.DarkCyan);
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(4f, 4f), vel * 4f, life, scale,
                color);
            Dust.NewDustPerfect(start, DustID.SilverCoin, vel * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(20, 50),
                default, Main.rand.NextFloat(.8f, 1.5f));
        }

        npc.velocity += -SwordDir * Item.knockBack * npc.knockBackResist;

        AssetRegistry.GennedSounds.ColdHitBig.Play(Projectile.Center, .9f, 0f, .11f);
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(1f, 5f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(50.2f, 60.9f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue,
                Color.CornflowerBlue, Color.Lerp(Color.Violet, Color.Blue, .5f), Color.DarkCyan);
            ParticleRegistry.SpawnCloudParticle(start, vel, color, Color.DarkSlateBlue, life, scale, .8f);
            Dust.NewDustPerfect(start, DustID.SilverCoin, vel * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(20, 50),
                default, Main.rand.NextFloat(.8f, 1.5f));
        }

        AssetRegistry.GennedSounds.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
    }

    public Trail trail;
    public TrailPoints points = new(25);

    public static float WidthFunct(float c)
    {
        return SmoothStep(0f, 1f, SmoothStep(1f, 0f, c)) * 91f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        float opacity = InverseLerp(0.022f, 0.07f, AngularVelocity);
        return MulticolorLerp(c.X, new Color(125, 251, 255), new Color(86, 196, 227), new Color(21, 92, 173)) * opacity;
    }

    public ref float BergScale => ref Projectile.AdditionsInfo().ExtraAI[7];
    public ref float BergOpacity => ref Projectile.AdditionsInfo().ExtraAI[8];

    public bool Slap
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[9] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[9] = value.ToInt();
    }

    public void UpdateBerg()
    {
        bool up = SwingDir == SwingDirection.Down;
        if (Slap || up)
        {
            if (BergScale > 0f)
            {
                BergScale = BergOpacity = 0f;
                this.Sync();
            }

            if (up)
            {
                Slap = false;
                this.Sync();
            }

            return;
        }

        int hitTime = (int) (SwingTime * MaxUpdates * .6f);
        Vector2 norm = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Vector2 offset = Center + norm * Projectile.height;
        RotatedRectangle bergRect =
            new RotatedRectangle(50f, offset + norm.PerpCCW() * 78f, offset + norm.PerpCW() * 78f);
        if (MathF.Round(Animation(), 1) == 0.5f)
        {
            Slap = true;
            BergScale = BergOpacity = 0f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = bergRect.RandomPoint();
                if (this.RunLocal())
                    Projectile.CreateProj(pos, norm * Main.rand.NextFloat(8f, 14f),
                        ModContent.ProjectileType<FlungShard>(),
                        (int) (Projectile.damage * .25f), Projectile.knockBack * .2f, Owner.whoAmI);

                for (int j = 0; j < 4; j++)
                {
                    ParticleRegistry.SpawnSparkParticle(bergRect.RandomPoint() + Main.rand.NextVector2Circular(4f, 4f),
                        norm * Main.rand.NextFloat(3f, 18f), Main.rand.Next(15, 25), Main.rand.NextFloat(2.1f, 2.4f),
                        Color.CornflowerBlue);
                    ParticleRegistry.SpawnMistParticle(bergRect.RandomPoint(),
                        norm.RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 9f), Main.rand.NextFloat(.3f, .8f),
                        AuroraGuard.Icey, AuroraGuard.DarkSlateBlue, Main.rand.NextFloat(220f, 250f));
                }
            }

            ScreenShakeSystem.New(new(.4f, .3f), bergRect.Center);
            AssetRegistry.GennedSounds.ColdPunch.Play(bergRect.Center, .8f, 0f, .2f);
            this.Sync();
        }
        else
        {
            float comp = InverseLerp(0f, hitTime, Time);
            BergScale = MakePoly(3f).OutFunction(comp);
            BergOpacity = MakePoly(2f).InFunction(comp);

            if (OverallTime % 2 == 1)
                ParticleRegistry.SpawnMistParticle(bergRect.RandomPoint(), RandomVelocity(.5f, 1f, 3f),
                    Main.rand.NextFloat(.4f, 1.1f), AuroraGuard.Icey, AuroraGuard.DarkSlateBlue,
                    Main.rand.NextFloat(120f, 200f));

            if (OverallTime % 4 == 3)
                ParticleRegistry.SpawnBloomPixelParticle(
                    bergRect.RandomPoint(),
                    norm.PerpCW() * Main.rand.NextFloat(.2f, .9f), Main.rand.Next(20, 40),
                    Main.rand.NextFloat(.3f, .6f),
                    AuroraGuard.SlateBlue, AuroraGuard.Icey);
        }
    }

    public void DrawBerg()
    {
        if (Slap)
            return;

        Texture2D tex = AssetRegistry.GennedTextures.GlacialSpike;
        Vector2 offset = Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.height;
        Color color = Color.White * BergOpacity;
        Vector2 scale = new Vector2(BergScale, 1f);
        Color flash = Projectile.GetAlpha(Color.White * BergOpacity) with { A = 0 } * BergOpacity;
        float rot = Projectile.velocity.ToRotation();
        SpriteEffects fx = Direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

        for (int i = 0; i < 10; i++)
            Main.spriteBatch.DrawBetter(tex, offset, null, flash, rot, tex.Size() / 2, scale, fx);
        Main.spriteBatch.DrawBetter(tex, offset, null, color, rot, tex.Size() / 2, scale, fx);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawBerg();

        // Determine the effects for drawing.
        Vector2 origin;
        bool flip = Direction != -1;

        if (flip)
        {
            origin = new Vector2(0, Tex.Height);

            RotationOffset = 0;
            Effects = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(Tex.Width, Tex.Height);

            RotationOffset = PiOver2;
            Effects = SpriteEffects.FlipHorizontally;
        }

        void draw()
        {
            if (trail == null || points == null || SwingCompletion < .45f || SwingCompletion > .95f)
                return;

            ManagedShader slash = AssetRegistry.GennedShaders.BloodBeaconShader;
            slash.SetTexture(AssetRegistry.GennedTextures.CrackedNoise, 1);
            trail.DrawTrail(slash, points.Points, 200, true);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}

public class FlungShard : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.GlacialShell.Path;

    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 38;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 360;
        Projectile.scale = 1f;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public bool HitGround
    {
        get => (int) Projectile.ai[0] == 1;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.ai[1];

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 10,
            3, 3f));

        Projectile.Opacity = InverseLerp(0f, 10f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);
        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * Projectile.scale * .4f);

        if (Main.rand.NextBool(15))
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(),
                -Projectile.velocity * Main.rand.NextFloat(.1f, .2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), Color.Cyan, Color.DeepSkyBlue, null, 1.2f);

        if (!HitGround)
        {
            Projectile.FacingUp();
            if (SolidCollisionFix(Projectile.Center, 10, 10))
            {
                SoundID.Item50.Play(Projectile.Center, 1.1f, -.1f, .1f);
                Collision.HitTiles(Projectile.Center, -Projectile.velocity, 10, 10);
                HitGround = true;
            }
        }
        else
        {
            Projectile.velocity = Vector2.Zero;
        }

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);
        return targetHitbox.LineCollision(Projectile.Center - vel * Projectile.height / 2f,
            Projectile.Center + vel * Projectile.height / 2f, Projectile.width)
            ? null
            : false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 10; i++)
        {
            ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().RandomPoint(),
                Projectile.velocity * Main.rand.NextFloat(.2f, .4f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .6f), Color.WhiteSmoke,
                Main.rand.NextFloat(-.1f, .1f));
        }

        SoundID.Item51.Play(Projectile.Center, .8f, .14f, .05f, 10);
        Projectile.Kill();
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = AssetRegistry.GennedTextures.GlowParticleSmall;
        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, tex,
            ToTarget(Projectile.Center, Projectile.Size * 1.65f), null,
            Color.LightCyan, Projectile.rotation, tex.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, tex,
            ToTarget(Projectile.Center, Projectile.Size * 1.85f), null,
            Color.DarkBlue, Projectile.rotation, tex.Size() / 2);

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [new(14, 32, 168)], Projectile.Opacity,
            Projectile.scale);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
