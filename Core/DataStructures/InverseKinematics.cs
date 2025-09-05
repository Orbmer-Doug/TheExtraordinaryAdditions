using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Core.DataStructures;

// A slightly modified version of the IK present in Calamity Fables
// love those guys

// TODO: Offsets to joint position without blowing up? Reliable angle limitations?
public class CCDKinematicJoint
{
    public CCDKinematicJoint Parent { get; set; }
    public CCDKinematicJoint Child { get; set; }

    public CCDKinematicsConstraint Constraints = null;

    #region Variables
    internal bool _needsRotationRecalculation = true;
    internal bool _needsPositionRecalculation = true;
    internal Vector2 _position;
    internal float _cachedAbsoluteRotation;
    internal float _rotation;
    internal float _jointLength;
    internal CCDKinematicJoint _endEffector;

    public float Rotation
    {
        get
        {
            // The first joint (which is just a hinge) doesn't have a rotation
            if (Parent == null)
                return 0;

            if (!_needsRotationRecalculation)
                return _cachedAbsoluteRotation;

            _cachedAbsoluteRotation = Parent.Rotation + _rotation;
            _needsRotationRecalculation = false;
            return _cachedAbsoluteRotation;
        }
        set
        {
            // First joint is just a hinge, you can't change its rotation
            if (Parent == null)
            {
                // If the first hinge has a child, the rotation change will instead affect the first limb
                if (Child != null)
                    Child.Rotation = value;
                return;
            }

            _needsPositionRecalculation = true;
            _needsRotationRecalculation = true;
            RecursivelyClearChildRotationCaches();

            // If unconstrained, just directly set the rotation.
            if (Constraints == null)
                _rotation = value - Parent.Rotation;

            // Else, we have to do a bit more trickery.
            else
            {
                float newRotation = value - Parent.Rotation;
                float newUnwrappedRotation = _rotation + _rotation.AngleBetween(newRotation);
                _rotation = Constraints.Apply(newUnwrappedRotation);
            }
        }
    }

    public float JointLength
    {
        get => _jointLength;
        set
        {
            // Changing the joint length means that we need to recalculate the cached position of all children
            RecursivelyClearChildPositionCaches();
            _jointLength = value;
        }
    }

    public Vector2 Position
    {
        get
        {
            // If this is the first part of the limb, just give the direct position
            if (Parent == null)
                return _position;

            // If we're on part of the limb but we don't need to recalculate it, just give the cached position
            if (!_needsPositionRecalculation)
                return _position;

            // Else, calculate the position from the parent's position, and cache it
            _position = Parent.Position + Rotation.ToRotationVector2() * JointLength;
            _needsPositionRecalculation = false;
            return _position;
        }

        set
        {
            // Can only set the position directly if it's the first part of the limb
            if (Parent == null)
            {
                _position = value;
                RecursivelyClearChildPositionCaches(); // Reset the cached positions of all the child segments since they will have been moved by moving the first segment
            }
        }
    }

    public CCDKinematicJoint EndEffector
    {
        get
        {
            // If no child, this is the end effector
            if (Child == null)
                return this;

            // If we cached an end effector, return it
            if (_endEffector != null)
                return _endEffector;

            _endEffector = GetEndEffector();
            return _endEffector;
        }
    }

    public void RecursivelyClearChildPositionCaches()
    {
        if (Child != null)
        {
            Child._needsPositionRecalculation = true;
            Child.RecursivelyClearChildPositionCaches();
        }
    }

    public void RecursivelyClearChildRotationCaches()
    {
        // Changing the rotation means not only changing the rotations of every other child, but also changing their position
        if (Child != null)
        {
            Child._needsPositionRecalculation = true;
            Child._needsRotationRecalculation = true;
            Child.RecursivelyClearChildRotationCaches();
        }
    }

    internal void RecursivelyClearParentsEndEffectors()
    {
        _endEffector = null;
        Parent?.RecursivelyClearParentsEndEffectors();
    }

    public Vector2 SegmentVector => Rotation.ToRotationVector2() * JointLength;
    #endregion

