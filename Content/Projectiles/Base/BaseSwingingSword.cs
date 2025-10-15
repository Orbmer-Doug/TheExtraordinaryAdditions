using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Base;

/// <summary>
/// Creates a new held sword projectile <br></br>
/// The derived class of all swords in the mod <br></br>
/// Any <see cref="AdditionsGlobalProjectile.ExtraAI"/> at and before 6 is taken and all of <see cref="Projectile.ai"/>
/// </summary>
/// Tip: bring up desmos scientific and use round(sqrt(width^2 + height^2)) to get accurate sizes (cause the sprites usually diagonal) 
public abstract class BaseSwordSwing : ModProjectile, ILocalizedModType, IModType
{
    #region Variables
    public enum SwingDirection : sbyte
    {
        Down = 1,
        Up = -1,
    }

    public AdditionsProjectileInfo ProjInfo => Projectile.AdditionsInfo();
    public Texture2D Tex => Projectile.ThisProjectileTexture();
    public ref float Time => ref Projectile.ai[0];

    /// <summary>
    /// Hitlag
    /// </summary>
    public ref float TimeStop => ref Projectile.ai[1];
    public bool PlayedSound
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float VanishTime => ref ProjInfo.ExtraAI[0];
    public ref float OverallTime => ref ProjInfo.ExtraAI[1];
    public ref float RotationOffset => ref ProjInfo.ExtraAI[2];
    public bool Initialized
    {
        get => ProjInfo.ExtraAI[3] == 1f;
        set => ProjInfo.ExtraAI[3] = value.ToInt();
    }
    public ref float InitialMouseAngle => ref ProjInfo.ExtraAI[4];
    public ref float InitialAngle => ref ProjInfo.ExtraAI[5];
    public SwingDirection SwingDir
    {
        get => (SwingDirection)ProjInfo.ExtraAI[6];
        set => ProjInfo.ExtraAI[6] = (int)value;
    }
    public float[] OldRotations = new float[5];
    public SpriteEffects Effects
    {
        get => (SpriteEffects)Projectile.spriteDirection;
        set => Projectile.spriteDirection = (int)value;
    }
    public int Direction
    {
        get => Projectile.direction;
        set => Projectile.direction = value;
    }
    public virtual int MaxUpdates
    {
        get;
        set;
    } = 3;

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Item => Owner.HeldItem;
    public float MeleeScale => Owner.GetAdjustedItemScale(Item);
    public float MeleeSpeed => Owner.GetTotalAttackSpeed(DamageClass.MeleeNoSpeed);

