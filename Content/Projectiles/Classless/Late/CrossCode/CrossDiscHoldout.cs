using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Classless;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.CrossUI;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class CrossDiscHoldout : BaseIdleHoldoutProjectile
{
    #region Defaults
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void Defaults()
    {
        Projectile.width = Projectile.height = 1;
    }
    public override string Texture => AssetRegistry.Invis;
    public override int IntendedProjectileType => ModContent.ProjectileType<CrossDiscHoldout>();
    public override int AssociatedItemID => ModContent.ItemType<CrossDisc>();

    public override void Load()
    {
        On_Main.DrawInterface_36_Cursor += ChangeCursor;
    }

    public override void Unload()
    {
        On_Main.DrawInterface_36_Cursor -= ChangeCursor;
    }

    private static void ChangeCursor(On_Main.orig_DrawInterface_36_Cursor orig)
    {
        if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<CrossDiscHoldout>(), Main.myPlayer))
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.CursorMelee);
            GlobalPlayer player = Main.LocalPlayer.Additions();
            if (player.MouseLeft.Current && Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<CrossSwing>()] <= 0)
                tex = AssetRegistry.GetTexture(AdditionsTexture.CursorRanged);

            Main.spriteBatch.Draw(tex, new Vector2(Main.mouseX + 1, Main.mouseY + 1), null, Color.White, 0f, new Vector2(.5f) * tex.Size(), Main.cursorScale * 1.1f, SpriteEffects.None, 0f);
        }
        else
            orig();
    }
    #endregion Defaults

    #region Definitions
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

    /// <summary>
    /// Describes cross disc elements
    /// </summary>
    [Flags]
    public enum Element
    {
        Neutral = 0,
        Cold = 1,
        Heat = 2,
        Shock = 3,
        Wave = 4,
    }

    /// <summary>
    /// The current elemental mode the cross disc
    /// </summary>
    public Element State
    {
        get => (Element)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }

    /// <summary>
    /// A small cooldown between melee swings
    /// </summary>
    public ref float SwingCooldown => ref Projectile.ai[1];

    /// <summary>
    /// The counter for the uncharged bolls
    /// </summary>
    public ref float ReticleCounter => ref Projectile.ai[2];

    /// <summary>
    /// The extra counter for a charged boll
    /// </summary>
    public ref float FullReticleCounter => ref Projectile.Additions().ExtraAI[0];

    /// <summary>
    /// How much balance this attack is going to use
    /// </summary>
    public ref float ElementalAmount => ref Projectile.Additions().ExtraAI[1];

    /// <summary>
    /// A small cooldown between shooting any boll
    /// </summary>
    public ref float BallCooldown => ref Projectile.Additions().ExtraAI[2];

    /// <summary>
    /// A small cooldown before the reticle disappears
    /// </summary>
    public ref float ReticleWait => ref Projectile.Additions().ExtraAI[3];

    /// <summary>
    /// 1 if melee, 0 if boll
    /// </summary>
    public float AttackState => Modded.mouseWorld.Distance(Owner.Center) < 200f ? 1f : 0f;

    public ElementalBalance ElementPlayer => Owner.GetModPlayer<ElementalBalance>();
    public bool HasOverload => Modded.CircuitOverload > 0;
    public const int BigBollCooldown = 20;
    public const int BollCooldown = 15;
    #endregion Definitions

    #region AI
    public BouncePrediction PredictionLine = new();
    public override void SafeAI()
    {
        if (Item.ModItem is not CrossDisc || Item.type != ModContent.ItemType<CrossDisc>() || Owner.dead || !Owner.active)
        {
            Projectile.Kill();
            return;
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld).SafeNormalize(Vector2.Zero) * 5f;
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }

        if (SwingCooldown > 0f)
            SwingCooldown--;

        #region Held
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.owner = Owner.whoAmI;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Projectile.Center = Center;
        #endregion Held

        ElementalBalanceUI.visible = true;

        Modded.crossIce = false;
        Modded.crossFire = false;
        Modded.crossWave = false;
        Modded.crossLightning = false;

        // For simplification in the ui
        ref int mode = ref Item.As<CrossDisc>().mode;

        mode = (int)Projectile.ai[0];

        #region Idle Effects

        // this adds in the overload on the circuit if used too much
        if (ElementPlayer.ElementCompletion >= 1f && State != Element.Neutral)
        {
            ElementalBalance.OverloadSound.Play(Owner.Center, 1.5f, -.2f);
            State = Element.Neutral;
            Modded.CircuitOverload = SecondsToFrames(30);
            this.Sync();
        }

        switch (State)
        {
            case Element.Neutral:
                Owner.statDefense += 10;
                Owner.statLifeMax2 += 50;
                Owner.lifeRegen += 1;
                Projectile.damage = 7200;
                ElementalAmount -= 2;

                if (Modded.GlobalTimer % 3 == 2)
                    ElementPlayer.ElementalResourceCurrent -= 1;
                break;
            case Element.Cold:
                Projectile.damage = 3600;
                ElementalAmount = 7;
                break;
            case Element.Heat:
                Owner.lifeRegenTime += 200;
                ElementalAmount = 7;
                break;
            case Element.Shock:
                Owner.moveSpeed *= 1.15f;
                Owner.runAcceleration += .05f;
                Owner.wingTimeMax += 25;
                Owner.statDefense -= 10;
                ElementalAmount = 3;
                break;
            case Element.Wave:
                Projectile.damage = 10000;
                Projectile.knockBack = 10f;
                Owner.lifeRegen += 3;
                ElementalAmount = 7;
                break;
        }
        #endregion Idle Effects

        #region Shoot Effects
        int type = ModContent.ProjectileType<CrossSwing>();
        if (Modded.SafeMouseRight.JustPressed && Owner.ownedProjectileCounts[type] <= 0 && this.RunLocal())
        {
            Vector2 velocity = Projectile.SafeDirectionTo(Modded.mouseWorld);

            // Make the swing
            Projectile swing = Main.projectile[Projectile.NewProj(Center, velocity, type,
                Projectile.damage, Projectile.knockBack, Projectile.owner)];
            swing.Additions().ExtraAI[6] = (float)BaseSwordSwing.SwingDirection.Up;
            swing.Additions().ExtraAI[7] = (float)State;
            swing.netUpdate = true;

            SwingCooldown = Item.useTime;
            this.Sync();
        }
        #endregion Shoot Effects

        // Handle Virtual Ricochet Projectile behaviors
        if (Owner.ownedProjectileCounts[type] <= 0)
            VRPBehavior();

        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, 0f);
    }

    public static readonly float MaxCharge = SecondsToFrames(.9f);
    public float ReticleProgress => InverseLerp(0f, MaxCharge, ReticleCounter);
    public float FullReticleProgress => InverseLerp(BigBollCooldown, BigBollCooldown * 2, FullReticleCounter);
    public float Spread => MathHelper.PiOver4 * (1f - ReticleProgress);
    private void VRPBehavior()
    {
        // Apply a decrease in accuracy the faster the cursor is spinning
        ReticleCounter = MathHelper.Clamp(ReticleCounter - (MathF.Abs(MathHelper.WrapAngle(Projectile.oldRot[0] - Projectile.oldRot[1])) * 5f), 0f, MaxCharge);

        if (Modded.MouseLeft.Current && this.RunLocal())
        {
            PredictionLine.Update(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero));
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
            Owner.moveSpeed *= .75f;

            if (ReticleCounter < MaxCharge)
                ReticleCounter++;
            if (ReticleProgress >= 1f)
            {
                FullReticleCounter++;
            }
            else
                FullReticleCounter = 0f;

            ReticleWait = 30f;
            this.Sync();
        }
        else if (!Modded.MouseLeft.Current && this.RunLocal())
        {
            if (ReticleWait > 0f)
            {
                ReticleCounter += .5f;
                ReticleWait--;
            }

            if (ReticleWait <= 0f)
                ReticleCounter = 0f;
            this.Sync();
        }

        if (BallCooldown > 0f)
            BallCooldown--;

        if (Modded.MouseLeft.JustReleased && this.RunLocal())
        {
            Vector2 pos = Center;
            Vector2 vel = Center.SafeDirectionTo(Modded.mouseWorld)
                .RotatedByRandom(Spread) * 5f;

            int type = ModContent.ProjectileType<VRP>();
            int damage = 10000;
            float kB = 0f;
            int own = Projectile.owner;
            float state = Projectile.ai[0];
            float progress = ReticleProgress;

            Projectile vrp = Main.projectile[Projectile.NewProj(pos, vel, type, damage, kB, own, state, progress)];
            vrp.Additions().ExtraAI[2] = (FullReticleProgress >= 1f).ToInt();

            AdditionsSound shoot = new();
            switch (State)
            {
                case Element.Neutral:
                    shoot = AdditionsSound.NeutralBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AdditionsSound.NeutralBallThrowCharged;
                    break;
                case Element.Cold:
                    shoot = AdditionsSound.ColdBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AdditionsSound.ColdBallThrowCharged;
                    break;
                case Element.Heat:
                    shoot = AdditionsSound.HeatBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AdditionsSound.HeatBallThrowCharged;
                    break;
                case Element.Shock:
                    shoot = AdditionsSound.ShockBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AdditionsSound.ShockBallThrowCharged;
                    break;
                case Element.Wave:
                    shoot = AdditionsSound.WaveBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AdditionsSound.WaveBallThrowCharged;
                    break;
            }
            shoot.Play(Projectile.Center, 1f, 0f, .2f, 20, Name);

            switch (State)
            {
                case Element.Neutral:
                    break;
                case Element.Cold:
                    ElementPlayer.ElementalResourceCurrent += 3;
                    break;
                case Element.Heat:
                    ElementPlayer.ElementalResourceCurrent += 4;
                    break;
                case Element.Shock:
                    ElementPlayer.ElementalResourceCurrent += 2;
                    break;
                case Element.Wave:
                    ElementPlayer.ElementalResourceCurrent += 3;
                    break;
            }

            FullReticleCounter = 0f;

            BallCooldown = BollCooldown;
            this.Sync();
        }
    }

    public override void OnKill(int timeLeft)
    {
        ElementalBalanceUI.visible = false;
    }

    #endregion AI

    #region Drawing
    public static readonly Texture2D normalReticle = AssetRegistry.GetTexture(AdditionsTexture.Reticle1);
    public static readonly Texture2D chargedReticle = AssetRegistry.GetTexture(AdditionsTexture.Reticle2);

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 screenPos = Main.screenPosition;
        if ((Modded.MouseLeft.Current || ReticleWait > 0f) && Owner.ownedProjectileCounts[ModContent.ProjectileType<CrossSwing>()] <= 0)
        {
            float opacity = ReticleWait < 29f ? .3f : 1f;
            int frame = (int)(FullReticleProgress * 3f);
            Rectangle dotFrame = normalReticle.Frame(1, 4, 0, frame);

            if (FullReticleProgress >= 1f)
            {
                for (int i = 1; i < PredictionLine.PathPoints.Count; i++)
                {
                    Vector2 pos = PredictionLine.PathPoints[i];
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f, dotFrame.Size() / 2, 1f, 0, 0f);
                }
            }
            else
            {
                for (float i = 0f; i < 500f; i += 100f)
                {
                    Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Spread) * i;
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f, dotFrame.Size() / 2, 1f, 0, 0f);
                    pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(-Spread) * i;
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f, dotFrame.Size() / 2, 1f, 0, 0f);
                }
            }

            Rectangle frame2 = chargedReticle.Frame(1, 4, 0, frame);
            Vector2 orig2 = frame2.Size() * .5f;
            Main.EntitySpriteDraw(chargedReticle, Modded.mouseWorld - screenPos, frame2, Color.White * opacity, 0f, orig2, 1f, 0);
        }

        return false;
    }
    #endregion Drawing
}

