using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Early;

public class ShockJavelin : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ShockJavelin);
    private const int StartingLife = 200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.extraUpdates = 10;
        Projectile.timeLeft = StartingLife;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
        Projectile.hide = true;
    }

    public ref float FlailAmt => ref Projectile.localAI[0];
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
    private readonly List<int> PreviousNPCs = [-1];
    public ref float Timer => ref Projectile.localAI[1];
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(StripWidth, StripColor, null, 120);
        cache ??= new(120);
        cache.Update(Projectile.Center);

        if (State == CurrentState.Thrown)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

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
                AccumulatedVel += Projectile.velocity.Length() * 0.05f;
                Projectile.velocity *= .995f;
            }

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
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
            AccumulatedVel -= 0.6f;
        }

        if (State == CurrentState.HitGround)
        {
            FlailAmt = MathHelper.Clamp(FlailAmt - 0.015f, 0f, 1f);
            Projectile.rotation -= MathF.Sin(AccumulatedVel * (MathHelper.TwoPi * 2f)) * 0.4f * FlailAmt * Projectile.direction;
            AccumulatedVel -= 0.6f;
        }

        Projectile.Opacity = Projectile.scale = InverseLerp(0f, 10f, Projectile.timeLeft, true);
    }

    public override void OnKill(int timeLeft)
    {
        SoundStyle zap = SoundID.DD2_LightningAuraZap;
        zap.MaxInstances = 0;
        zap.Pitch = .18f;
        zap.PitchVariance = 0.3f;
        SoundStyle killSound = zap;
        SoundEngine.PlaySound(killSound, (Vector2?)Projectile.Center, null);
        for (int i = 0; i < 9; i++)
        {
            Color randomColor = Color.Lerp(Color.MediumPurple, Color.BlueViolet, Main.rand.NextFloat());
            randomColor.A = 0;
            Dust.NewDustDirect(Projectile.Center - new Vector2(5f), 10, 10, DustID.AncientLight, 0f, 0f, 0, randomColor, 2f).noGravity = true; // 306
        }
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
            SoundStyle item = SoundID.Item108;
            item.MaxInstances = 0;
            item.Pitch = 1f;
            item.PitchVariance = 0.2f;
            item.Volume = 0.3f;
            SoundStyle attachSound = item;
            SoundEngine.PlaySound(attachSound, Projectile.Center, null);
        }

        Projectile.netUpdate = true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (State == 0f)
        {
            SetCollided(true);
        }

        Projectile.velocity *= 0.01f;
        Projectile.Center += oldVelocity * 3f;
        return false;
    }

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

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Create some on hit particles
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.height * .5f;
        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, Main.rand.Next(12, 15), 3f, Color.White, Color.Purple, 1.4f);
        for (int i = 0; i < 14; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.12f) * Main.rand.NextFloat(2f, 5f);
            ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), Color.Purple.Lerp(Color.White, Main.rand.NextFloat(.4f, .6f)));
        }

        // Add the target to the list
        PreviousNPCs.Add(target.whoAmI);
        target.immune[Projectile.owner] = 20;

        // Stick to the final target
        if (Projectile.numHits >= 1)
        {
            // Set the sticking variables
            if (target.life > 0)
            {
                Projectile.tileCollide = false;
                EnemyID = target.whoAmI;
                offset = Projectile.position - target.position;
                offset -= Projectile.velocity;

                SetCollided(false);
            }
        }
        else
            SeekNPC();
    }

    public void SeekNPC()
    {
        float range = 400f;
        int targetNPC = -1;
        for (int i = 0; i < Main.npc.Length; i++)
        {
            NPC target = Main.npc[i];
            if (target.CanBeChasedBy(Projectile, false) && !PreviousNPCs.Contains(i))
            {
                float distance = Vector2.Distance(target.Center, Projectile.Center);
                if (distance < range && Collision.CanHit(Projectile, target))
                {
                    range = distance;
                    targetNPC = i;
                }
            }
        }

        if (targetNPC != -1f)
        {
            Projectile.velocity = Projectile.SafeDirectionTo(Main.npc[targetNPC].Center) * 7f;
        }
    }

    public override bool? CanDamage()
    {
        return State == CurrentState.Thrown;
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

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            if (AccumulatedVel > 2f)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
                shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 5f);
                trail.DrawTrail(shader, cache.Points, 200, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();

        Vector2 origin = texture.Size() * 0.5f;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 direction = Projectile.rotation.ToRotationVector2() * 10f;

        Vector2 pos = Projectile.Center + direction - Main.screenPosition;
        Vector2 orig = texture.Size() * new Vector2(1f, 0.5f);
        for (int i = 0; i < 5; i++)
        {
            Vector2 offset = new Vector2(1f).RotatedBy((double)(MathHelper.PiOver2 * i + Projectile.rotation), default);
            Main.EntitySpriteDraw(texture, pos + offset * 2f, null, new Color(154, 92, 236, 0) * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale, 0, 0f);
            Main.EntitySpriteDraw(texture, pos + offset, null, new Color(234, 164, 244, 0) * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        }

        Main.EntitySpriteDraw(texture, pos, null, lightColor * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        return false;
    }

    internal float StripWidth(float c)
    {
        return OptimizedPrimitiveTrail.HemisphereWidthFunct(c, MathHelper.SmoothStep(Projectile.height * .9f, 0f, c) * Projectile.scale * 1.5f);
    }

    internal Color StripColor(SystemVector2 c, Vector2 position)
    {
        Color color = Color.Lerp(new Color(234, 164, 244, 0), new Color(154, 92, 236, 128), Main.GlobalTimeWrappedHourly + c.X);
        float speed = Utils.GetLerpValue(0f, 60f, AccumulatedVel, true);
        return color * speed * Projectile.Opacity;
    }
}
