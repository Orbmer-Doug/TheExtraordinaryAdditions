using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
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
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Execution);
    public override void SetDefaults()
    {
        Projectile.Size = new(34, 144);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.timeLeft = 200;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public ref float Time => ref Projectile.ai[0];
    public List<Vector2> TelePositions = [];
    public Vector2 Start;
    public Vector2 TeleEnd;
    public Vector2 End;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
        writer.WriteVector2(TeleEnd);
        writer.WriteVector2(End);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
        TeleEnd = reader.ReadVector2();
        End = reader.ReadVector2();
    }

    public const int TeleTime = 50;
    public const int DiveTime = 40;
    public const int FadeTime = 30;
    public enum SpearState
    {
        Teleport,
        Wait,
        Dive,
        Fade,
    }

    public SpearState State
    {
        get => (SpearState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }
    public ref float ImageOpacity => ref Projectile.ai[2];
    public override void AI()
    {
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        if (State != SpearState.Fade)
            Owner.velocity = Vector2.Zero;

        int dir = Projectile.velocity.X.NonZeroSign();
        switch (State)
        {
            case SpearState.Teleport:
                Start = Center;
                ImageOpacity = 0f;
                AdditionsSound.GabrielTeleport.Play(Center, 1f);
                Projectile.rotation = MathHelper.Pi;
                if (this.RunLocal())
                {
                    Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
                    TeleEnd = Modded.mouseWorld.ClampInCircle(Center, 700f);
                }

                dir = Projectile.velocity.X.NonZeroSign();
                Projectile.spriteDirection = dir;

                Vector2 end = TeleEnd + Vector2.UnitY * 600f;
                Vector2? tile = RaytraceTiles(TeleEnd, end);
                if (tile.HasValue)
                    End = tile.Value - Vector2.UnitY * Owner.height;
                else
                    End = end;
                if (End.X == 0f)
                    End = end;

                TelePositions = Center.GetLaserControlPoints(TeleEnd, 20);
                Owner.Teleport(TeleEnd, -1);
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Owner.whoAmI, TeleEnd.X, TeleEnd.Y, -1);

                State = SpearState.Wait;
                Time = 0f;
                this.Sync();
                break;
            case SpearState.Wait:
                ImageOpacity = InverseLerp(0f, TeleTime, Time);
                if ((int)Time == TeleTime / 2)
                    AdditionsSound.GabrielTelegraph.Play(Center, 1f);

                Projectile.Opacity = InverseLerp(0f, 10f, Time);
                if (Time >= TeleTime)
                {
                    AdditionsSound.GabrielSwing.Play(Center, 2f, 0f);
                    Time = 0f;
                    State = SpearState.Dive;
                    this.Sync();
                }
                break;
            case SpearState.Dive:
                Owner.Center = Vector2.Lerp(TeleEnd, End, MakePoly(6f).OutFunction(InverseLerp(0f, DiveTime / 2, Time)));
                if (Time >= DiveTime)
                {
                    Time = 0f;
                    State = SpearState.Fade;
                    this.Sync();
                }
                this.Sync();
                break;
            case SpearState.Fade:
                Projectile.Opacity = InverseLerp(FadeTime, 0f, Time);
                if (Time >= FadeTime)
                {
                    Projectile.Kill();
                }
                break;
        }

        Owner.SetFrontHandBetter(0, dir < 0 ? MathHelper.Pi : 0f);
        Owner.SetBackHandBetter(0, (dir < 0 ? MathHelper.Pi : 0f) + (.6f * dir));
        Owner.ChangeDir(dir);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * 40f;
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Bottom, Projectile.BaseRotHitbox().Top, Projectile.width);
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => State == SpearState.Dive ? null : false;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = Projectile.BaseRotHitbox().Top;
        for (int i = 0; i < 6; i++)
        {
            ParticleRegistry.SpawnBloodStreakParticle(pos, Vector2.UnitY.RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 5f), Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, .9f), Color.DarkRed);
        }
        for (int i = 0; i < 40; i++)
        {
            ParticleRegistry.SpawnBloodParticle(pos, -Vector2.UnitY.RotatedByRandom(.6f) * Main.rand.NextFloat(7f, 12f), Main.rand.Next(50, 90), Main.rand.NextFloat(.6f, 1.2f), Color.DarkRed);
        }
    }

    public override void Load()
    {
        On_LegacyPlayerRenderer.DrawPlayerFull += PlayerAfterImages;
    }

    public override void Unload()
    {
        On_LegacyPlayerRenderer.DrawPlayerFull -= PlayerAfterImages;
    }

    private static void PlayerAfterImages(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig, LegacyPlayerRenderer self, Terraria.Graphics.Camera camera, Player player)
    {
        int type = ModContent.ProjectileType<JudgeSpear>();
        if (player.ownedProjectileCounts[type] > 0 && Utility.FindProjectile(out Projectile spear, type, player.whoAmI))
        {
            JudgeSpear judge = spear.As<JudgeSpear>();

            SpriteBatch spriteBatch = camera.SpriteBatch;
            SamplerState samplerState = camera.Sampler;
            if (player.mount.Active && player.fullRotation != 0f)
            {
                samplerState = LegacyPlayerRenderer.MountedSamplerState;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState, DepthStencilState.None, camera.Rasterizer, null, camera.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < judge.TelePositions.Count; i++)
            {
                Vector2 pos = judge.TelePositions[i];
                self.DrawPlayer(camera, player, pos, player.fullRotation, player.fullRotationOrigin, MathHelper.Lerp(.6f, 20f, judge.ImageOpacity) * InverseLerp(judge.TelePositions.Count, 0f, i), 1f);
            }
            spriteBatch.End();
        }

        orig.Invoke(self, camera, player);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}