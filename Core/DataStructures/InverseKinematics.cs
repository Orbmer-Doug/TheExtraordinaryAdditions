using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using TheExtraordinaryAdditions.Core.Utilities;
namespace TheExtraordinaryAdditions.Core.DataStructures;

// A slightly modified version of the IK present in Calamity Fables
// love those guys

// TODO: Offsets to joint position without blowing up?
public class CCDKinematicJoint
{
    public CCDKinematicJoint Parent { get; set; }
    public CCDKinematicJoint Child { get; set; }

    public CCDKinematicsConstraint? Constraint = null;

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

            // If unconstrained, just directly set the rotation
            if (Constraint == null)
                _rotation = value - Parent.Rotation;

            // Else constrain the angles
            else
            {
                float desiredRelative = MathHelper.WrapAngle(value - Parent.Rotation);
                _rotation = Constraint.Value.Apply(desiredRelative);
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
    public void Append(Vector2 newJointPosition, CCDKinematicsConstraint? constraints = null)
    {
        CCDKinematicJoint newJoint = new(newJointPosition, this)
        {
            Constraint = constraints
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
    public void Extend(Vector2 newJointPosition, CCDKinematicsConstraint? constraints = null)
    {
        if (Child != null)
            Child.Extend(newJointPosition, constraints);
        else
            Append(newJointPosition, constraints);
    }

    /// <summary>
    /// Adds a newly created joint to the final segment from the segment chain this joint is part of, with the specified offset from the end of the chain
    /// </summary>
    public void ExtendInDirection(Vector2 newJointOffset, CCDKinematicsConstraint? constraints = null)
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
public readonly struct CCDKinematicsConstraint
{
    /// <summary>
    /// Minimum angle the limb may go
    /// </summary>
    public readonly float MinimumAngle;

    /// <summary>
    /// Maximum angle the limb may go
    /// </summary>
    public readonly float MaximumAngle;

    /// <summary>
    /// Maximum overshoot (in radians) this joint can go
    /// </summary>
    public readonly float Delta;

    /// <summary>
    /// How sharp the limb moves beyond its limits
    /// </summary>
    public readonly float Stiffness;
    public CCDKinematicsConstraint(float minimumAngle, float maximumAngle, float delta = 0.1f, float stiffness = 2f, bool flip = false)
    {
        MinimumAngle = flip ? -maximumAngle : minimumAngle;
        MaximumAngle = flip ? -minimumAngle : maximumAngle;
        Delta = delta;
        Stiffness = stiffness;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Apply(float angle)
    {
        if (angle < MinimumAngle)
            return MinimumAngle + Delta * (float)Math.Tanh(Stiffness * (angle - MinimumAngle));
        else if (angle > MaximumAngle)
            return MaximumAngle + Delta * (float)Math.Tanh(Stiffness * (angle - MaximumAngle));
        return angle;

        /* stiff
        angle = MathHelper.WrapAngle(angle);
        return MathHelper.Clamp(angle, MinimumAngle, MaximumAngle);
        */
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
        if (joints.Count < 2)
            return; // Nothing to simulate

        CCDKinematicJoint endEffector = joints[^1];

        // Handle unreachable targets by aligning straight
        float maxLength = joint.GetLimblength();
        if (target.Distance(joints[0].Position) > maxLength)
        {
            float straightAngle = joints[0].Position.AngleTo(target);
            for (int i = 1; i < joints.Count; i++)
                joints[i].Rotation = straightAngle;
            return;
        }

        for (int k = 0; k < iterations; k++)
        {
            // Backward pass
            for (int i = joints.Count - 1; i >= 1; i--)
            {
                Vector2 hingePos = joints[i - 1].Position;
                float angleToTarget = hingePos.AngleTo(target);

                if (i == joints.Count - 1)
                    joints[i].Rotation = angleToTarget;
                else
                {
                    float angleToEE = hingePos.AngleTo(endEffector.Position);
                    float angleDiff = MathHelper.WrapAngle(angleToTarget - angleToEE); // Use wrapped diff for signed accuracy
                    joints[i].Rotation += angleDiff;
                }
            }

            // Forward pass (root to end)
            for (int i = 1; i < joints.Count; i++)
            {
                Vector2 hingePos = joints[i - 1].Position;
                float angleToTarget = hingePos.AngleTo(target);
                float angleToEE = hingePos.AngleTo(endEffector.Position);
                float angleDiff = MathHelper.WrapAngle(angleToTarget - angleToEE); // Signed wrapped diff
                joints[i].Rotation += angleDiff;
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
        List<CCDKinematicJoint> joints = rootJoint.GetSubLimb();
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
                new Vector2(0, 0.5f),
                new Vector2(length, 5f),
                SpriteEffects.None,
                0
            );
        }
    }
}

public class JointChain
{
    private readonly List<CCDKinematicJoint> joints = new();
    public Vector2 RootPosition { get; set; }
    public int JointCount => joints.Count;
    public CCDKinematicJoint Root => joints[0];
    public CCDKinematicJoint EndEffector => joints[^1].EndEffector;

    public JointChain(Vector2 rootPosition, params (float length, CCDKinematicsConstraint? constraints)[] jointDefinitions)
    {
        RootPosition = rootPosition;
        CCDKinematicJoint root = new(rootPosition);
        joints.Add(root);
        foreach (var (length, constraints) in jointDefinitions)
        {
            CCDKinematicJoint joint = new(Vector2.Zero) { JointLength = length, Constraint = constraints };
            joints[^1].Extend(joint);
            joints.Add(joint);
        }
    }

    public void Update(Vector2 target, int iterations = 10)
    {
        Root.Position = RootPosition;
        CCDKinematics.SimulateLimb(Root, target, iterations);
    }

    public void SetJointLength(int index, float length)
    {
        if (index >= 0 && index < joints.Count - 1)
            joints[index + 1].JointLength = length;
    }

    public void SetConstraint(int index, CCDKinematicsConstraint? constraints)
    {
        if (index >= 0 && index < joints.Count - 1)
            joints[index + 1].Constraint = constraints;
    }
}