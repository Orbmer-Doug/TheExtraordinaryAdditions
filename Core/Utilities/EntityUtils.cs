using Terraria;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class EntityUtils
{
    extension(Entity entity)
    {
        public void SmoothFlyNear(Vector2 destination, float sharpnessInterpolant,
            float smoothnessInterpolant)
        {
            // The closer sharpness is to 1, the more closely the entity will hover exactly at the destination
            Vector2 idealVelocity =
                (destination - entity.Center) * MathHelper.Clamp(sharpnessInterpolant, 0.0001f, 1f);

            // Interpolate towards the ideal velocity
            // The closer smoothness is to 1, the more likely the entity will overshoot and have more "curvy" motion
            entity.velocity = Vector2.Lerp(entity.velocity, idealVelocity,
                MathHelper.Clamp(1f - smoothnessInterpolant, 0.0001f, 1f));
        }

        public void SmoothFlyNearWithSlowdownRadius(Vector2 destination,
            float sharpnessInterpolant, float smoothnessInterpolant, float slowdownRadius)
        {
            float distanceToSlowdownRadius = entity.Distance(destination) - slowdownRadius;
            if (distanceToSlowdownRadius < 0f)
                distanceToSlowdownRadius = 0f;

            float idealSpeed = distanceToSlowdownRadius * MathHelper.Clamp(sharpnessInterpolant, 0.0001f, 1f);
            Vector2 idealVelocity = entity.Center.SafeDirectionTo(destination) * idealSpeed;

            entity.velocity = Vector2.Lerp(entity.velocity, idealVelocity,
                MathHelper.Clamp(1f - smoothnessInterpolant, 0.0001f, 1f));
        }

        public Vector2 SafeDirectionTo(Vector2 destination, Vector2? fallback = null)
        {
            fallback ??= Vector2.Zero;
            return (destination - entity.Center).SafeNormalize(fallback.Value);
        }
    }
}
