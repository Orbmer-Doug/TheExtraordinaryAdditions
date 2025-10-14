using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;

namespace TheExtraordinaryAdditions.Core.DataStructures;

public class VerletSimulatedSegment(Vector2 _position, bool _locked = false, Vector2 _velocity = new())
{
    public Vector2 position = _position;

    public Vector2 oldPosition = _position;

    public Vector2 velocity = _velocity;

    public bool locked = _locked;

    /// <summary>
    /// Simulates rope that acts independently of other things
    /// </summary>
    /// <param name="segments"></param>
    /// <param name="segmentDistance"></param>
    /// <param name="loops"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public static List<VerletSimulatedSegment> SimpleSimulation(List<VerletSimulatedSegment> segments, float segmentDistance, int loops = 10, float weight = 0.3f)
    {
        // Update each segment
        foreach (VerletSimulatedSegment segment in segments)
        {
            if (!segment.locked)
            {
                Vector2 positionBeforeUpdate = segment.position;
                segment.position += segment.position - segment.oldPosition;
                segment.position += Vector2.UnitY * weight;
                segment.oldPosition = positionBeforeUpdate;
            }
        }

        // Create the segments
        int segmentCount = segments.Count;
        for (int j = 0; j < loops; j++)
        {
            for (int i = 0; i < segmentCount - 1; i++)
            {
                VerletSimulatedSegment pointA = segments[i];
                VerletSimulatedSegment pointB = segments[i + 1];
                Vector2 segmentCenter = (pointA.position + pointB.position) / 2f;
                Vector2 segmentDirection = (pointA.position - pointB.position).SafeNormalize(Vector2.UnitY);
                if (!pointA.locked)
                {
                    pointA.position = segmentCenter + segmentDirection * segmentDistance / 2f;
                }
                if (!pointB.locked)
                {
                    pointB.position = segmentCenter - segmentDirection * segmentDistance / 2f;
                }
                segments[i] = pointA;
                segments[i + 1] = pointB;
            }
        }
        return segments;
    }

    /// <summary>
    /// Moves a segment based on a entity passing through it
    /// </summary>
    /// <param name="_segment"></param>
    /// <param name="e"></param>
    /// <param name="playedSound"></param>
    public static void MoveSegmentBasedOnEntity(List<VerletSimulatedSegment> _segment, Entity entity, SoundStyle? playedSound = null)
    {
        Vector2 entityVelocity = entity.velocity * 0.425f;
        for (int i = 1; i < _segment.Count - 1; i++)
        {
            VerletSimulatedSegment segment = _segment[i];
            VerletSimulatedSegment next = _segment[i + 1];

            // Check to see if the entity is between two verlet segments via line/box collision checks.
            // If they are, add the entity's velocity to the two segments relative to how close they are to each of the two.
            float _ = 0f;
            if (Collision.CheckAABBvLineCollision(entity.TopLeft, entity.Size, segment.position, next.position, 20f, ref _))
            {
                // Weigh the entity's distance between the two segments.
                // If they are close to one point that means the strength of the movement force applied to the opposite segment is weaker, and vice versa.
                float distanceBetweenSegments = segment.position.Distance(next.position);
                float currentMovementOffsetInterpolant = Utils.GetLerpValue(entity.Distance(segment.position), distanceBetweenSegments, distanceBetweenSegments * 0.2f, true);
                float nextMovementOffsetInterpolant = 1f - currentMovementOffsetInterpolant;

                // Move the segments based on the weight values.
                segment.position += entityVelocity * currentMovementOffsetInterpolant;
                if (!next.locked)
                    next.position += entityVelocity * nextMovementOffsetInterpolant;

                // Play some cool sounds.
                if (playedSound.HasValue && playedSound != null && entityVelocity.Length() >= 0.1f)
                {
                    SoundEngine.PlaySound(playedSound, entity.Center);
                }
            }
        }
    }

    public static float GetSegmentDistance(List<VerletSimulatedSegment> list)
    {
        Vector2 start = list[0].position;
        Vector2 end = list.Last().position;
        return Vector2.Distance(start, end) / list.Count;
    }
}