    #region Constructors
    public CCDKinematicJoint(Vector2 position)
    {
        Position = position;
        _jointLength = 0;
        _rotation = 0;
        _cachedAbsoluteRotation = 0;
    }

    public CCDKinematicJoint(Vector2 position, CCDKinematicJoint parent)
    {
        parent.Append(this);

        JointLength = position.Distance(parent.Position);

        float rotation = (position - parent.Position).ToRotation();
        Rotation = rotation;
    }
    #endregion

    #region Adding children
    /// <summary>
    /// Appends a new joint to the end of the limb
    /// </summary>
    public void Append(CCDKinematicJoint newJoint)
    {
        newJoint.Parent = this;
        Child = newJoint;
        Child.RecursivelyClearChildRotationCaches();
        Child.RecursivelyClearParentsEndEffectors();
    }

    /// <summary>
    /// Creates and appends a new joint to the end of the limb at the specified joint position, with the specified constraints
    /// </summary>
    public void Append(Vector2 newJointPosition, CCDKinematicsConstraint constraints = null)
    {
        CCDKinematicJoint newJoint = new(newJointPosition, this)
        {
            Constraints = constraints
        };
        Append(newJoint);
    }

    /// <summary>
    /// Adds a new joint to the final segment from the segment chain this joint is part of
    /// </summary>
    public void Extend(CCDKinematicJoint newJoint)
    {
        if (Child != null)
            Child.Extend(newJoint);
        else
            Append(newJoint);
    }

    /// <summary>
    /// Adds a newly created joint to the final segment from the segment chain this joint is part of with the specified position and constraints
    /// </summary>
    public void Extend(Vector2 newJointPosition, CCDKinematicsConstraint constraints = null)
    {
        if (Child != null)
            Child.Extend(newJointPosition, constraints);
        else
            Append(newJointPosition, constraints);
    }

    /// <summary>
    /// Adds a newly created joint to the final segment from the segment chain this joint is part of, with the specified offset from the end of the chain
    /// </summary>
    public void ExtendInDirection(Vector2 newJointOffset, CCDKinematicsConstraint constraints = null)
    {
        if (Child != null)
            Child.ExtendInDirection(newJointOffset, constraints);
        else
            Append(Position + newJointOffset, constraints);
    }
    #endregion

    public List<CCDKinematicJoint> GetSubLimb()
    {
        List<CCDKinematicJoint> joints = [];
        RecursivelyFillSubLimbList(ref joints);
        return joints;
    }

    internal void RecursivelyFillSubLimbList(ref List<CCDKinematicJoint> joints)
    {
        joints.Add(this);
        Child?.RecursivelyFillSubLimbList(ref joints);
    }

    public float GetLimblength()
    {
        float currentlength = 0f;
        RecursivelyAddLimblength(ref currentlength);
        return currentlength;
    }

    internal void RecursivelyAddLimblength(ref float length)
    {
        length += JointLength;
        Child?.RecursivelyAddLimblength(ref length);
    }

    public float GetDistanceToEndEffector() => EndEffector.Position.Distance(Position);

    public CCDKinematicJoint GetEndEffector()
    {
        if (Child != null)
            return Child.GetEndEffector();
        return this;
    }

    public override string ToString()
    {
        return $"Rotation={Rotation}, Joint Length={JointLength}, Position={Position}, Segment Vector={SegmentVector}, End Effector={EndEffector}";
    }
}

/// <summary>
/// Allows for easy constraints on limbs
/// </summary>
/// <param name="minimumAngle">Maximum angle the limb may go</param>
/// <param name="maximumAngle">Minimum angle the limb may go</param>
/// <param name="delta">The maximum overshoot this limb can go</param>
/// <param name="stiffness">How sharp the limb moves beyond its limits</param>
/// <param name="flip">Flip the constraint angles?</param>
public class CCDKinematicsConstraint(float minimumAngle, float maximumAngle, float delta = 0.1f, float stiffness = 2f, bool flip = false)
{
    public float Delta { get; set; } = delta;
    public float Stiffness { get; set; } = stiffness;
    public bool FlipConstraintAngles { get; set; } = flip;
    public float MinimumAngle
    {
        get => FlipConstraintAngles ? -maximumAngle : minimumAngle;
        set => minimumAngle = value;
    }
    public float MaximumAngle
    {
        get => FlipConstraintAngles ? -minimumAngle : maximumAngle;
        set => maximumAngle = value;
    }

