using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class JudgeSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JusticeIsSplendorW);

    /// <summary>
    /// BLUE
    /// </summary>
    public bool Splendor
    {
        get => Projectile.AdditionsInfo().ExtraAI[7] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[7] = value.ToInt();
    }

    public override int SwingTime => 20;

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, 1f, 1f, MakePoly(4.2f).InOutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
    }

    public override void SafeInitialize()
    {
        if (Splendor)
            TimeStop = 10 * MaxUpdates;
        if (!Splendor && SwingDir == SwingDirection.Up)
            TimeStop = 10 * MaxUpdates;
        points.Clear();
    }

    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 20);

        // Owner values
        if (Splendor)
            Owner.SetBackHandBetter(0, Projectile.rotation - SwordRotation);
        else
            Owner.SetFrontHandBetter(0, Projectile.rotation - SwordRotation);
        Owner.ChangeDir(Direction);
        Projectile.rotation = SwingOffset();
        Projectile.Center = Splendor ? Owner.GetBackHandPositionImproved() : Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);

        Owner.itemRotation = WrapAngle(Projectile.rotation);

        // swoosh
        if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
        {
            AdditionsSound.GabrielSwing.Play(Projectile.Center, .6f, 0f, .2f);
            PlayedSound = true;
        }

        // Update trails
        if (TimeStop <= 0f)
        {
            points.Update((Projectile.Center + PolarVector(66f, Projectile.rotation - SwordRotation)) - Center);
        }

        float scaleUp = MeleeScale;
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
                this.Sync();
            }
            else
            {
                foreach (Projectile p in Utility.AllProjectilesFromOwner(Type, Owner))
                {
                    JudgeSwing swing = p.As<JudgeSwing>();
                    swing.VanishTime++;
                    p.netUpdate = true;
                    p.netSpam = 0;
                }
            }
        }
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(9f, 14f);
            int life = Main.rand.Next(20, 28);
            float scale = Main.rand.NextFloat(.2f, .9f);
            Color color = ColorFunct(SystemVector2.Zero, Vector2.Zero);
            ParticleRegistry.SpawnBloomLineParticle(start + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale, color);
        }
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(9f, 14f);
            int life = Main.rand.Next(20, 28);
            float scale = Main.rand.NextFloat(.2f, .9f);
            Color color = ColorFunct(SystemVector2.Zero, Vector2.Zero);
            ParticleRegistry.SpawnBloomLineParticle(start + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale, color);
        }

        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
    }

    public float WidthFunct(float c) => 66f * Projectile.scale;
    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.016f, 0.07f, AngularVelocity);
        return (Splendor ? new Color(48, 114, 194) : new(255, 226, 42)) * (1f - c.X) * opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points);
        }

        Vector2 origin;
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;

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

        Texture2D tex = Splendor ? AssetRegistry.GetTexture(AdditionsTexture.SplendorIsJusticeW) : Tex;
        float rot = RotationOffset;
        SpriteEffects fx = Effects;
        void sword()
        {
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
                Projectile.rotation + rot, origin, Projectile.scale, fx, 0f);
        }
        PixelationLayer layer = Splendor ? PixelationLayer.UnderPlayers : PixelationLayer.OverPlayers;
        LayeredDrawSystem.QueueDrawAction(sword, layer);
        PixelationSystem.QueuePrimitiveRenderAction(draw, layer);

        return false;
    }
}

