using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
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

    public AdditionsGlobalProjectile ModdedProj => Projectile.Additions();
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
    public ref float VanishTime => ref ModdedProj.ExtraAI[0];
    public ref float OverallTime => ref ModdedProj.ExtraAI[1];
    public ref float RotationOffset => ref ModdedProj.ExtraAI[2];
    public bool Initialized
    {
        get => ModdedProj.ExtraAI[3] == 1f;
        set => ModdedProj.ExtraAI[3] = value.ToInt();
    }
    public ref float InitialMouseAngle => ref ModdedProj.ExtraAI[4];
    public ref float InitialAngle => ref ModdedProj.ExtraAI[5];
    public SwingDirection SwingDir
    {
        get => (SwingDirection)ModdedProj.ExtraAI[6];
        set => ModdedProj.ExtraAI[6] = (int)value;
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

public class ExampleSwingItem : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ImpureAstralKatanas);

    public override void SetDefaults()
    {
        // normal item thingymajigs
        Item.damage = 50;
        Item.knockBack = 0f;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.rare = ItemRarityID.Blue;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<ExampleSwing>();
        Item.shootSpeed = 0f;
        Item.noMelee = Item.noUseGraphic = true;
    }
    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer, 0f, 0f, 0f);
        return false;
    }
}

public class ExampleSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ImpureAstralKatanas);
    public ref float HitRatio => ref ModdedProj.ExtraAI[7];

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        after ??= new(8, () => Projectile.Center);

        // Reset arrays
        after.afterimages = null;
        old.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Projectile.rotation = SwingOffset();

        // swoosh
        if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
        {
            AdditionsSound.BraveSwingMedium.Play(Projectile.Center, 1f, 0f, .2f);
            PlayedSound = true;
        }

        // Create the trail if needed
        if (trail == null || trail._disposed)
            trail = new(Tip, WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 15 * MaxUpdates);

        // Update trails
        if (TimeStop <= 0f)
        {
            // We subtract by the owners center here to later offset the points all at once in drawing
            // This makes the trail points glue to the sword rather than acting on their own terms
            // Also add velocity cause uhh desync shenanigans
            old.Update(Rect().Top + Owner.velocity - Center);

            if (Time % 2 == 1)
            {
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 200, 3, Animation() * 3f));
            }
        }

        // Smoothly decrease the hit effects
        HitRatio = Clamp(MakePoly(3f).OutFunction.Evaluate(HitRatio, -.25f, .01f), 0f, 1f);

        float scaleUp = MeleeScale * 2f;
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
                VanishTime++;
                this.Sync();
            }
        }

        CreateSparkles();
    }

    public void CreateSparkles()
    {
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .04f || Time < 5f || Main.dedServ)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = Rect().RandomPoint();
            Vector2 vel = -SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color color = Color.DarkViolet;

            ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, color, false, false, null);
        }

        // tip
        ParticleRegistry.SpawnSparkleParticle(old.Points[0] + Projectile.Center, -SwordDir * Main.rand.NextFloat(4f, 10f), Main.rand.Next(30, 50),
            Main.rand.NextFloat(.4f, .6f), Color.MediumPurple, Color.DarkViolet, Main.rand.NextFloat(.7f, 1.2f), Main.rand.NextFloat(-.1f, .1f));

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(1.2f, 1.9f);
            Color color = Color.BlueViolet.Lerp(Color.Violet, Main.rand.NextFloat(.2f, .5f));
            ParticleRegistry.SpawnSquishyPixelParticle(start + Main.rand.NextVector2Circular(14f, 14f), vel, life, scale, color, Color.Violet);
        }
        npc.velocity += SwordDir * 8f * npc.knockBackResist;

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.BraveSmashH01.Play(start, 1f, 0f, .3f);
        TimeStop = StopTime;
        HitRatio = 1f;
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(.9f, 1.5f);
            Color color = Color.BlueViolet;
            ParticleRegistry.SpawnSquishyPixelParticle(start + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale, color, Color.Violet);
        }

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.BraveSmashH01.Play(start, 1f, 0f, .3f);
        TimeStop = StopTime;
        HitRatio = 1f;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Make it a crit if the strike was with the very tip
        if (new RotatedRectangle(30f, Rect().Top - PolarVector(10f, Projectile.rotation - SwordRotation),
            Rect().Top + PolarVector(10f, Projectile.rotation - SwordRotation)).Intersects(target.Hitbox))
        {
            modifiers.SetCrit();
        }
    }

    public OptimizedPrimitiveTrail trail;
    public static readonly ITrailTip Tip = new RoundedTip(25);
    public FancyAfterimages after;
    public TrailPoints old = new(25);

    public static float WidthFunct(float c)
    {
        return SmoothStep(1f, 0f, c) * 20f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.016f, 0.07f, AngularVelocity);

        return MulticolorLerp(c.X, Color.MediumPurple, Color.Purple, Color.DarkViolet) * opacity;
    }

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

        // Prepare the zany trail
        void draw()
        {
            if (trail == null || old == null)
                return;

            ManagedShader shader = ShaderRegistry.EnlightenedBeam;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 2);
            trail.DrawTippedTrail(shader, old.Points, Tip, true, 150, true);
        }

        // Do a little strike glow
        Color col = Color.Lerp(Color.Purple, Color.MediumPurple, HitRatio) with { A = 0 } * 10.5f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 off = (TwoPi * InverseLerp(0f, 8, i)).ToRotationVector2() * 4f * HitRatio;
            Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition + off, null, col,
                Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        }

        // Not manually setting the rotation offset and sprite effects here caused a latency between frames where rarely a artifact would occur
        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.Violet * .8f * Brightness], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        // Draw the main sword
        // Alternatively can be drawn with shaders for more advanced effects
        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        // Queue the trail for drawing
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}