public struct BouncePrediction
{
    private const float MaxDistance = 2000f;
    private const float DotInterval = 100f;
    private const int MaxBounces = 4;
    private List<Vector2> pathPoints;

    public IReadOnlyList<Vector2> PathPoints => pathPoints.AsReadOnly();

    public BouncePrediction()
    {
        pathPoints = new List<Vector2>();
    }

    public void Update(Vector2 start, Vector2 direction)
    {
        pathPoints.Clear();

        if (direction == Vector2.Zero)
            return;

        direction = direction.SafeNormalize(Vector2.Zero); // Normalize safely
        Vector2 currentPosition = start;
        float remainingDistance = MaxDistance;
        pathPoints.Add(currentPosition);

        // World boundaries
        float WorldMinX = Main.leftWorld;
        float WorldMaxX = Main.rightWorld;
        float WorldMinY = Main.topWorld;
        float WorldMaxY = Main.bottomWorld;

        int bounceCount = 0;
        while (remainingDistance > 0 && bounceCount < MaxBounces)
        {
            Vector2 rayEnd = currentPosition + direction * remainingDistance;

            // Clamp to world boundaries
            if (rayEnd.X < WorldMinX || rayEnd.X > WorldMaxX || rayEnd.Y < WorldMinY || rayEnd.Y > WorldMaxY)
            {
                float tX = direction.X == 0 ? float.MaxValue : (direction.X > 0 ? (WorldMaxX - currentPosition.X) / direction.X : (WorldMinX - currentPosition.X) / direction.X);
                float tY = direction.Y == 0 ? float.MaxValue : (direction.Y > 0 ? (WorldMaxY - currentPosition.Y) / direction.Y : (WorldMinY - currentPosition.Y) / direction.Y);
                float t = Math.Min(tX, tY);
                if (t <= 0 || float.IsInfinity(t) || float.IsNaN(t))
                {
                    break;
                }
                rayEnd = currentPosition + direction * t;
            }

            Vector2? collision = RaytraceTiles(currentPosition, rayEnd);

            if (!collision.HasValue || (collision.Value.X == 0 && collision.Value.Y == 0))
            {
                // No collision: Add points along the ray up to rayEnd
                AddDotPoints(currentPosition, currentPosition + direction * MaxDistance);
                pathPoints.Add(rayEnd);
                break; // Exit after adding points for the full ray
            }

            Vector2 collisionPoint = collision.Value;
            if (!WorldGen.InWorld((int)(collisionPoint.X / 16f), (int)(collisionPoint.Y / 16f), 0))
            {
                AddDotPoints(currentPosition, collisionPoint);
                pathPoints.Add(collisionPoint);
                break;
            }

            AddDotPoints(currentPosition, collisionPoint);
            pathPoints.Add(collisionPoint);

            Vector2 normal = GetFallbackNormal(collisionPoint, direction);

            Vector2 oldDirection = direction;
            direction = Vector2.Reflect(direction, normal);

            currentPosition = collisionPoint + direction * 0.1f; // Nudge to avoid re-collision
            remainingDistance -= Vector2.Distance(start, collisionPoint);
            bounceCount++;
        }
    }

