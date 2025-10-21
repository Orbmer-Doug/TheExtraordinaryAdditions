using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Shaders;
using Terraria.GameInput;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;
using SwingDirection = TheExtraordinaryAdditions.Content.Projectiles.Base.BaseSwordSwing.SwingDirection;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla;

public class FancyTool : ModProjectile, ILocalizedModType, IModType
{
    #region Variables
    public sealed override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Item Item => Owner.HeldItem;
    public AdditionsProjectileInfo ProjInfo => Projectile.AdditionsInfo();
    public Texture2D Tex => Item.ThisItemTexture();
    public ref float Time => ref Projectile.ai[0];
    public ref float OverallTime => ref Projectile.ai[1];
    public bool PlayedSound
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float VanishTime => ref ProjInfo.ExtraAI[0];
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
    public const int MaxUpdates = 3;

    /// <summary>
    /// The owners center
    /// </summary>
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter);

    public const float ToolRotation = PiOver4;
    public float Scale => 1.1f * Owner.GetAdjustedItemScale(Item);
    public int MaxTime => (int)(Item.useAnimation * Owner.pickSpeed * MaxUpdates / Owner.GetTotalAttackSpeed(Projectile.DamageType));

    public float SwingCompletion => InverseLerp(0f, MaxTime, Time, true);
    public Vector2 ToolDir;

    public const float Wait = .2f;
    public const float Swing = .8f;
    public float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, -1.2f, Wait, MakePoly(2f).OutFunction) // Reel
            .Add(-1.2f, .9f, Swing, MakePoly(4f).InFunction) // Swing
            .Add(.9f, 1f, 1f, MakePoly(2f).OutFunction) // End-Swing
            .Evaluate(SwingCompletion);
    }

    public float SwingOffset()
    {
        return ToolRotation + InitialMouseAngle + (PiOver2 + .2f) * Animation() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;
    }

    public RotatedRectangle Rect()
    {
        float width = MathF.Min(Tex?.Height ?? 1, Tex?.Width ?? 1) / 3 * Projectile.scale;
        float height = Sqrt((Tex?.Height ?? 1).Squared() + (Tex?.Width ?? 1).Squared()) * Projectile.scale;
        return new(width, Projectile.Center, Projectile.Center + PolarVector(height, Projectile.rotation - ToolRotation));
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
    public sealed override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false; // Manual
    }
    public sealed override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.MeleeNoSpeed;

        Projectile.timeLeft = 10000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ownerHitCheck = true;
        Projectile.ownerHitCheckDistance = 700f;

        Projectile.penetrate = -1;
        Projectile.MaxUpdates = MaxUpdates;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.ContinuouslyUpdateDamageStats = true;

        Projectile.noEnchantmentVisuals = true; // Manual
        Projectile.netImportant = true;
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

    public override bool? CanCutTiles() => CanDamage();

    public override void CutTiles()
    {
        bool[] tileCutIgnorance = Owner.GetTileCutIgnorance(allowRegrowth: Item.type == ItemID.StaffofRegrowth || Item.type == ItemID.AcornAxe, Projectile.trap);
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        DelegateMethods.tileCutIgnore = tileCutIgnorance;
        Utils.PlotTileLine(Rect().BottomLeft, Rect().TopRight, Rect().Width, DelegateMethods.CutTiles);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ItemLoader.OnHitNPC(Item, Owner, target, hit, damageDone);
        NPCLoader.OnHitByItem(target, Owner, Item, hit, damageDone);
        PlayerLoader.OnHitNPC(Owner, target, hit, damageDone);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        ItemLoader.OnHitPvp(Item, Owner, target, info);
    }

    #endregion

    public FancyAfterimages after;
    public sealed override void AI()
    {
        if (this.RunLocal() && (!Owner.Available() || Item == null || (Item.pick <= 0 && Item.axe <= 0 && Item.hammer <= 0)))
        {
            Projectile.Kill();
            return;
        }

        Projectile.width = Tex?.Width ?? 1;
        Projectile.height = Tex?.Height ?? 1;

        if (!Initialized)
        {
            after ??= new(7, () => Projectile.Center);

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

        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - ToolRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);
        Projectile.rotation = SwingOffset();
        Projectile.damage = Item.damage;
        Projectile.knockBack = Item.knockBack;
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetCompositeArmFront(true, 0, (Projectile.rotation - PiOver4 - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f));
        Owner.toolTime = 4; // Nuh-uh
        Projectile.timeLeft = 1000;

        if (Owner.itemAnimation < 2)
            Owner.itemAnimation = 2;
        if (Owner.itemTime < 2)
            Owner.itemTime = 2;

        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * Scale;
        }
        else
        {
            Projectile.scale = MakePoly(2f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, Scale, 0f);
            if (Projectile.scale <= 0f)
                Projectile.Kill();
            VanishTime++;
        }

        // Reset or vanish at the end of the swing if the player is still using the item
        if (SwingCompletion >= 1f)
        {
            if ((this.RunLocal() &&
                (Owner.altFunctionUse == ItemAlternativeFunctionID.None && Modded.SafeMouseLeft.Current) ||
                (Owner.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed && Modded.SafeMouseRight.Current)) && VanishTime <= 0)
            {
                Owner.SetDummyItemTime(Owner.itemAnimationMax);
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

        if (SwingCompletion.BetweenNum(Wait + .3f, Swing, true) && !PlayedSound)
        {
            if (Owner.whoAmI == Main.myPlayer)
            {
                bool ham = Item.hammer > 0;
                ToolModifierUtils.Mine(Owner, Item, ham, ham && Modded.SafeMouseRight.Current);

                if (Item.createTile != -1)
                {
                    Point tile = ToolModifierUtils.GetTileTarget(Owner);
                    bool canPlace = false;
                    bool newObjectType = false;
                    bool? overrideCanPlace = null;
                    int? forcedRandom = null;
                    TileObject objectData = default(TileObject);
                    Owner.FigureOutWhatToPlace(Main.tile[tile], Item, out int tileToCreate, out int previewPlaceStyle, out overrideCanPlace, out forcedRandom);
                    PlantLoader.CheckAndInjectModSapling(tile.X, tile.Y, ref tileToCreate, ref previewPlaceStyle);
                    if (overrideCanPlace.HasValue)
                        canPlace = overrideCanPlace.Value;
                    else if (!TileLoader.CanPlace(tile.X, tile.Y, tileToCreate))
                        canPlace = false;
                    else if (TileObjectData.CustomPlace(tileToCreate, previewPlaceStyle) && tileToCreate != 82 && tileToCreate != 227)
                    {
                        newObjectType = true;
                        canPlace = TileObject.CanPlace(tile.X, tile.Y, (ushort)tileToCreate, previewPlaceStyle, Owner.direction, out objectData, onlyCheck: false, forcedRandom);
                    }
                    else
                        canPlace = Owner.PlaceThing_Tiles_BlockPlacementForAssortedThings(canPlace);
                    if (canPlace)
                        Owner.PlaceThing_Tiles_PlaceIt(newObjectType, objectData, tileToCreate);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, tile.X, tile.Y, tileToCreate, Item.placeStyle);
                }
            }

            Item.UseSound?.Play(Projectile.Center, 1f, 0f, .1f);
            PlayedSound = true;
        }

        bool side = false;
        if (Direction == 1 && SwingDir == SwingDirection.Down)
            side = true;
        else if (Direction == -1 && SwingDir == SwingDirection.Up)
            side = true;
        RotationOffset = side ? 0f : PiOver2;
        Effects = side ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One,
            Projectile.Opacity, Projectile.rotation, Effects, 250, 0, 0f, null, false, 0f));
        if (Projectile.FinalExtraUpdate())
        {
            Vector2 point = Rect().RandomPoint();
            Projectile.EmitEnchantmentVisualsAt(point, 1, 1);
            ItemLoader.MeleeEffects(Item, Owner, point.ToRectangle(1, 1));
            Owner.ItemCheck_EmitUseVisuals(Owner.HeldItem, point.ToRectangle(1, 1));
            Item.position = Rect().Center;
            Item.UpdateItem_VisualEffects();
            ItemSpecificLogic();
        }
        ToolDir = (Projectile.rotation - ToolRotation + PiOver2).ToRotationVector2() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;

        if (!Main.dedServ)
            ProduceWaterRipples();

        Time++;
        OverallTime++;
    }

    public void ItemSpecificLogic()
    {
        // Allows for continuous breaking of herbs
        if ((Item.type == ItemID.StaffofRegrowth || Item.type == ItemID.AcornAxe) && Owner.whoAmI == Main.myPlayer)
        {
            Point target = ToolModifierUtils.GetTileTarget(Owner);
            if (Main.tile[target].HasTile)
                Owner.PlaceThing_Tiles_BlockPlacementForAssortedThings(!TileLoader.CanPlace(target.X, target.Y, Main.tile[target].TileType));
        }

        if (this.RunLocal())
        {
            if (Item.type == ItemID.Hammush)
            {
                int num = Owner.itemAnimationMax;
                if (CanDamage() == null && OverallTime % 4 == 3)
                    Projectile.NewProjectile(Owner.GetProjectileSource_Item(Item), Rect().RandomPoint(), ToolDir * Main.rand.NextFloat(12f, 20f), ProjectileID.Mushroom, Item.damage / 2, 0f, Owner.whoAmI);
            }
        }
    }

    /// <summary>
    /// Makes some garnular water ripples at the right place
    /// </summary>
    public void ProduceWaterRipples()
    {
        WaterShaderData water = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
        float power = 11f;
        float waveSine = 1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
        Vector2 size = Projectile.Size / 2f;
        Vector2 ripplePos = Rect().Center;
        Color waveData = new Color(power, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
        water.QueueRipple(ripplePos, waveData, size, RippleShape.Square, ToolDir.ToRotation());
    }

    /// <summary>
    /// Glue to the player
    /// </summary>
    public override bool ShouldUpdatePosition() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Item.ThisItemTexture();
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
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color col = lightColor * Projectile.Opacity;
        Color glow = Color.White * Projectile.Opacity;
        float scale = Projectile.scale;
        float rot = Projectile.rotation + RotationOffset;

        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        // no glowmask
        if (Item.Name.Contains("Solar") || Item.Name.Contains("Molten"))
            col = glow;

        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [col], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);
        Main.spriteBatch.Draw(tex, pos, frame, col, rot, origin, scale, Effects, 0f);

        if (Item.glowMask >= 0)
        {
            Asset<Texture2D> glowmask = TextureAssets.GlowMask[Item.glowMask];
            if (!glowmask.IsDisposed && glowmask.IsLoaded && glowmask != null)
            {
                after?.DrawFancySwordAfterimages(glowmask.Value, Projectile.Center, [glow], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);
                Main.spriteBatch.Draw(glowmask.Value, pos, frame, glow, rot, origin, scale, Effects, 0f);
            }
        }
        return false;
    }
}