    public float Apply(float angle)
    {
        // Using a hyperbolic tangent not only allow for natural movement (adding leeway to min and max angles)
        // But it also enables the system to handle unreachable targets by gracefully exceeding limits
        if (angle < MinimumAngle)
            return MinimumAngle + Delta * (float)Math.Tanh(Stiffness * (angle - MinimumAngle));
        else if (angle > MaximumAngle)
            return MaximumAngle + Delta * (float)Math.Tanh(Stiffness * (angle - MaximumAngle));
        return angle;
    }

    public override string ToString()
    {
        return $"Delta={Delta}, Stiffness={Stiffness}, Min={MinimumAngle}, Max={MaximumAngle}";
    }
}

public static class CCDKinematics
{
    /// <summary>
    /// Simulates a limbs chain
    /// </summary>
    /// <param name="joint">The root</param>
    /// <param name="target">The target to try to reach</param>
    /// <param name="iterations">How fine of precision should the following calculations be</param>
    public static void SimulateLimb(CCDKinematicJoint joint, Vector2 target, int iterations)
    {
        List<CCDKinematicJoint> joints = joint.GetSubLimb();
        CCDKinematicJoint endEffector = joints[^1];

        for (int k = 0; k < iterations; k++)
        {
            for (int i = joints.Count - 1; i >= 1; i--)
            {
                // Get the angle of the previous joint to the target (aka the "hinge" of the current joint)
                float angleToTarget = joints[i - 1].Position.AngleTo(target);

                // If we are at the end effector, just rotate it to point towards the target.
                if (i == joints.Count - 1)
                    joints[i].Rotation = angleToTarget;
                else
                {
                    float angleToEndEffector = joints[i - 1].Position.AngleTo(endEffector.Position);
                    float angleDifference = angleToEndEffector.AngleBetween(angleToTarget);

                    // Rotate so that the angle towards the end effector from the joint's hinge points toward the target.
                    joints[i].Rotation += angleDifference;
                }
            }
        }
    }

    public static CCDKinematicJoint CreateLimb(params Vector2[] points)
    {
        CCDKinematicJoint firstJoint = new(points[0]);
        CCDKinematicJoint previousJoint = firstJoint;

        for (int i = 1; i < points.Length; i++)
            previousJoint = new CCDKinematicJoint(points[i], previousJoint);

        return firstJoint;
    }

    public static void DebugDraw(CCDKinematicJoint rootJoint)
    {
        // Collect all joints in the chain
        List<CCDKinematicJoint> joints = rootJoint.GetSubLimb();

        // Draw each segment as a red rectangle
        for (int i = 0; i < joints.Count - 1; i++)
        {
            Vector2 start = joints[i].Position;
            Vector2 end = joints[i + 1].Position;
            float length = Vector2.Distance(start, end);
            float angle = start.AngleTo(end);

            Main.spriteBatch.Draw(
                AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                start - Main.screenPosition,
                null,
                Color.Red.Lerp(Color.Blue, InverseLerp(0f, joints.Count, i)),
                angle,
                new Vector2(0, 0.5f), // Origin at left center of pixel
                new Vector2(length, 5f), // Scale to segment length and thickness
                SpriteEffects.None,
                0
            );
        }
    }
}

public class JointChain
{
    private List<CCDKinematicJoint> joints = [];
    public Vector2 RootPosition { get; set; }
    public int JointCount => joints.Count;
    public CCDKinematicJoint Root => joints[0];
    public CCDKinematicJoint EndEffector => joints[^1].EndEffector;

    // Optional: Optional: Store initial configuration for reference
    private List<(float length, CCDKinematicsConstraint constraints)> jointConfigs;