    /// <summary>
    /// The owners center
    /// </summary>
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);

    /// <summary>
    /// A quick way to access the current light level of this sword
    /// </summary>
    public float Brightness
    {
        get
        {
            Point point = Projectile.Center.ToTileCoordinates();
            return Lighting.Brightness(point.X, point.Y);
        }
    }

    public virtual float SwordRotation => PiOver4;
    public virtual float SwingAngle => TwoPi / 3f;
    public virtual int SwingTime => 40;
    public int MaxTime => (int)(SwingTime * MaxUpdates / MeleeSpeed);
    public virtual int StopTimeFrames => 4;
    public virtual float StopTime => (StopTimeFrames - (int)((MeleeSpeed - 1) * 5f)) * MaxUpdates;

    /// <summary>
    /// The difference in rotation based on the last frame.
    /// </summary>
    public float AngularVelocity => Abs(WrapAngle(Projectile.rotation - OldRotations[1]));

    public float SwingCompletion => InverseLerp(0f, MaxTime, Time, true);
    public Vector2 SwordDir;

    /// <summary>
    /// Controls the easing for <see cref="SwingOffset"/>
    /// </summary>
    /// <returns></returns>
    public virtual float Animation()
    {
        return Animators.Exp(2.2f).InOutFunction.Evaluate(Time, 0f, MaxTime, -1f, 1f);
    }

    public virtual float SwingOffset()
    {
        return SwordRotation + InitialMouseAngle + SwingAngle * Animation() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;
    }

    /// <summary>
    /// The rotating hitbox of this sword. Likely need this to be tweaked for each sword.
    /// <br></br>
    /// Defaults to the one for <see cref="ExampleSwing"/>
    /// </summary>
    /// <returns></returns>
    public virtual RotatedRectangle Rect()
    {
        float width = MathF.Min(Tex?.Height ?? 1, Tex?.Width ?? 1) / 3 * Projectile.scale;
        float height = Sqrt((Tex?.Height ?? 1).Squared() + (Tex?.Width ?? 1).Squared()) * Projectile.scale;
        return new(width, Projectile.Center, Projectile.Center + PolarVector(height, Projectile.rotation - SwordRotation));
    }
    #endregion

    #region Netwerking
    public sealed override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((sbyte)Projectile.direction);
        writer.Write((float)Projectile.rotation);
        writer.Write((sbyte)Projectile.spriteDirection);
        WriteExtraAI(writer);
    }
    public sealed override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.direction = (sbyte)reader.ReadSByte();
        Projectile.rotation = (float)reader.ReadSingle();
        Projectile.spriteDirection = (sbyte)reader.ReadSByte();
        GetExtraAI(reader);
    }
    public virtual void WriteExtraAI(BinaryWriter writer) { }
    public virtual void GetExtraAI(BinaryReader reader) { }
    #endregion
    public virtual void Defaults() { }
    public virtual void StaticDefaults() { }
    public sealed override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false; // Manual
        StaticDefaults();
    }
    public sealed override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.MeleeNoSpeed;

        Projectile.timeLeft = 10000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;

        Projectile.penetrate = -1;
        Projectile.MaxUpdates = MaxUpdates;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.ContinuouslyUpdateDamageStats = true;

        Projectile.noEnchantmentVisuals = true;
        Projectile.netImportant = true;
        Defaults();
    }

    #region Collision

    /// <summary>
    /// Defaults to <see cref="Rect"/> seeing if it intersects with a target.
    /// </summary>
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Rect().Intersects(targetHitbox);
    }

    public override bool? CanDamage() => SwingCompletion.BetweenNum(.3f, .8f, true) ? null : false;

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().BottomLeft, Rect().TopRight, Rect().Width, DelegateMethods.CutTiles);
    }

    public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ItemLoader.OnHitNPC(Owner.HeldItem, Owner, target, hit, damageDone);
        NPCLoader.OnHitByItem(target, Owner, Owner.HeldItem, hit, damageDone);
        PlayerLoader.OnHitNPC(Owner, target, hit, damageDone);

        RotatedRectangle rect = Rect();

        // Try to get a accurate point of collision
        if (CheckLinearCollision(rect.BottomLeft, rect.TopRight, target.Hitbox, out Vector2 start, out Vector2 end))
        {
            NPCHitEffects(start, end, target, hit);
        }

        // Otherwise just choose a random spot to not break cohesion.
        else
            NPCHitEffects(target.RotHitbox().RandomPoint(), target.RotHitbox().RandomPoint(), target, hit);

        if (Main.netMode == NetmodeID.Server && target.whoAmI < Main.maxNPCs)
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, target.whoAmI);
        if (Main.netMode != NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, target.whoAmI, -1f);
    }

    public sealed override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        ItemLoader.OnHitPvp(Owner.HeldItem, Owner, target, info);

        RotatedRectangle rect = Rect();

        // Try to get a accurate point of collision
        if (CheckLinearCollision(rect.BottomLeft, rect.TopRight, target.Hitbox, out Vector2 start, out Vector2 end))
        {
            PlayerHitEffects(start, end, target, info);
        }

        // Otherwise just choose a random spot to not break cohesion.
        else
            PlayerHitEffects(target.RotHitbox().RandomPoint(), target.RotHitbox().RandomPoint(), target, info);
    }

    public virtual void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit) { }
    public virtual void PlayerHitEffects(in Vector2 position, in Vector2 end, Player player, Player.HurtInfo info) { }
    #endregion

    public sealed override void AI()
    {
        if (this.RunLocal() && !Owner.Available())
        {
            KillEffect();
        }

        Projectile.width = Tex?.Width ?? 1;
        Projectile.height = Tex?.Height ?? 1;

        if (TimeStop <= 0f)
        {
            for (int i = OldRotations.Length - 1; i > 0; i--)
            {
                OldRotations[i] = OldRotations[i - 1];
            }

            OldRotations[0] = Projectile.rotation;
        }

        if (!Initialized)
        {
            SafeInitialize();

            Projectile.ResetLocalNPCHitImmunity();

            // Reset time and sync
            if (this.RunLocal())
            {
                PlayedSound = false;

                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
                Direction = Projectile.velocity.X.NonZeroSign();
                InitialAngle = SwingOffset();
                InitialMouseAngle = Projectile.velocity.ToRotation();
                Time = 0f;

                this.Sync();
                Initialized = true;
            }
        }

        SafeAI();
        SwordDir = (Projectile.rotation - SwordRotation + PiOver2).ToRotationVector2() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;

        if (!Main.dedServ)
            ProduceWaterRipples();

        if (TimeStop <= 0f)
            Time++;
        if (TimeStop > 0f)
            TimeStop--;

        OverallTime++;
    }

    /// <summary>
    /// Makes some garnular water ripples at the right place
    /// </summary>
    public void ProduceWaterRipples()
    {
        WaterShaderData water = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
        float power = 12f;
        float waveSine = 1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
        Vector2 size = Projectile.Size / 2f;
        Vector2 ripplePos = Rect().Center;
        Color waveData = new Color(power, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
        water.QueueRipple(ripplePos, waveData, size, RippleShape.Square, SwordDir.ToRotation());
    }

    public virtual void SafeInitialize() { }

    public virtual void SafeAI() { }

    /// <summary>
    /// Defaults to just killing the projectile
    /// </summary>
    public virtual void KillEffect()
    {
        Projectile.Kill();
        return;
    }

    /// <summary>
    /// Glue to the player
    /// </summary>
    public override bool ShouldUpdatePosition() => false;

    public void Debug()
    {
        Texture2D pix = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Vector2 orig = pix.Size() / 2f;
        float size = 10f;

        // Rotation differences
        for (int i = 0; i <= 400; i += 10)
        {
            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * i - Main.screenPosition;
            Main.spriteBatch.Draw(pix, pos, null, Color.DarkRed, Projectile.rotation, orig, size, 0, 0f);

            pos = Projectile.Center + (Projectile.rotation + SwordRotation).ToRotationVector2() * i - Main.screenPosition;
            Main.spriteBatch.Draw(pix, pos, null, Color.LawnGreen, Projectile.rotation + SwordRotation, orig, size, 0, 0f);

            pos = Projectile.Center + (Projectile.rotation - SwordRotation).ToRotationVector2() * i - Main.screenPosition;
            Main.spriteBatch.Draw(pix, pos, null, Color.Blue, Projectile.rotation - SwordRotation, orig, size, 0, 0f);
        }

        // The hitbox
        Main.spriteBatch.RenderRectangle(Rect());

        // Sides
        Main.spriteBatch.Draw(pix, Rect().Top - Main.screenPosition, null, Color.Purple, Projectile.rotation - SwordRotation, orig, size, 0, 0f);
        Main.spriteBatch.Draw(pix, Rect().Bottom - Main.screenPosition, null, Color.Yellow, Projectile.rotation - SwordRotation, orig, size, 0, 0f);
        Main.spriteBatch.Draw(pix, Rect().Left - Main.screenPosition, null, Color.Pink, Projectile.rotation - SwordRotation, orig, size, 0, 0f);
        Main.spriteBatch.Draw(pix, Rect().Right - Main.screenPosition, null, Color.White, Projectile.rotation - SwordRotation, orig, size, 0, 0f);
    }
}