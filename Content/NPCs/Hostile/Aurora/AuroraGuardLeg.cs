using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

// Largely designed off of the legs of Crabulon in Calamity Fables
public class AuroraGuardLeg : Entity
{
    public float Maxlength;
    public bool LatchedOn = false;

    public AuroraGuardLeg PairedLeg;
    public AuroraGuardLeg SisterLeg;

    public bool FrontPair;
    public bool LeftSet;

    public Vector2 LegOrigin;
    public Vector2 LegKnee;
    public Vector2 LegTip;

    public Vector2 LegTipGraphic;
    public Vector2 LegOriginGraphic;

    public float GrabDelay = 0;
    public float StepTimer = 0;
    public float StrideTimer = 0;
    public float FallTime = 0f;

    /// <summary>
    /// The absolutely ideal grab position of the leg
    /// </summary>
    public Vector2 DesiredGrabPosition;

    /// <summary>
    /// The best grab position we found
    /// </summary>
    public Vector2? GrabPosition;

    /// <summary>
    /// The previously best grab position we found
    /// </summary>
    public Vector2? PreviousGrabPosition;

    /// <summary>
    /// The best grab tile we found
    /// </summary>
    public Point? GrabTile
    {
        get
        {
            if (GrabPosition != null)
                return GrabPosition.Value.ToTileCoordinates();
            return null;
        }
    }

    public float Foreleglength => 40.5f;
    public float Leglength => 96f;

    public AuroraGuard turret;
    public float BaseRotation;

    public bool PlayedStepEffects = true; // If it needs to play its stepping sound
    public float StepEffectForce = 1f; // Volume of the stepping sound when played. Amps up the longer the foot is left in the air
    public int Direction => LeftSet ? -1 : 1;
    public float SisterInfluence => SisterLeg.LatchedOn ? SisterLeg.StepTimer : 1;
    public NPC NPC => turret.NPC;

    public AuroraGuardLeg(AuroraGuard turret, bool frontPair, bool leftSet, float baseRotation)
    {
        this.turret = turret;
        this.FrontPair = frontPair;
        this.LeftSet = leftSet;
        this.BaseRotation = baseRotation;

        LegOrigin = GetLegOrigin();
        LegKnee = LegOrigin + Vector2.UnitY * Foreleglength;
        LegTip = LegKnee + Vector2.UnitY * Leglength;
        LegTipGraphic = LegTip;
        LegOriginGraphic = LegOrigin + Vector2.UnitY * turret.VerticalVisualOffset;

        ForelimbAsset = AssetRegistry.GetTexture(AdditionsTexture.AuroraLimbStart);
        LimbAsset = AssetRegistry.GetTexture(AdditionsTexture.AuroraLimbEnd);

        forelegSpriteOrigin = new Vector2(6, 6);

        if (leftSet)
            forelegSpriteOrigin.Y = ForelimbAsset.Height - forelegSpriteOrigin.Y;

        legSpriteOrigin = new Vector2(8, 24);

        if (leftSet)
            legSpriteOrigin.Y = LimbAsset.Height - legSpriteOrigin.Y;
    }