    public JointChain(Vector2 rootPosition, params (float length, CCDKinematicsConstraint constraints)[] jointDefinitions)
    {
        RootPosition = rootPosition;
        jointConfigs = new(jointDefinitions);

        // Initialize the chain
        CCDKinematicJoint root = new(rootPosition);
        joints.Add(root);

        foreach (var (length, constraints) in jointDefinitions)
        {
            CCDKinematicJoint joint = new(Vector2.Zero)
            {
                JointLength = length,
                Constraints = constraints
            };
            joints[^1].Extend(joint);
            joints.Add(joint);
        }
    }

    // Update the entire chain to reach a target
    public void Update(Vector2 target, int iterations = 10)
    {
        Root.Position = RootPosition;
        CCDKinematics.SimulateLimb(Root, target, iterations);
    }

    // Set joint length at index
    public void SetJointLength(int index, float length)
    {
        if (index >= 0 && index < joints.Count - 1) // Exclude root
        {
            joints[index + 1].JointLength = length;
        }
    }

    // Set constraints at index
    public void SetConstraints(int index, CCDKinematicsConstraint constraints)
    {
        if (index >= 0 && index < joints.Count - 1)
        {
            joints[index + 1].Constraints = constraints;
        }
    }

    // Update constraints dynamically based on a condition (e.g., direction)
    public void UpdateConstraints(Func<int, float, CCDKinematicsConstraint, CCDKinematicsConstraint> constraintUpdater)
    {
        for (int i = 0; i < jointConfigs.Count; i++)
        {
            var (length, constraints) = jointConfigs[i];
            joints[i + 1].Constraints = constraintUpdater(i, length, constraints);
        }
    }
}

/// <summary>
/// This is good at raymarching but it is abhorrant at finding a good tile for some reason
/// It appears that anchors must not only require a sense of pathfinding but have specific things related to whatever entity
/// What the fuck do we do
/// Seriously dont know
/// </summary>
public class Anchor
{
    // Public properties
    public float MaxLength { get; set; }
    public Vector2 DesiredPosition { get; private set; }
    public Vector2? GrabPosition { get; private set; }

    // Private fields
    private readonly Entity entity;
    private readonly Func<Vector2> getOrigin;
    private readonly Func<Vector2> getDirection;
    private readonly float velocityMultiplier;
    private readonly float minDistanceFactor;

    // Transition fields for smooth easing
    private float transitionTimer = 0f;
    private float transitionDuration = 20f; // Frames
    private Vector2? previousGrabPosition;
    private Point? lastGrabTile;

    // Constructor with configurable parameters
    public Anchor(Entity entity, float maxLength, Func<Vector2> getOrigin, Func<Vector2> getDirection,
                  float velocityMultiplier = 10f, float minDistanceFactor = 0.45f)
    {
        this.entity = entity;
        this.MaxLength = maxLength;
        this.getOrigin = getOrigin;
        this.getDirection = getDirection;
        this.velocityMultiplier = velocityMultiplier;
        this.minDistanceFactor = minDistanceFactor;
    }

    // Main update method
    public void Update()
    {
        $"I'm updating with: velMult = {velocityMultiplier}, minDist = {minDistanceFactor}, transition Timer = {transitionTimer}, transition duration {transitionDuration}, MaxLength = {MaxLength} (max * min = {MaxLength * minDistanceFactor}), to entity whoAmI {entity.whoAmI}".Log();

        Vector2 origin = getOrigin();
        Vector2 direction = getDirection().SafeNormalize(Vector2.Zero);
        DesiredPosition = CalculateDesiredPosition(origin, direction);
        $"Desired Position at={DesiredPosition} when the direction is {direction} and origin is {origin}".Log();
        Vector2? newGrabPosition = FindGrabPosition();
        $"{(newGrabPosition.HasValue == false ? "null" : newGrabPosition.Value)}".Log();

        // Handle transition to new grab position
        if (newGrabPosition.HasValue && newGrabPosition != GrabPosition)
        {
            previousGrabPosition = GrabPosition ?? origin;
            GrabPosition = newGrabPosition;
            transitionTimer = transitionDuration;
            $"Found new grab position! GrabPosition={GrabPosition}, Previous={(previousGrabPosition.HasValue == false ? "null" : previousGrabPosition.Value)}".Log();
        }

        // Apply smooth easing if transitioning
        if (transitionTimer > 0)
        {
            float t = 1f - (transitionTimer / transitionDuration);
            GrabPosition = Vector2.Lerp(previousGrabPosition.Value, GrabPosition.Value, Animators.Sine.InOutFunction(t));
            transitionTimer--;
            $"Smoothing grab position coords... {GrabPosition}".Log();
        }
        else
        {
            GrabPosition = newGrabPosition;
            $"Snapped grab postion to: {GrabPosition} because transition timer finished.".Log();
        }
    }

