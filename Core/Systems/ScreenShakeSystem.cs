using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Systems;

#nullable enable

// Main source and copyright belongs to Mirsario & Contributors in Terraria Overhaul
// https://github.com/Mirsario/TerrariaOverhaul/blob/dev/Common/Camera/
public struct ScreenShake
{
    public delegate float PowerDelegate(float progress);

    public const float DefaultRange = 1024f;

    public float Power = 0.5f;
    public PowerDelegate? PowerFunction;
    public float LengthInSeconds = 0.5f;
    public float Range = DefaultRange;
    public string? UniqueId;

    public ScreenShake(PowerDelegate powerFunction, float lengthInSeconds, float range = DefaultRange) : this()
    {
        PowerFunction = powerFunction;
        LengthInSeconds = lengthInSeconds;
        Range = range;
    }

    public ScreenShake(float power, float lengthInSeconds, float range = DefaultRange) : this()
    {
        Power = power;
        LengthInSeconds = lengthInSeconds;
        Range = range;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class ScreenShakeSystem : ModSystem
{
    private struct ScreenShakeInstance
    {
        public float StartTime;
        public float EndTime;
        public Vector2? Position;
        public ScreenShake Style;

        public readonly float TimeLeft => MathF.Max(0f, EndTime - TimeSystem.RenderTime);
        public readonly float Progress => Style.LengthInSeconds > 0f ? MathHelper.Clamp((TimeSystem.RenderTime - StartTime) / Style.LengthInSeconds, 0f, 1f) : 1f;
    }

    public static readonly float ScreenShakeStrength = AdditionsConfigClient.Instance.ScreenshakePower;

    private static readonly List<ScreenShakeInstance> screenShakes = [];

    private static FastNoiseLite? noise;

    public override void Load()
    {
        noise = new FastNoiseLite();

        CameraSystem.RegisterCameraModifier(1, innerAction =>
        {
            // Because this modifier runs very early (to not get smoothed out), it has to undo its previous frame's offset
            // For the camera smoothing system to also not pick it up.

            innerAction();

            if (Main.gameMenu)
                return;

            const float BaseScreenShakePower = 25f;

            Vector2 samplingPosition = Main.LocalPlayer?.Center ?? CameraSystem.ScreenCenter;
            float screenShakePower = GetPowerAtPoint(samplingPosition);
            Vector2 noiseOffset = GetNoiseValue();

            screenShakePower = Math.Min(screenShakePower, 1f);

            Vector2 screenShakeOffset = noiseOffset * BaseScreenShakePower * screenShakePower;

            Main.screenPosition += screenShakeOffset;
        });
    }

    public static Vector2 GetNoiseValue()
    {
        if (noise == null)
            return Vector2.Zero;

        static float GetValueWithSeed(int seed, float x)
        {
            noise!.SetSeed(seed);

            return noise!.GetNoise(x, 0f);
        }

        float time = TimeSystem.RenderTime;

        // Basic 2D
        const float FrequencyScale = 14.0f;

        noise.SetFrequency(FrequencyScale);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

        var result = new Vector2(
            GetValueWithSeed(420, time),
            GetValueWithSeed(1337, time)
        );

        float length = result.Length();

        result = length <= 1f ? result : result / length;

        return result;
    }

    public static float GetPowerAtPoint(Vector2 point)
    {
        if (Main.dedServ)
            return 0f;

        float power = 0f;

        foreach (ref ScreenShakeInstance instance in EnumerateScreenShakes())
        {
            ref readonly ScreenShake style = ref instance.Style;
            float progress = instance.Progress;

            float intensity;

            if (style.PowerFunction != null)
            {
                intensity = MathHelper.Clamp(style.PowerFunction(progress), 0f, 1f);
            }
            else
            {
                intensity = MathHelper.Clamp(style.Power, 0f, 1f);
                intensity *= MathF.Pow(1f - progress, 2f);
            }

            if (instance.Position.HasValue)
            {
                float distance = Vector2.Distance(instance.Position.Value, point);
                float distanceFactor = 1f - Math.Min(1f, distance / style.Range);

                intensity *= MathF.Pow(distanceFactor, 2f); // Exponential 
            }

            //power += maxPower * (1f - progress);
            power = MathF.Max(power, intensity);
        }

        return MathHelper.Clamp(power * ScreenShakeStrength, 0f, 1f);
    }

    public static void New(ScreenShake style, Vector2? position)
    {
        if (Main.dedServ)
        {
            return;
        }

        ScreenShakeInstance instance;
        instance.Style = style;
        instance.Position = position;
        instance.StartTime = TimeSystem.RenderTime;
        instance.EndTime = instance.StartTime + style.LengthInSeconds;
        string? uniqueId = style.UniqueId;

        if (uniqueId != null && screenShakes.FindIndex(i => i.Style.UniqueId == uniqueId) is (>= 0 and int index))
        {
            screenShakes[index] = instance;
            return;
        }

        screenShakes.Add(instance);
    }

    private static Span<ScreenShakeInstance> EnumerateScreenShakes()
    {
        screenShakes.RemoveAll(s => s.TimeLeft <= 0f);
        return CollectionsMarshal.AsSpan(screenShakes);
    }
}