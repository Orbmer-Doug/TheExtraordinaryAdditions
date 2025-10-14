using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class SanguineLance : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SanguineLance);
    private const int StartingLife = 200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.extraUpdates = 4;
        Projectile.timeLeft = StartingLife;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
        Projectile.hide = true;
    }

    public enum CurrentState
    {
        Thrown,
        HitEnemy,
        HitGround
    }

    public CurrentState State
    {
        get => (CurrentState)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }

    public ref float AccumulatedVel => ref Projectile.ai[1];
    public ref float EnemyID => ref Projectile.ai[2];
    public ref float FlailAmt => ref Projectile.localAI[0];
    public ref float Timer => ref Projectile.localAI[1];
    public ref float StickTime => ref Projectile.localAI[2];
    private Vector2 offset;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(FlailAmt);
        writer.Write(Timer);
        writer.WriteVector2(offset);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        FlailAmt = reader.ReadSingle();
        Timer = reader.ReadSingle();
        offset = reader.ReadVector2();
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 50);

        cache ??= new(50);
        cache.Update(Projectile.Center);

        if (State == CurrentState.Thrown)
        {
            const int Slow = 30;
            if (Timer > (StartingLife - Slow))
            {
                AccumulatedVel -= .6f;
                Projectile.Opacity = Projectile.scale = 1f - InverseLerp(StartingLife - Slow, StartingLife, Timer, true);
                Projectile.velocity *= .6f;
                Projectile.extraUpdates = 1;
            }
            else
            {
                Projectile.Opacity = InverseLerp(0f, 30f, Timer);
                AccumulatedVel += Projectile.velocity.Length() * 0.05f;
                Projectile.velocity *= .995f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Timer++;
            return;
        }
        Projectile.velocity *= 0.91f;

        if (State == CurrentState.HitEnemy)
        {
            // Stick to the target
            NPC target = Main.npc[(int)EnemyID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.velocity *= InverseLerp(20f * Projectile.MaxUpdates, 0f, StickTime);
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
                offset += Projectile.velocity * .85f;
                StickTime++;
            }
            AccumulatedVel -= 0.6f;
        }

        if (State == CurrentState.HitGround)
        {
            FlailAmt = MathHelper.Clamp(FlailAmt - 0.015f, 0f, 1f);
            Projectile.rotation -= MathF.Sin(AccumulatedVel * (MathHelper.TwoPi * 2f)) * 0.2f * FlailAmt * Projectile.direction;
            AccumulatedVel -= 0.6f;
        }

        Projectile.Opacity = InverseLerp(0f, 14f * Projectile.MaxUpdates, Projectile.timeLeft, true);
    }

    private void SetCollided(bool stick)
    {
        Projectile.extraUpdates = 1;
        State = stick ? CurrentState.HitGround : CurrentState.HitEnemy;
        FlailAmt = 1f;
        Projectile.timeLeft = stick ? 150 : 120;
        if (stick)
        {
            Projectile.tileCollide = false;
            SoundID.Item108.Play(Projectile.Center, .3f, 1f, .2f, null, 20, Name);
        }

        this.Sync();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (State == 0f)
            SetCollided(true);

        Projectile.velocity *= 0.01f;
        Projectile.Center += oldVelocity * 3f;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = Projectile.BaseRotHitbox().Right;
        for (int i = 0; i < 20; i++)
        {
            if (i < 4)
                ParticleRegistry.SpawnBloodStreakParticle(pos, Projectile.velocity.SafeNormalize(Vector2.Zero),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), Color.DarkRed);
            ParticleRegistry.SpawnGlowParticle(pos, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 9f),
                Main.rand.Next(30, 50), Main.rand.NextFloat(20f, 30f), Color.DarkRed, .8f);
        }

        // Stick to the target
        if (target.life > 0)
        {
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            SetCollided(false);
        }
    }

    public override bool? CanDamage()
    {
        return State == CurrentState.Thrown ? null : false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (State == CurrentState.HitEnemy)
        {
            behindNPCsAndTiles.Add(index);
        }
        else
        {
            Projectile.hide = false;
        }
    }

    public float WidthFunct(float c)
    {
        return OptimizedPrimitiveTrail.HemisphereWidthFunct(c, MathHelper.SmoothStep(Projectile.height * .9f, 0f, c) * Projectile.scale * 1.5f);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color color = Color.Lerp(Color.Crimson, Color.DarkRed, Main.GlobalTimeWrappedHourly + c.X);
        float speed = Utils.GetLerpValue(0f, 60f, AccumulatedVel, true);
        return color * speed * Projectile.Opacity * InverseLerp(20f * Projectile.MaxUpdates, 0f, StickTime);
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            if (AccumulatedVel > 2f)
            {
                ManagedShader shader = ShaderRegistry.FlameTrail;

                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkRidgeNoise), 1);
                trail.DrawTrail(shader, cache.Points, 200, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 direction = Projectile.rotation.ToRotationVector2() * 10f;

        Vector2 pos = Projectile.Center + direction - Main.screenPosition;
        Vector2 orig = texture.Size() * new Vector2(1f, 0.5f);

        for (int i = 0; i < 8; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / 8 + Main.GlobalTimeWrappedHourly).ToRotationVector2() * 5f;
            Color col = Color.DarkRed with { A = 0 } * 0.95f * Animators.MakePoly(4f).OutFunction(Projectile.Opacity);
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, col, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        }

        Main.EntitySpriteDraw(texture, pos, null, lightColor * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        return false;
    }
}