    // Calculate desired position based on direction and velocity
    private Vector2 CalculateDesiredPosition(Vector2 origin, Vector2 direction)
    {
        Vector2 desired = origin + direction * MaxLength * 0.9f;
        if (entity.velocity.Length() > 2f)
        {
            desired += entity.velocity * velocityMultiplier;
        }
        if (Vector2.Distance(origin, desired) > MaxLength)
        {
            desired = origin + Vector2.Normalize(desired - origin) * MaxLength;
        }
        return desired;
    }

    // Find the best grab position using raycasting and radial scans
    private Vector2? FindGrabPosition()
    {
        Vector2 origin = getOrigin();
        Vector2 direction = getDirection();
        Point? bestGuess = RaytraceToFirstSolid(origin, DesiredPosition);
        $"guessing at: {(bestGuess.HasValue == false ? "No guess!" : bestGuess.Value)} when the direction is {direction} and the origin is at {origin}".Log();

        if (bestGuess.HasValue && IsPositionFeasible(bestGuess.Value))
        {
            $"Raytrace hit at: {bestGuess.Value}".Log();
            return TileToGripPoint(bestGuess.Value, origin, direction);
        }
        else
            $"Raytrace couldn't find a valid tile.".Log();

        bestGuess = RadialDownGrabPosScan(8, 1.2f);
        if (bestGuess.HasValue && IsPositionFeasible(bestGuess.Value))
        {
            $"RadialDownGrab hit at: {bestGuess.Value}".Log();
            return TileToGripPoint(bestGuess.Value, origin, direction);
        }
        else
            $"RadialDownGrab couldn't find a valid target.".Log();

        bestGuess = RadialGrabPosScan(PiOver4, Pi * 0.95f, MaxLength * 0.8f);
        if (bestGuess.HasValue)
        {
            $"RadialGrab hit at: {bestGuess.Value}".Log();
            return TileToGripPoint(bestGuess.Value, origin, direction);
        }
        else
            $"RadialGrab couldn't find a valid target".Log();

        return null;
    }

    // Check if a position is within acceptable distance bounds
    private bool IsPositionFeasible(Point tilePos)
    {
        Vector2? worldPos = TileToGripPoint(tilePos, getOrigin(), getDirection());
        $"Feasible world pos?: {(worldPos.HasValue == false ? "null" : worldPos.Value)}. Proposed tile pos at: {tilePos}".Log();
        if (worldPos.HasValue == false)
            return false;

        Dust.QuickDust(worldPos.Value, Color.Yellow);

        float distance = Vector2.Distance(getOrigin(), worldPos.Value);
        $"Distance from world pos: {distance}".Log();

        bool result = distance >= MaxLength * minDistanceFactor && distance <= MaxLength;
        if (result)
            "Position is feasible!".Log();
        return result;
    }

