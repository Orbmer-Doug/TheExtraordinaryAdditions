using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;

namespace TheExtraordinaryAdditions.Core.DataStructures;

public class VerletSimulatedSegment(Vector2 position, bool locked = false, Vector2 velocity = new())
{
    public Vector2 Position = position;

    public Vector2 OldPosition = position;

    public Vector2 Velocity = velocity;

    public bool Locked = locked;

    /// <summary>
    /// Simulates rope that acts independently of other things
    /// </summary>
    /// <param name="segments"></param>
    /// <param name="segmentDistance"></param>
    /// <param name="loops"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    public static List<VerletSimulatedSegment> SimpleSimulation(List<VerletSimulatedSegment> segments,
        float segmentDistance, int loops = 10, float weight = 0.3f)
    {
        // Update each segment
        foreach (VerletSimulatedSegment segment in segments)
        {
            if (segment.Locked) 
                continue;
            
            Vector2 positionBeforeUpdate = segment.Position;
            segment.Position += segment.Position - segment.OldPosition;
            segment.Position += Vector2.UnitY * weight;
            segment.OldPosition = positionBeforeUpdate;
        }

        // Create the segments
        int segmentCount = segments.Count;
        for (int j = 0; j < loops; j++)
        {
            for (int i = 0; i < segmentCount - 1; i++)
            {
                VerletSimulatedSegment pointA = segments[i];
                VerletSimulatedSegment pointB = segments[i + 1];
                Vector2 segmentCenter = (pointA.Position + pointB.Position) / 2f;
                Vector2 segmentDirection = (pointA.Position - pointB.Position).SafeNormalize(Vector2.UnitY);
                if (!pointA.Locked)
                {
                    pointA.Position = segmentCenter + segmentDirection * segmentDistance / 2f;
                }

                if (!pointB.Locked)
                {
                    pointB.Position = segmentCenter - segmentDirection * segmentDistance / 2f;
                }

                segments[i] = pointA;
                segments[i + 1] = pointB;
            }
        }

        return segments;
    }

    public static float GetSegmentDistance(List<VerletSimulatedSegment> list)
    {
        Vector2 start = list[0].Position;
        Vector2 end = list.Last().Position;
        return Vector2.Distance(start, end) / list.Count;
    }
}