    public void Update()
    {
        NPC owner = turret.NPC;
        Maxlength = Leglength + Foreleglength;
        Vector2 legDirection = (BaseRotation + owner.rotation).ToRotationVector2();

        LegOrigin = GetLegOrigin();
        LegOriginGraphic = LegOrigin + Vector2.UnitY * turret.VerticalVisualOffset;

        // Check if the leg is latched onto something based on if its close enough to the grab position
        LatchedOn = false;
        if (GrabPosition != null && Vector2.Distance(LegTip, GrabPosition.Value) < 10f)
        {
            LegTip = GrabPosition.Value;
            LatchedOn = true;
        }

        UpdateDesiredGrabPosition(legDirection);
        bool frontSet = Math.Sign(turret.NPC.velocity.X) == Direction;

        width = height = (int)Maxlength;
        position = LegOrigin;
        Center = LegKnee;

        int dir = turret.NPC.velocity.X.NonZeroSign();
        if (LeftSet)
            direction = -1;
        else
            direction = 1;

        // When grappled
        if (LatchedOn)
        {
            // Check if the leg is "uncomfortable" enough and release if it is
            if (ShouldReleaseLeg(frontSet, out bool noStepDelay))
            {
                ReleaseGrip();
                if (noStepDelay)
                    GrabDelay = 0;
            }

            // Step effects
            if (!PlayedStepEffects)
            {
                if (StepEffectForce > 0.4f)
                {
                    // Step sound volume scales with how long the leg has been out in the air
                    float stepPitch = InverseLerp(300f, 1000f, LegTip.Distance(Main.LocalPlayer.Center), true) * .3f;
                    float stepVolume = InverseLerp(0.5f, 1f, StepEffectForce);
                    AdditionsSound.LegStomp.Play(LegTip, stepVolume * .36f, stepPitch, .1f, 20);
                    Collision.HitTiles(LegTip, LegTip.SafeDirectionTo(turret.VisualCenter), 15, 15);

                    // Screenshake if big enough
                    ScreenShakeSystem.New(new(stepVolume * .03f, .3f), LegTip);
                }
                PlayedStepEffects = true;
            }

            StepEffectForce = 0f;
            FallTime = 0f;

            // Tick down the step timer (controls the small ground stab motion when it finishes a new step)
            StepTimer -= 1 / (60f * 0.3f);
            if (StepTimer < 0)
                StepTimer = 0;
        }

        // When free
        else
        {
            // Check for a new position to latch on if we dont have one
            if (GrabPosition == null)
                FindGrabPos();

            // If we still dont have a valid grab position
            if (GrabPosition == null)
            {
                // Fall
                if (owner.velocity.Y > 2)
                {
                    FallTime++;

                    // When falling, flail legs around a point
                    Vector2 fallingPosition = DesiredGrabPosition - Vector2.UnitY * 100f;
                    Vector2 fallPositionOffset = new((float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f) * 40f, 21f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 30f) * 70f);
                    fallingPosition += fallPositionOffset;

                    // Back set of legs is a bit more retracted towards the center
                    if (!FrontPair)
                        fallingPosition.X -= Direction * 30f;

                    // Undo the lateral displacement of the desired grab position, the legs should be spread evenly
                    fallingPosition.X -= DesiredGrabPositionVelocityXOffset;

                    // Move towards the falling leg position
                    LegTip = Vector2.SmoothStep(LegTip, fallingPosition, Animators.MakePoly(3f).OutFunction(Min(1f, FallTime / 8f) * 0.1f));

                    // Entirely lose your previous grab position if falling for too long
                    if (FallTime > 10f)
                        PreviousGrabPosition = null;
                }

                // Leg limps down
                else
                {
                    LegTip.Y += 4.2f;
                    if (SolidCollisionFix(LegTip, 2, 2, true))
                        LegTip.Y -= 4.2f;
                }
            }

            // Otherwise
            else
            {
                // If we have a previous position to step from, do a nicely eased step
                if (PreviousGrabPosition.HasValue)
                {
                    float amount = MakePoly(2.7f).InOutFunction(1 - StrideTimer);
                    LegTip = Vector2.SmoothStep(PreviousGrabPosition.Value, GrabPosition.Value, amount);

                    // Upwards bump motion
                    LegTip.Y -= 12.5f * (float)Math.Sin(StrideTimer * Pi);
                }

                // If this is the first step after spawning, or after falling, do a slightly less clean motion towards the Target
                else
                {
                    // Move faster towards the tip if the leg has been falling for a while
                    float moveSpeed = (10f + InverseLerp(20f, 40f, FallTime) * 15f);
                    LegTip = LegTip.MoveTowards(GrabPosition.Value, moveSpeed);
                    LegTip.Y -= 4.5f * InverseLerp(0f, 50f, Math.Abs(LegTip.X - GrabPosition.Value.X));
                }

                // Time to move between the last grab position and the new one. Increases with Turrets speed
                float stepTime = 0.32f - 0.12f * InverseLerp(4f, 8f, Math.Abs(NPC.velocity.X));
                StrideTimer -= 1 / (60f * stepTime);
                if (StrideTimer < 0)
                    StrideTimer = 0;

                // If we somehow moved away from the grab position so far that the leg cant even reach it, stop trying to grip it and find a new one next frame
                if (LegOrigin.Distance(GrabPosition.Value) > Maxlength)
                {
                    ReleaseGrip();
                }
            }

            // Reset visual variables and charge up the force of the step effects
            StepEffectForce = Math.Min(1f, StepEffectForce + 0.125f);
            PlayedStepEffects = false;
            StepTimer = 1f;
        }