    // Convert tile position to precise grip point on surface
    private static Vector2? TileToGripPoint(Point tilePosition, Vector2 rayOrigin, Vector2 rayDirection)
    {
        int tileX = tilePosition.X;
        int tileY = tilePosition.Y;
        Tile tile = Main.tile[tilePosition];

        if (!EnsureTileExist(tile))
            return null;

        Vector2 lineStart = GetSurfaceLineStart(tile, tileX, tileY);
        Vector2 lineEnd = GetSurfaceLineEnd(tile, tileX, tileY);

        Dust.QuickDust(lineStart, Color.Pink);
        Dust.QuickDust(lineEnd, Color.Brown);

        Vector2 rayEnd = rayOrigin + rayDirection * 1000f; // Extend ray far enough
        $"Calculating tile grip point line start at: {lineStart} and end at {lineEnd} after making the ray end: {rayEnd}".Log();

        if (Fixes.LinesIntersect2(rayOrigin, rayEnd, lineStart, lineEnd, out Vector2 intersection))
        {
            $"Found intersection at {intersection}!".Log();
            return intersection; // Successful intersection
        }
        else
        {
            $"No tile intersection found! Using tile center.".Log();
            return new Vector2(tileX * 16 + 8, tileY * 16 + 8); // Fallbak to the tiles center
        }
    }

    public static bool EnsureTileExist(Tile tile)
    {
        return tile != null && tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
    }

    // Get start point of tile surface line based on slope/half-brick
    private static Vector2 GetSurfaceLineStart(Tile tile, int tileX, int tileY)
    {
        float x = tileX * 16;
        float y = tileY * 16;
        if (tile.IsHalfBlock)
            return new Vector2(x, y + 8);

        return tile.Slope switch
        {
            SlopeType.Solid => new Vector2(x, y),
            SlopeType.SlopeDownLeft => new Vector2(x, y),
            SlopeType.SlopeDownRight => new Vector2(x + 16, y),
            SlopeType.SlopeUpLeft => new Vector2(x, y + 16),
            SlopeType.SlopeUpRight => new Vector2(x + 16, y + 16),
            _ => new Vector2(x, y),
        };
    }

    // Get end point of tile surface line based on slope/half-brick
    private static Vector2 GetSurfaceLineEnd(Tile tile, int tileX, int tileY)
    {
        float x = tileX * 16;
        float y = tileY * 16;
        if (tile.IsHalfBlock)
            return new Vector2(x + 16, y + 8);

        return tile.Slope switch
        {
            SlopeType.Solid => new Vector2(x + 16, y),
            SlopeType.SlopeDownLeft => new Vector2(x + 16, y + 16),
            SlopeType.SlopeDownRight => new Vector2(x, y + 16),
            SlopeType.SlopeUpLeft => new Vector2(x + 16, y),
            SlopeType.SlopeUpRight => new Vector2(x, y),
            _ => new Vector2(x + 16, y),
        };
    }

    // Raycast to find first solid tile
    private static Point? RaytraceToFirstSolid(Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).SafeNormalize(Vector2.Zero);
        float distance = Vector2.Distance(start, end);
        Vector2 current = start;

        while (Vector2.Distance(start, current) < distance)
        {
            Point tilePos = current.ToTileCoordinates();
            Tile tile = Main.tile[tilePos.X, tilePos.Y];
            if (EnsureTileExist(tile))
            {
                return tilePos;
            }
            current += direction * 8f;
        }
        return null;
    }

    // Radial scan downward with angle spread
    private Point? RadialDownGrabPosScan(int iterations, float angleSpread)
    {
        Vector2 origin = getOrigin();
        float baseAngle = PiOver2; // Downward
        List<(Point tilePos, float distance)> hits = [];

        for (int i = 0; i < iterations; i++)
        {
            float t = i / (float)(iterations - 1);
            float angle = baseAngle + MathHelper.Lerp(-angleSpread, angleSpread, t);
            Vector2 direction = angle.ToRotationVector2();
            Vector2 endPoint = origin + direction * MaxLength;
            Point? hit = RaytraceToFirstSolid(origin, endPoint);
            if (hit.HasValue && IsPositionFeasible(hit.Value))
            {
                Vector2? gripPoint = TileToGripPoint(hit.Value, origin, direction);
                if (gripPoint.HasValue)
                {
                    float distance = Math.Abs(gripPoint.Value.X - origin.X); // Horizontal distance
                    hits.Add((hit.Value, distance));
                }
            }
        }

        if (hits.Count > 0)
        {
            // Sort by horizontal distance and pick the closest
            var (tilePos, distance) = hits.OrderBy(h => h.distance).First();
            $"RadialDownGrab Hit Closest Tile at: {tilePos}, Horizontal Distance: {distance}".Log();
            return tilePos;
        }

        "RadialDownGrab: No valid tile found".Log();
        return null;
    }

    // General radial scan between two angles
    private Point? RadialGrabPosScan(float angleStart, float angleEnd, float searchRadius)
    {
        Vector2 origin = getOrigin();
        int steps = 8;
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            float angle = Lerp(angleStart, angleEnd, t);
            Vector2 direction = angle.ToRotationVector2();
            Vector2 endPoint = origin + direction * searchRadius;
            Point? hit = RaytraceToFirstSolid(origin, endPoint);
            if (hit.HasValue && IsPositionFeasible(hit.Value))
                return hit;
        }
        return null;
    }

    public void CheckDistanceAndReset()
    {
        if (GrabPosition.HasValue)
        {
            float distance = Vector2.Distance(getOrigin(), GrabPosition.Value);
            if (distance > MaxLength)
            {
                GrabPosition = null;
            }
        }
    }

    public void ForceNewGrabPosition()
    {
        if (GrabPosition.HasValue)
        {
            lastGrabTile = GrabPosition.Value.ToTileCoordinates();
            GrabPosition = null;
        }
    }
}

