using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class HorsemenSwing : BaseSwordSwing
{
    public override string Texture => ItemID.TheHorsemansBlade.GetTerrariaItem();
    public override int SwingTime => Item.useAnimation;

    public override void SafeInitialize()
    {
        after ??= new(8, () => Projectile.Center);

        // Reset arrays
        after.Clear();
    }
    public override void SafeAI()
    {
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);
        Projectile.rotation = SwingOffset();

        if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
        {
            SoundID.Item1.Play(Projectile.Center, 1f, 0f, .2f);
            PlayedSound = true;
        }

        after?.UpdateFancyAfterimages(new(Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 0, 3));

        float scale = MeleeScale * 1.48f;
        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * scale;
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, scale, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

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
                VanishTime++;
                this.Sync();
            }
        }
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Dust d = Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(10f, 10f), DustID.Torch, SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(6f, 12f),
                0, default, Main.rand.NextFloat(1.9f, 2.8f));
            d.noGravity = true;
        }
        if (!npc.immortal && !npc.SpawnedFromStatue && !NPCID.Sets.CountsAsCritter[npc.type])
            Owner.HorsemansBlade_SpawnPumpkin(npc.whoAmI, Projectile.damage, Projectile.knockBack);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing. These must be done here otherwise silly things WILL happen.
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

        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [new(255, 255, 204), new(255, 255, 0), new(244, 184, 0), new(254, 158, 35), new(252, 95, 4)], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }
}

public class HorsemenDive : ModProjectile
{
    public override string Texture => ItemID.TheHorsemansBlade.GetTerrariaItem();

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 500;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
        Projectile.scale = 1.48f;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
        writer.WriteVector2(End);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
        End = reader.ReadVector2();
    }

    public ref float Time => ref Projectile.ai[0];
    public const int WaitTime = 15;
    public const int DiveTime = 30;
    public const int FadeTime = 10;
    public const int Width = 20;
    public float MaxDist => 600f * Owner.GetTotalAttackSpeed(DamageClass.Melee);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public RotatedRectangle Rect()
    {
        return new(Width * Projectile.scale, Projectile.Center, Projectile.Center + PolarVector(76f * Projectile.scale, Projectile.rotation - MathHelper.PiOver4));
    }
    public override void AI()
    {
        if (Time == 0f)
        {
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);

            Start = Projectile.Center;
            Projectile.netUpdate = true;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.velocity = Vector2.Zero;

        Vector2 expectedEnd = Start + Projectile.velocity * MaxDist;
        Vector2? tile = RaytraceTiles(Start, expectedEnd);
        if (tile.HasValue)
            End = tile.Value - Projectile.velocity * Owner.height;
        else
            End = expectedEnd;

        if (Time < WaitTime)
        {
            float comp = InverseLerp(0f, WaitTime, Time);
            Projectile.rotation = MathHelper.PiOver4 + Projectile.velocity.ToRotation() - MathHelper.PiOver2 * MakePoly(3f).InOutFunction.Evaluate(1f, 0f, comp) * Dir * Owner.gravDir;
        }
        else if (Time == WaitTime)
            SoundID.Item73.Play(Projectile.Center, 1.1f, -.2f, 0f, null, 20, Name);
        else if (Time < (WaitTime + DiveTime))
        {
            float comp = MakePoly(4f).OutFunction(InverseLerp(WaitTime, WaitTime + DiveTime, Time));
            Owner.Center = Vector2.Lerp(Start, End, comp);

            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Rect().RandomPoint(), DustID.Torch, -(Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * Main.rand.NextFloat(2f, 8f),
                    0, default, Main.rand.NextFloat(1.7f, 2.1f));
                d.noGravity = true;
            }
        }
        else if (Time <= (WaitTime + DiveTime + FadeTime))
        {
            Projectile.Opacity = 1f - InverseLerp(WaitTime + DiveTime, WaitTime + DiveTime + FadeTime, Time);
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();
        }
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved();

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Owner.GiveIFrames(7);
        ParticleRegistry.SpawnTwinkleParticle(Rect().Top, Vector2.Zero, 40, Vector2.One, Color.OrangeRed, 4);
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = Projectile.rotation.ToRotationVector2().RotatedByRandom(.4f) * Main.rand.NextFloat(9f, 17f);
            Dust d = Dust.NewDustPerfect(Rect().Top + Main.rand.NextVector2Circular(10f, 10f), DustID.Torch, vel,
                0, default, Main.rand.NextFloat(1.9f, 2.8f));
            d.noGravity = true;
        }

        if (!target.immortal && !target.SpawnedFromStatue && !NPCID.Sets.CountsAsCritter[target.type])
            Owner.HorsemansBlade_SpawnPumpkin(target.whoAmI, Projectile.damage, Projectile.knockBack);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Rect().Intersects(targetHitbox);

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();

        Vector2 origin;
        bool flip = Dir == -1;

        float off;
        SpriteEffects fx;
        if (flip)
        {
            origin = new Vector2(0, tex.Height);

            off = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(tex.Width, tex.Height);

            off = PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity,
            Projectile.rotation + off, origin, Projectile.scale, fx, 0f);

        return false;
    }
}