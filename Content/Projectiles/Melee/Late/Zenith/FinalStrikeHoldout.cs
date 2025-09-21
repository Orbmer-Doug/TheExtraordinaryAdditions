using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.ScreenEffects;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;

public class FinalStrikeHoldout : ModProjectile
{
    public enum FinalStrikeState
    {
        Aim,
        Fire,
        Wait,
        DivinePierce,
        Stab
    }
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 TipOfSpear => Projectile.RotHitbox().TopRight;

    public FinalStrikeState CurrentState
    {
        get => (FinalStrikeState)Projectile.ai[0];
        set => Projectile.ai[0] = (int)value;
    }
    public ref float StateTime => ref Projectile.ai[1];
    public ref float Counter => ref Projectile.ai[2];

    public bool Init
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }
    public ref float Time => ref Projectile.Additions().ExtraAI[1];
    public ref float VanishTime => ref Projectile.Additions().ExtraAI[2];
    public bool Vanish
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }
    public ref float DivineFormInterpolant => ref Projectile.localAI[0];
    public ref float OldArmRot => ref Projectile.localAI[1];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FinalStrike);

    public override void SetDefaults()
    {
        Projectile.width = 138;
        Projectile.height = 140;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = 14400;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Melee;
    }

    public override void AI()
    {
        switch (CurrentState)
        {
            case FinalStrikeState.Aim:
                DoBehavior_Aim();
                break;
            case FinalStrikeState.Fire:
                DoBehavior_Fire();
                break;
            case FinalStrikeState.Wait:
                DoBehavior_Wait();
                break;
            case FinalStrikeState.DivinePierce:
                DoBehavior_Pierce();
                break;
            case FinalStrikeState.Stab:
                DoBehavior_Stab();
                break;
        }
        if (CurrentState != FinalStrikeState.Aim)
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

        StateTime++;
        Time++;
    }

    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    private static readonly int shootDelay = SecondsToFrames(2.4f);
    public void DoBehavior_Aim()
    {
        float animationCompletion = InverseLerp(0f, shootDelay, StateTime);
        DivineFormInterpolant = MakePoly(3).InFunction(animationCompletion);

        int frequency = 5;
        if (animationCompletion.BetweenNum(.33f, .66f, true))
            frequency = 3;
        if (animationCompletion.BetweenNum(.66f, 1f, true))
            frequency = 1;

        if (StateTime % frequency == frequency - 1f)
        {
            Vector2 pos = TipOfSpear + Main.rand.NextVector2Circular(150f, 150f);
            int life = Main.rand.Next(90, 120);
            float size = Main.rand.NextFloat(.3f, .6f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, Vector2.Zero, life, size, Color.Wheat, Color.AntiqueWhite, TipOfSpear, 1f, 7, false);
        }

        // Play a magic sound when the spear is ready to fire.
        if (StateTime == shootDelay)
        {
            for (int i = 0; i < 40; i++)
            {
                ParticleRegistry.SpawnSquishyPixelParticle(TipOfSpear, Main.rand.NextVector2CircularLimited(10f, 10f, .5f, 1f), Main.rand.Next(90, 150), Main.rand.NextFloat(.9f, 1.6f), Color.AntiqueWhite, Color.Wheat, 9, false, false, Main.rand.NextFloat(-.1f, .1f));
            }
            AdditionsSound.spearCharge.Play(Owner.Center, 1f, 0f, .1f, 1, Name);
        }

        if (StateTime >= shootDelay)
        {
            float speed = -Main.rand.NextFloat(5f, 12f);
            float scale = Main.rand.NextFloat(.7f, 1.2f);
            Vector2 sparkVelocity = Projectile.velocity.RotatedByRandom(.35f) * speed;
            ParticleRegistry.SpawnSparkParticle(TipOfSpear, sparkVelocity, Main.rand.Next(40, 50), scale, Color.Wheat);
        }
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * animationCompletion * 1.4f);

        // Aim the spear.
        if (this.RunLocal())
        {
            float aimInterpolant = Utils.GetLerpValue(5f, 25f, Center.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), aimInterpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        // Stick to the player.
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        float frontArmRotation = Projectile.rotation - PiOver4 - animationCompletion * Owner.direction * 0.74f;
        if (Owner.direction == 1)
            frontArmRotation += Pi;

        Projectile.Center = Center + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 27f + Projectile.velocity * Projectile.scale;

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Projectile.spriteDirection = Owner.direction;

        Item heldItem = Owner.HeldItem;
        if (this.RunLocal() && !Owner.Available() || heldItem is null)
        {
            Projectile.Kill();
            return;
        }

        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        OldArmRot = frontArmRotation;

        // Check if the spear can be fired if the player has stopped channeling.
        // If it can, fire it. Otherwise, destroy the speaar.
        if (this.RunLocal() && !Owner.channel)
        {
            if (StateTime >= shootDelay)
            {
                AdditionsSound.pierce.Play(Projectile.Center, 1f, 0f, .2f);

                StateTime = 0f;
                CurrentState = FinalStrikeState.Fire;
                Projectile.netUpdate = true;

                Projectile.velocity *= heldItem.shootSpeed;
                return;
            }

            Projectile.Kill();
        }
    }

    public void DoBehavior_Fire()
    {
        if (Projectile.timeLeft > 360)
            Projectile.timeLeft = 360;
        Projectile.extraUpdates = 3;
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * 1.4f);

        float throwCompletion = InverseLerp(0f, 25f * Projectile.extraUpdates, StateTime);
        float rot = OldArmRot + Pi * Dir;
        float anim = MakePoly(6).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
        Owner.SetCompositeArmFront(throwCompletion < 1f, Player.CompositeArmStretchAmount.Full, anim);
        if (throwCompletion < 1f)
            Owner.ChangeDir(Dir);

        if (StateTime % 5f == 0f)
        {
            IEntitySource source = Projectile.GetSource_FromThis(null);
            int damage = (int)(Projectile.damage * .5f);
            float off = ToRadians(10f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = TipOfSpear;
                float scale = 1.8f;
                Color col1 = Color.AntiqueWhite;

                Vector2 perturbedSpeed = new Vector2((0f - Projectile.velocity.X) / 3f,
                    (0f - Projectile.velocity.Y) / 3f).RotatedBy((double)Lerp(0f - off, off, i / (2 - 1)), default);
                for (int j = 0; j < 2; j++)
                {
                    if (this.RunLocal())
                        Projectile.NewProjectile(source, Projectile.Center, perturbedSpeed * 1.2f, ModContent.ProjectileType<Streaks>(), damage, 0f, Projectile.owner, 0f, 0f, 0f);

                    for (int p = 0; p < 2; p++)
                    {
                        ParticleRegistry.SpawnSparkParticle(pos, perturbedSpeed * 2, 180, scale, col1);
                    }

                    perturbedSpeed *= 1.55f;
                }
            }
        }

        Projectile.spriteDirection = 1;
    }

    public DeicidePortal portal;
    public const float waitTime = 180f;
    public float WaitCompletion => InverseLerp(0f, waitTime, StateTime);
    public void DoBehavior_Wait()
    {
        float ratio = MakePoly(2).InFunction(InverseLerp(0f, waitTime / 2, StateTime));
        cache ??= new(40);

        Vector2 dir = Projectile.Center.SafeDirectionTo(Modded.mouseWorld);
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, dir, ratio);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        cache.SetPoints(Projectile.RotHitbox().BottomLeft.GetLaserControlPoints(Projectile.RotHitbox().BottomLeft + Projectile.velocity.SafeNormalize(Vector2.Zero) * WaitCompletion * 2500f, 40));

        float displace = new PiecewiseCurve()
            .Add(0f, 70f,  .8f, MakePoly(4).InFunction)
            .Add(70f, 0f, 1f, Exp().OutFunction)
            .Evaluate(InverseLerp(waitTime / 2, waitTime, StateTime));
        Projectile.Center = Vector2.Lerp(Projectile.Center, portal.Projectile.Center, .5f);
        portal.Projectile.timeLeft = 20;
        Projectile.timeLeft = 300;
        Projectile.extraUpdates = 3;

        if (StateTime > waitTime)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 20, 0f, Vector2.One, 0f, .4f, Color.AntiqueWhite, true);
            Projectile.velocity *= 16f;
            Projectile.MaxUpdates = 8;
            AdditionsSound.IkeFinal.Play(Projectile.Center, 1f, -.2f, .1f);
            AdditionsSound.pierce.Play(Projectile.Center, 1.5f, -.5f, .1f);
            CurrentState = FinalStrikeState.DivinePierce;
            this.Sync();
        }
    }

    public void DoBehavior_Pierce()
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(12f, 19f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.5f, .9f);
            Color col = Color.Wheat.Lerp(Color.AntiqueWhite, Main.rand.NextFloat(.2f, 5f));
            ParticleRegistry.SpawnSparkParticle(TipOfSpear + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale, col);
            ParticleRegistry.SpawnSparkleParticle(TipOfSpear, vel, life, scale, col, Color.Wheat, 1.2f, Main.rand.NextFloat(-.2f, .2f));
        }
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * 2.3f);
    }

    public Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(offset);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        offset = reader.ReadVector2();
    }

    public float Completion => InverseLerp(0f, 18f, StateTime);
    public float Bump => GetLerpBump(.1f, .55f, 1f, .85f, Completion);
    public void DoBehavior_Stab()
    {
        if (!Init)
        {
            Projectile.ResetLocalNPCHitImmunity();
            Projectile.numHits = 0;
            StateTime = 0f;
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld).RotatedByRandom(.15f);
            Init = true;
            this.Sync();
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = Projectile.timeLeft = 2;
        Owner.ChangeDir(Dir);

        if (Vanish)
        {
            Projectile.Opacity = MakePoly(3).OutFunction(1f - InverseLerp(0f, 15f, VanishTime));
            if (VanishTime > 15f)
                Projectile.Kill();

            VanishTime++;
        }

        if (Completion >= 1f)
        {
            if (Modded.SafeMouseRight.Current && VanishTime <= 0f)
            {
                Init = false;
            }
            else
            {
                Vanish = true;
            }
        }

        if (StateTime == 0f)
        {
            AdditionsSound.etherealSwordAttackBasic3.Play(TipOfSpear, Main.rand.NextFloat(1f, 1.5f), 0f, .2f, 0, Name);
        }

        float pierce = new PiecewiseCurve()
            .Add(-20f, 70f, .6f, MakePoly(7).OutFunction)
            .Add(70f, -20f, 1f, MakePoly(3).OutFunction)
            .Evaluate(Completion);
        Lighting.AddLight(TipOfSpear, Color.AntiqueWhite.ToVector3() * Bump * 1.4f);

        Projectile.Center = Center + Projectile.velocity * pierce;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        float length = (Projectile.height + 10f) * Projectile.scale;
        float width = 12f * Projectile.scale;
        Vector2 start = Projectile.Center;
        Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.Zero) * length;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, width, ref collisionPoint);
    }

    public float WidthFunct(float c)
    {
        return 11f * WaitCompletion;
    }
    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return Color.White * SmoothStep(1f, 0f, c.X) * InverseLerp(waitTime, waitTime - 24f, StateTime);
    }

    public ManualTrailPoints cache;
    public void DrawTele()
    {
        if (CurrentState == FinalStrikeState.Wait)
        {
            void draw()
            {
                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1);
                OptimizedPrimitiveTrail line = new(WidthFunct, ColorFunct, null, 40);
                line.DrawTrail(shader, cache.Points, 50);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }
    }


    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.MediumExplosion.Play(TipOfSpear, 1.2f, 0f, .2f);
        Projectile.damage = (int)MathF.Max(500f, Projectile.damage * 0.91f);

        if (CurrentState != FinalStrikeState.DivinePierce)
        {
            Vector2 pos;
            if (CheckLinearCollision(Projectile.RotHitbox().TopRight, Projectile.RotHitbox().BottomLeft, target.Hitbox, out Vector2 start, out Vector2 end))
                pos = start;
            else
                pos = TipOfSpear;

            ScreenShakeSystem.New(new(CurrentState == FinalStrikeState.Fire ? 1f : .2f, CurrentState == FinalStrikeState.Fire ? .5f : .1f), pos);

            Vector2 splatterDirection = CurrentState == FinalStrikeState.Stab ? Projectile.velocity * Main.rand.NextFloat(6f, 14f) : Projectile.velocity / 2;
            for (int i = 0; i < 20; i++)
            {
                int life = Main.rand.Next(55, 70);
                float scale = Main.rand.NextFloat(1.7f, Main.rand.NextFloat(3.3f, 5.5f)) * 0.85f;
                Color col = Color.Lerp(Color.Beige, Color.Wheat * 1.2f, Main.rand.NextFloat(0.7f));
                col = Color.Lerp(col, Color.AntiqueWhite, Main.rand.NextFloat());
                Vector2 vel = splatterDirection.RotatedByRandom(0.599) * Main.rand.NextFloat(.5f, 1.2f);

                ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, col);
                ParticleRegistry.SpawnSparkParticle(pos, vel * 1.5f, 80, scale * .7f, Color.AntiqueWhite);
                if (i % 4 == 3)
                {
                    ParticleRegistry.SpawnLightningArcParticle(pos, vel.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(90f, 300f), Main.rand.Next(20, 40), 1.5f, Color.White);
                }
            }
        }
        else
        {
            Projectile.damage = Owner.HeldItem.damage * 10;
            Projectile.knockBack = 30f;
            target.velocity *= Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.knockBack * target.knockBackResist;

            for (int i = 0; i < 180; i++)
            {
                int life = Main.rand.Next(50, 90);
                float scale = Main.rand.NextFloat(1.5f, 2.4f);
                Color col = Color.Wheat.Lerp(Color.AntiqueWhite, Main.rand.NextFloat(.2f, .7f));
                Vector2 vel = Main.rand.NextVector2Circular(80f, 80f);
                ParticleRegistry.SpawnSparkParticle(TipOfSpear, vel, life, scale, col);
                ParticleRegistry.SpawnSquishyPixelParticle(TipOfSpear, vel, life * 2, scale * 3f, col, Color.Wheat, Main.rand.NextByte(2, 6), false, false, Main.rand.NextFloat(-.09f, .09f));
                ParticleRegistry.SpawnSquishyPixelParticle(TipOfSpear, vel * 2, life * 2, scale * 2f, col, Color.Wheat);
                ParticleRegistry.SpawnGlowParticle(TipOfSpear, vel * 1.6f, life / 2, scale, col);
            }

            float off = RandomRotation();
            for (int i = 0; i < 6; i++)
            {
                DivineLightning light = Main.projectile[Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DivineLightning>(),
                    Projectile.damage / 4, 0f, Owner.whoAmI)].As<DivineLightning>();
                light.Start = TipOfSpear;
                light.End = TipOfSpear + (TwoPi * InverseLerp(0f, 6f, i) + off).ToRotationVector2() * Main.rand.NextFloat(1000f, 2000f);
            }

            Projectile.CreateFriendlyExplosion(TipOfSpear, new(600f), Projectile.damage / 3, 0f, 10, 9);
            AdditionsSound.LightningExplosion.Play(TipOfSpear, 1.4f, 0f, .2f, 1, Name);
            ScreenShakeSystem.New(new(2f, 2f, 1700f), Projectile.Center);
            ParticleRegistry.SpawnFlash(Projectile.Center, 30, .4f, 6000f);
            Projectile.Kill();
        }

        if (CurrentState == FinalStrikeState.Stab && Projectile.numHits <= 0)
        {
            if (Modded.FinalStrikeCounter >= 10)
            {
                portal = Main.projectile[Projectile.NewProj(TipOfSpear, Vector2.Zero, ModContent.ProjectileType<DeicidePortal>(), 0, 0f, Projectile.owner)].As<DeicidePortal>();
                Modded.FinalStrikeCounter = 0;
            }

            Modded.FinalStrikeCounter++;
        }
    }

    public override bool? CanDamage()
    {
        if (CurrentState == FinalStrikeState.Stab)
            return Completion.BetweenNum(0f, .7f) ? null : false;
        return CurrentState == FinalStrikeState.Aim || CurrentState == FinalStrikeState.Wait ? false : null;
    }
    
    public void DrawBackglow()
    {
        SpriteBatch sb = Main.spriteBatch;

        float backglowWidth = DivineFormInterpolant * 2f;
        if (backglowWidth <= 0.5f)
            backglowWidth = 0f;

        Color backglowColor = Color.AntiqueWhite;
        backglowColor = Color.Lerp(backglowColor, Color.NavajoWhite, Utils.GetLerpValue(0.7f, 1f, DivineFormInterpolant, true) * 0.56f) * 0.4f;
        backglowColor.A = (byte)(20 * Projectile.Opacity);

        Texture2D glowmaskTexture = Projectile.ThisProjectileTexture();
        Rectangle frame = glowmaskTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;
        for (int i = 0; i < 10; i++)
        {
            Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * backglowWidth;
            sb.Draw(glowmaskTexture, drawPosition + drawOffset, frame, backglowColor * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0f);
        }

        if (CurrentState != FinalStrikeState.Stab)
        {
            Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
            float auraRotation = Projectile.velocity.ToRotation() + PiOver4;
            Vector2 drawStartOuter = offsets + Projectile.Center + Projectile.velocity;
            Vector2 spinPoint = -Vector2.UnitY * 6f * DivineFormInterpolant;
            float time = Main.GlobalTimeWrappedHourly;
            float rotation = TwoPi * time / 3f;
            float opacity = .85f * DivineFormInterpolant;
            for (int i = 0; i < 6; i++)
            {
                Vector2 spinStart = drawStartOuter + spinPoint.RotatedBy((double)(rotation - (float)Math.PI * i / 3f), default);
                Color glowAlpha = Projectile.GetAlpha(backglowColor * Projectile.Opacity);
                glowAlpha.A = (byte)Projectile.alpha;
                sb.Draw(glowmaskTexture, spinStart, frame, glowAlpha * opacity, auraRotation, origin, Projectile.scale, 0, 0f);
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D spearTexture = Projectile.ThisProjectileTexture();
        Rectangle frame = spearTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;

        DrawTele();
        Main.spriteBatch.Draw(spearTexture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0f);
        if (CurrentState != FinalStrikeState.Stab)
            DrawBackglow();

        void draw()
        {
            for (float i = 1f; i < 1.5f; i += .1f)
            {
                Texture2D flare = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Vector2 size = new(30f * i * DivineFormInterpolant);
                size.Y += MathF.Sin(StateTime * .04f) * 10f * i;
                if (CurrentState == FinalStrikeState.Stab)
                    size = new(60f * i * Bump);
                Rectangle target = ToTarget(Projectile.RotHitbox().TopRight, size);
                Vector2 orig = flare.Size() / 2f;
                float rot = Projectile.rotation - PiOver4;
                Main.spriteBatch.Draw(flare, target, null, Color.AntiqueWhite, rot, orig, 0, 0f);
            }
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverProjectiles, BlendState.Additive);

        return false;
    }
}