public static class Fixes
{
    public static bool LinesIntersect2(Vector2 rayStart, Vector2 rayEnd, Vector2 lineStart, Vector2 lineEnd, out Vector2 intersectPoint)
    {
        intersectPoint = Vector2.Zero;

        float denominator = (lineEnd.Y - lineStart.Y) * (rayEnd.X - rayStart.X) - (lineEnd.X - lineStart.X) * (rayEnd.Y - rayStart.Y);
        if (Math.Abs(denominator) < 0.0001f)
        {
            // Lines are parallel or coincident
            if (rayEnd.X == rayStart.X) // Vertical ray
            {
                float rayX = rayStart.X;
                if (lineStart.Y == lineEnd.Y) // Horizontal tile surface
                {
                    float tileY = lineStart.Y;
                    // Check if the ray's X is within the tile's X range
                    float clampedX = Clamp(rayX, Math.Min(lineStart.X, lineEnd.X), Math.Max(lineStart.X, lineEnd.X));

                    // Check if the tile's Y is between the ray's start and end Y
                    if ((tileY >= Math.Min(rayStart.Y, rayEnd.Y) && tileY <= Math.Max(rayStart.Y, rayEnd.Y)))
                    {
                        intersectPoint = new Vector2(clampedX, tileY);
                        return true;
                    }
                }
            }
            return false;
        }

        float a = (lineEnd.X - lineStart.X) * (rayStart.Y - lineStart.Y) - (lineEnd.Y - lineStart.Y) * (rayStart.X - lineStart.X);
        float b = (rayEnd.X - rayStart.X) * (rayStart.Y - lineStart.Y) - (rayEnd.Y - rayStart.Y) * (rayStart.X - lineStart.X);

        float ua = a / denominator;
        float ub = b / denominator;

        if (ua >= 0 && ub >= 0 && ub <= 1)
        {
            float x = rayStart.X + ua * (rayEnd.X - rayStart.X);
            float y = rayStart.Y + ua * (rayEnd.Y - rayStart.Y);
            intersectPoint = new Vector2(x, y);
            return true;
        }
        else if (ua >= 0) // Ray misses the segment but is in the right direction
        {
            // For a vertical ray and horizontal tile, clamp the intersection to the tile's X range
            if (rayEnd.X == rayStart.X && lineStart.Y == lineEnd.Y)
            {
                float rayX = rayStart.X;
                float tileY = lineStart.Y;
                float clampedX = Clamp(rayX, Math.Min(lineStart.X, lineEnd.X), Math.Max(lineStart.X, lineEnd.X));
                intersectPoint = new Vector2(clampedX, tileY);
                return true;
            }
        }

        return false;
    }
}