    private void AddDotPoints(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        if (float.IsNaN(distance))
            return;

        int dotCount = (int)(distance / DotInterval);
        if (dotCount == 0)
            return;

        for (int i = 1; i <= dotCount; i++)
        {
            float t = i * DotInterval / distance;
            Vector2 dotPoint = Vector2.Lerp(from, to, t);
            if (!pathPoints.Contains(dotPoint))
            {
                pathPoints.Add(dotPoint);
            }
        }
    }

    public static Vector2 GetFallbackNormal(Vector2 collisionPoint, Vector2 incomingDirection)
    {
        int x = (int)(collisionPoint.X / 16f);
        int y = (int)(collisionPoint.Y / 16f);
        float tileLeft = x * 16f;
        float tileRight = (x + 1) * 16f;
        float tileTop = y * 16f;
        float tileBottom = (y + 1) * 16f;

        Point collisionTile = collisionPoint.ToTileCoordinates();
        Vector2 center = collisionTile.ToWorldCoordinates();
        Tile tile = Main.tile[collisionTile];
        float sideView = MathHelper.PiOver2;
        if (tile.IsHalfBlock)
        {
            sideView = MathHelper.PiOver4;
            center.Y += 4f;
        }
        //center.SuperQuickDust(Color.Purple, 5);
        //collisionPoint.SuperQuickDust(Color.Yellow, 41);

        if (tile.Slope == SlopeType.Solid)
        {
            if (center.IsInFieldOfView(0f, collisionPoint, sideView, 100f)) // Right
                return Vector2.UnitX;
            else if (center.IsInFieldOfView(MathHelper.PiOver2, collisionPoint, MathHelper.PiOver2, 100f)) // Down
                return Vector2.UnitY;
            else if (center.IsInFieldOfView(MathHelper.Pi, collisionPoint, sideView, 100f)) // Left
                return -Vector2.UnitX;
            else if (center.IsInFieldOfView(-MathHelper.PiOver2, collisionPoint, MathHelper.PiOver2, 100f)) // Up
                return -Vector2.UnitY;
        }
        else
        {
            float diagonal = MathF.Sqrt(2f) / 2f;

            if (tile.Slope == SlopeType.SlopeDownLeft)
                return new Vector2(-diagonal, diagonal);
            if (tile.Slope == SlopeType.SlopeDownRight)
                return new Vector2(diagonal, diagonal);
            if (tile.Slope == SlopeType.SlopeUpLeft)
                return new Vector2(-diagonal, -diagonal);
            if (tile.Slope == SlopeType.SlopeUpRight)
                return new Vector2(diagonal, -diagonal);
        }

        return Vector2.UnitY; // Default to upward if no clear side
    }
}