public class JudgeSpear : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GabesSpear);

    public Vector2 Size = new Vector2(30, 126);
    public Vector2 Top => Projectile.Center + PolarVector(Size.Y / 2, Projectile.rotation - PiOver2);
    public Vector2 Bottom => Projectile.Center - PolarVector(Size.Y / 2, Projectile.rotation - PiOver2);
    public override void SetDefaults()
    {
        Projectile.Size = new(10);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 200;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = 1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float OldArmRot => ref Projectile.ai[1];
    public bool Thrown
    {
        get => (int)Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }
    public int FadeTime
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = value;
    }
    public bool Hit
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[1] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    
    public float MeleeSpeed => Owner.GetTotalAttackSpeed(DamageClass.MeleeNoSpeed);
    public int ReelTime => (int)(20 / MeleeSpeed);
    public static readonly int ThrowTime = SecondsToFrames(4);
    
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

    public override void AI()
    {
        Projectile.Opacity = InverseLerp(0, 10, Time);
        if (!Thrown)
        {
            DoReel();
        }
        else
        {
            DoThrow();
        }

        Time++;
    }
    
    public void DoReel()
    {
        float completion = InverseLerp(0f, ReelTime, Time);
        if (!Owner.Available())
        {
            Projectile.Kill();
            return;
        }
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .9f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        
        Projectile.timeLeft = 22;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.direction);

        float vel = Projectile.velocity.ToRotation();
        float reelAnim = MakePoly(3f).InOutFunction.Evaluate(vel, vel - (1.3f * Projectile.direction * Owner.gravDir), completion);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, reelAnim);
        OldArmRot = reelAnim;
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;

        if (this.RunLocal() && completion >= 1f)
        {
            SoundID.Item1.Play(Projectile.Center, 1.4f, .1f, .2f);
            Time = 0;
            Thrown = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * ThrowTime;
            Projectile.velocity = Utility.SafeDirectionTo(Projectile, Modded.MouseWorld) * 20f;
            this.Sync();
        }
    }

    public void DoThrow()
    {
        after ??= new(10, () => Projectile.Center);
        float fade = InverseLerp(30f, 0f, FadeTime);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One,
            fade, Projectile.rotation, 0, 0, 1, 0f));
        if (Time < 30)
        {
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(Projectile.direction);
            float throwCompletion = InverseLerp(0f, 30, Time);
            float rot = OldArmRot + (Pi * Projectile.direction * Owner.gravDir);
            float anim = Animators.MakePoly(6f).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, anim);
            this.Sync();
        }

        if (Hit)
            fadeAway();
        else if (Collision.SolidCollision(Top, 10, 10))
        {
            HitEffects();
            fadeAway();
        }
        if (FadeTime >= 30)
            Projectile.Kill();
        return;

        void fadeAway()
        {
            Projectile.Opacity = 0f;
            Projectile.velocity *= .8f;
            FadeTime++;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        HitEffects();
    }

    private void HitEffects()
    {
        if (Hit) 
            return;
        
        if (this.RunLocal())
            Projectile.NewProj(Projectile.RotHitbox().Top, Vector2.Zero, ModContent.ProjectileType<JudgeKaboom>(),
                (int)(Projectile.damage * .5f), 1f, Projectile.owner);
        AdditionsSound.GenericExplo.Play(Top, .6f, 0f, .2f, 20);
        Hit = true;
        this.Sync();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Bottom, Top, Size.X);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 size = texture.Size();
        Color col = Color.Lerp(Color.Yellow with { A = 0 } * Projectile.Opacity, Projectile.GetAlpha(lightColor), Projectile.Opacity);
        
        float fade = InverseLerp(30f, 0f, FadeTime);
        after?.DrawFancyAfterimages(texture, [Color.DarkOrange, Color.Orange, Color.Gold], fade);
        Main.EntitySpriteDraw(texture, drawPosition, null,
            col, Projectile.rotation, size / 2, Projectile.scale, 0);

        return false;
    }
}

public class JudgeKaboom : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    private const int Lifetime = 55;
    public override void SetDefaults()
    {
        Projectile.scale = 0f;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public Player Owner => Main.player[Projectile.owner];

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int TimeOffset
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    } 

    public override void AI()
    {
        if (Time == 0)
        {
            TimeOffset = Main.rand.Next(0, 360);
            
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero,
                40, 0f, Vector2.One, 0f, 280f, Color.White * .68f);
            for (int i = 0; i < 30; i++)
            {
                ParticleRegistry.SpawnBloomLineParticle(Projectile.Center,
                    Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f),
                    Main.rand.Next(12, 20), Main.rand.NextFloat(.4f, .9f), Color.White);
            }
        }

        Projectile.scale = MakePoly(5f).OutFunction.Evaluate(Time, 0,
            90, 0f, 190f);
        Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * InverseLerp(0, Lifetime, Projectile.timeLeft) * 2f);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.scale, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void render()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.DarkRidgeNoise);
            float opacity = InverseLerp(0, Lifetime, Projectile.timeLeft);
            ManagedShader shader = AssetRegistry.GetShader("GabrielExplosion");
            shader.TrySetParameter("sides", 8);
            shader.TrySetParameter("opacity", opacity);
            shader.TrySetParameter("time", Time * .01f + TimeOffset);
            shader.TrySetParameter("col1", new Vector3(1f, 0.885f, 0f));
            shader.TrySetParameter("col2", new Vector3(1f, 0.515f, 0f));
            shader.SetTexture(noise, 1, SamplerState.LinearWrap);
            shader.Render();
            Main.spriteBatch.DrawBetter(tex,
                Projectile.Center, null, Color.White, 0f, Vector2.One / 2, Projectile.scale);
        }
        PixelationSystem.QueueTextureRenderAction(render, PixelationLayer.UnderProjectiles, BlendState.AlphaBlend, AssetRegistry.GetShader("GabrielExplosion"));
        return false;
    }
}