        LegTipGraphic = LegTip;

        // Leg tip "pierces" the ground a bit when stepping
        LegTipGraphic += Vector2.UnitY * 7f;
        if (StepTimer < 1)
            LegTipGraphic.Y += 10f * MakePoly(2.4f).InFunction(StepTimer);

        LegKnee = CalculateJointPosition(LegOriginGraphic, LegTipGraphic, Foreleglength, Leglength, !LeftSet);

        if (LegTipGraphic.Distance(LegOriginGraphic) > Maxlength)
            LegTipGraphic = LegOriginGraphic + LegOriginGraphic.DirectionTo(LegTipGraphic) * Maxlength;
    }

    public float DesiredGrabPositionVelocityXOffset => (Math.Abs(NPC.velocity.X) > 2f && (NPC.velocity.X * Direction < 0)) ?
        NPC.velocity.X.NonZeroSign() * 150f : 0f;

    public void UpdateDesiredGrabPosition(Vector2 legDirection)
    {
        DesiredGrabPosition = NPC.Center + (legDirection * 1.25f + Vector2.UnitY).SafeNormalize(Vector2.UnitY) * Maxlength * 0.9f;

        // Offset grab positions sideways
        DesiredGrabPosition += Vector2.UnitX * 90f * Direction;

        // Offset the grab positions latterally by the NPCs velocity if on the set of legs trailing behind
        DesiredGrabPosition.X += DesiredGrabPositionVelocityXOffset;

        // Clamp the distance
        if (DesiredGrabPosition.Distance(LegOrigin) >= Maxlength)
            DesiredGrabPosition = LegOrigin + LegOrigin.DirectionTo(DesiredGrabPosition) * Maxlength;
    }

    public Vector2 GetLegOrigin()
    {
        float x = false ? (FrontPair ? 46f : 32f) * (LeftSet ? -1 : 1) : (FrontPair ? 70f : 46f) * (LeftSet ? -1 : 1);
        float y = false ? (FrontPair ? 7f : 20f) : (FrontPair ? 5f : -11f);
        Vector2 offset = new Vector2(x / 2, y);

        return turret.NPC.Center + offset;
    }

    #region Check if leg should release
    public bool ShouldReleaseLeg(bool frontSet, out bool noDelay)
    {
        noDelay = false;

        float maxExtensionTreshold = 1f - SisterInfluence * 0.15f;

        // If the legs are the ones being walked away from, the max length treshold is also shortened even more
        if (!frontSet)
            maxExtensionTreshold -= (1 - SisterInfluence) * 0.2f;

        // Keep the treshold full if walking slowly enough
        if (Math.Abs(NPC.velocity.X) < 1.4f)
            maxExtensionTreshold = 1f;

        float minExtensionTreshold = 0.26f - SisterInfluence * 0.16f;

        float tooFarUnderTreshold = (0.25f + SisterInfluence * 0.75f) * 40f;
        float maxHeightTreshold = 30f;

        float extension = LegTip.Distance(LegOrigin);

        if (LegTip.Distance(GrabPosition.Value) > Maxlength)
            return true;

        // Ungrip when extended too far out
        if (extension > Maxlength * maxExtensionTreshold)
            return true;

        // Ungrip when the leg is too compressed
        else if (extension < Maxlength * minExtensionTreshold)
        {
            noDelay = true;
            return true;
        }

        // Ungrip when the leg is too far behind and should take a new step forward
        // Either immediately if part of the front set of legs, or if the step timer is over (to avoid back legs rapid fire)
        else if ((LegOrigin.X - LegTip.X) * Direction > tooFarUnderTreshold && (frontSet || StepTimer <= 0))
        {
            noDelay = true;
            return true;
        }

        // Ungrip when the leg is too far above the turret and too close to the turret
        else if (LegOrigin.Y - LegTip.Y > maxHeightTreshold && (LegTip.X - LegOrigin.X) * Direction < Maxlength * 0.2f)
            return true;

        return false;
    }

    public void ReleaseGrip()
    {
        if (PairedLeg.GrabDelay < 1 && GrabDelay < 1)
            GrabDelay = 4;

        StrideTimer = 1f;
        PreviousGrabPosition = GrabPosition ?? LegTip;
        GrabPosition = null;
        LatchedOn = false;
    }
    #endregion

    #region Grab Position Scanning
    private void FindGrabPos(bool debugView = false)
    {
        // Dont grab if in delay period
        if (GrabDelay > 0)
        {
            GrabDelay--;
            return;
        }

        bool frontSet = Math.Sign(turret.NPC.velocity.X) == Direction;

        // The position tracing from the shoulder to the desired grab position
        Vector2 shoulder = LegOrigin;
        Vector2 grip = DesiredGrabPosition;
        if (frontSet)
        {
            shoulder.X += turret.NPC.velocity.X * 40f;
            grip.X += turret.NPC.velocity.X * 10f;
            grip.Y -= 20f;

            // Clamp distances
            if (grip.Distance(LegOrigin) > Maxlength)
                grip = LegOrigin + LegOrigin.DirectionTo(grip) * Maxlength;

            if (shoulder.Distance(LegOrigin) > Maxlength)
                shoulder = LegOrigin + Vector2.UnitX * Direction * Maxlength;

            if (debugView)
            {
                shoulder.SuperQuickDust(Color.Red);
                grip.SuperQuickDust(Color.Yellow);
                AdditionsDebug.DebugLine(shoulder, grip, Color.Blue);
            }
        }
        Vector2? trace = RaytraceTiles(shoulder, grip, true);
        Point? fromShoulderGuess = trace.HasValue ? trace.Value.ToTileCoordinates() : null;
        Point? bestGuess = null;
        bool tooClose = false;

        // We dont really want to grab a tile thats too close
        if (fromShoulderGuess != null)
        {
            if (TileToGripPoint(fromShoulderGuess.Value).Distance(LegOrigin) < Maxlength * 0.45f)
                tooClose = true;
            else
                bestGuess = fromShoulderGuess;
        }

        if (bestGuess == null)
        {
            // Look down to find a potential grab position
            if (!tooClose)
                bestGuess = RadialDownGrabPosScan(12, 1.2f, ref tooClose, debugView);

            // Look around to find a grab position, without any raycasting
            if (tooClose)
            {
                float radius = Maxlength * (FrontPair ? 0.8f : 0.6f);
                float startAngle = frontSet ? PiOver4 : PiOver2 * 0.8f;
                bestGuess = RadialGrabPosScan(startAngle, Pi * 0.95f, radius, debugView);
            }
        }

        // If we couldnt find anything better with the radial check, just go with the straight raycast as a fallback
        if (bestGuess == null && fromShoulderGuess.HasValue)
            bestGuess = fromShoulderGuess;

        if (bestGuess != null)
            ConfirmGrabPosition(bestGuess.Value);
    }

    /// <summary>
    /// Tries to look downwards for solid ground by raycasting from the shoulder to the desired grab position, rotated more and more towards the floor
    /// </summary>
    /// <param name="iterations">How many raycasts should happen</param>
    /// <param name="angle">How far down should the check be</param>
    /// <param name="tooClose"></param>
    /// <returns></returns>
    public Point? RadialDownGrabPosScan(int iterations, float angle, ref bool tooClose, bool debugView = false)
    {
        int i = 0;
        Point? bestGuess = null;
        Vector2 toGrabPosition = LegOrigin.DirectionTo(DesiredGrabPosition);

        while (i < iterations && bestGuess == null)
        {
            // Try tilting the grab position downwards until we find ground
            Vector2 tiltedGrabPosition = LegOrigin + toGrabPosition.RotatedBy(i * Direction / (float)iterations * angle) * Maxlength * 0.95f;

            if (debugView)
                tiltedGrabPosition.SuperQuickDust(Color.Green);

            Vector2? trace = RaytraceTiles(LegOrigin, tiltedGrabPosition, true);
            bestGuess = trace.HasValue ? trace.Value.ToTileCoordinates() : null;

            // Cant grab if the resulting grip location would be too close
            if (bestGuess.HasValue && TileToGripPoint(bestGuess.Value).Distance(LegOrigin) < Maxlength * 0.45f)
            {
                bestGuess = null;
                tooClose = true;
            }
            else
            {
                if (debugView)
                    tiltedGrabPosition.SuperQuickDust(Color.White);

                tooClose = false;
            }
            i++;
        }

        return bestGuess;
    }

    /// <summary>
    /// Tries to look in a radius to the side of the leg for any solid ground tile. Prioritizes gripping on tiles that are exposed to air, but cant grab inside the ground
    /// Prefers having a grab spot thats close to straight to the side
    /// </summary>
    /// <param name="angleStart"></param>
    /// <param name="angleEnd"></param>
    /// <param name="searchRadius"></param>
    /// <returns></returns>
    public Point? RadialGrabPosScan(float angleStart, float angleEnd, float searchRadius, bool debugView = false)
    {
        Vector2 origin = LegOrigin;

        // If the turret is moving
        if (Math.Abs(turret.NPC.velocity.X) > 2f)
        {
            // Move the check for the pair of legs that is being dragged a bit ahead
            if (turret.NPC.velocity.X * Direction < 0)
                origin.X += turret.NPC.velocity.X.NonZeroSign() * 90f;

            // Make the radius for the pair of legs that is moving forward a bit bigger, but not bigger than the max leg length
            else
                searchRadius = Math.Min(Maxlength, searchRadius * 1.2f);
        }

        float totalAngle = angleEnd - angleStart;
        bool lastInAir = false;
        float progress = 0f;
        float halfTileAngle = 8f / searchRadius;
        float step = halfTileAngle / totalAngle;
        List<Point> potentialGrabPoints = [];
        List<Point> insideTilesPositions = [];

        while (progress <= 1f)
        {
            float angle = (angleStart + progress * totalAngle) * Direction;
            Vector2 tiltedGrabPosition = origin + (-Vector2.UnitY).RotatedBy(angle) * searchRadius;
            Point candidate = tiltedGrabPosition.ToTileCoordinates();
            Tile t = Main.tile[candidate];

            if (t.HasUnactuatedTile && Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0))
            {
                // If we find a solid tile and we were previously in the air, thats a potential new step candidate
                if (lastInAir)
                {
                    potentialGrabPoints.Add(candidate);

                    if (debugView)
                        candidate.SuperQuickDust(Color.Red);
                }
                else
                    insideTilesPositions.Add(candidate);
                lastInAir = false;
            }

            if (debugView)
                candidate.SuperQuickDust(Color.Blue);

            if (!t.HasUnactuatedTile || (!Main.tileSolid[t.TileType] && !TileID.Sets.Platforms[t.TileType]))
                lastInAir = true;

            progress += step;
        }

        if (potentialGrabPoints.Count > 0)
            return potentialGrabPoints.OrderBy(RadialPosScanRating).Last();
        else if (insideTilesPositions.Count > 0)
            return insideTilesPositions.OrderBy(RadialPosScanRating).Last();

        return null;
    }

    public float RadialPosScanRating(Point p)
    {
        Vector2 worldPos = TileToGripPoint(p);
        float length = worldPos.Distance(LegOrigin);

        // Check the angle from the left so its easier to get
        Vector2 angleStart = worldPos;
        if (angleStart.X < LegOrigin.X)
            angleStart.X += (LegOrigin.X - angleStart.X) * 2;
        float angle = LegOrigin.AngleTo(angleStart);

        Vector2 idealAngleStart = DesiredGrabPosition;
        if (idealAngleStart.X < LegOrigin.X)
            idealAngleStart.X += (LegOrigin.X - idealAngleStart.X) * 2;
        float idealAngle = LegOrigin.AngleTo(idealAngleStart);

        float idealGrabHeightBias = 0.2f + 0.8f * InverseLerp(100f, 10f, Math.Abs(turret.FloorPosition.Y - worldPos.Y));

        // Platforms with a close enough Y position are penalized to prevent from grabbing onto platforms that are going through itself
        float closePlatformScoreReduction = 0f;
        if (Main.tileSolidTop[Main.tile[p].TileType])
            closePlatformScoreReduction += InverseLerp(16f, 80f, LegOrigin.Y - worldPos.Y);

        return (1 - Math.Abs(angle - idealAngle) / PiOver2) * InverseLerp(0, Maxlength * 0.85f, length) * idealGrabHeightBias - closePlatformScoreReduction;
    }

    private void ConfirmGrabPosition(Point potentialGrabPosition)
    {
        if (GrabPosition == null || RateGripPoint(potentialGrabPosition) > RateGripPoint(GrabTile.Value))
        {
            Vector2 attachPoint = TileToGripPoint(potentialGrabPosition);

            // Grab destination is the closest point on the tile
            if (GrabTile != null && GrabTile.Value == attachPoint.ToTileCoordinates())
                return;

            GrabPosition = attachPoint;
        }
    }

    public Vector2 TileToGripPoint(Point tilePosition)
    {
        Tile t = Main.tile[tilePosition];
        Vector2 tileWorldCoordinates = tilePosition.ToWorldCoordinates();
        Rectangle aroundTile = RectangleFromVectors(tileWorldCoordinates - Vector2.One * 9f, tileWorldCoordinates + Vector2.One * 9f);
        if (t.IsHalfBlock || t.Slope != SlopeType.Solid)
        {
            aroundTile.Y += 8;
            aroundTile.Height -= 8;
        }

        return LegOrigin.ClampInRect(aroundTile);
    }

    public float RateGripPoint(Point gripPoint)
    {
        return 100f / Vector2.Distance(gripPoint.ToWorldCoordinates(), DesiredGrabPosition);
    }

    public float ReleaseScore()
    {
        float releaseScore;
        if (GrabPosition == null)
            releaseScore = Vector2.Distance(LegTip, DesiredGrabPosition);
        else
            releaseScore = Vector2.Distance(GrabPosition.Value, DesiredGrabPosition) * 2f;

        if (LatchedOn)
            releaseScore *= 2f;

        // We really dont want to release if the other leg in the pair isnt latched on
        if (!PairedLeg.LatchedOn)
        {
            releaseScore /= 100f;
        }

        int direction = LeftSet ? -1 : 1;
        if ((LegTip.X - turret.NPC.Center.X).NonZeroSign() != direction)
            releaseScore *= 100f;

        return releaseScore;
    }
    #endregion

    #region Drawing
    internal readonly Texture2D ForelimbAsset;
    internal readonly Texture2D LimbAsset;

    public readonly Vector2 forelegSpriteOrigin;
    public readonly Vector2 legSpriteOrigin;

    public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        SpriteEffects flip = LeftSet ? SpriteEffects.FlipVertically : SpriteEffects.None;
        spriteBatch.Draw(ForelimbAsset, LegOriginGraphic - screenPos, null, drawColor, LegOriginGraphic.AngleTo(LegKnee), forelegSpriteOrigin, turret.NPC.scale, flip, 0);
        spriteBatch.Draw(LimbAsset, LegKnee - screenPos, null, drawColor, LegKnee.AngleTo(LegTipGraphic), legSpriteOrigin, turret.NPC.scale, flip, 0);
    }
    #endregion
}