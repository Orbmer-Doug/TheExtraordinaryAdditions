using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameInput;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Core;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Netcode;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class CoreUtils
{
    public static int EstimateLightRadius(Vector3 lightColor, LightMaskMode medium = LightMaskMode.None,
        float minIntensityThreshold = 0.0185f, int maxRadius = 15)
    {
        float maxIntensity = Math.Max(Math.Max(lightColor.X, lightColor.Y), lightColor.Z);
        if (maxIntensity <= 0)
            return 0;

        var decayRate = medium switch
        {
            LightMaskMode.Solid => 0.56f, // LightDecayThroughSolid
            LightMaskMode.Water => 0.88f * 0.91f, // Min of LightDecayThroughWater (R channel), avg random factor ~0.99
            LightMaskMode.Honey => 0.6f * 0.91f, // Min of LightDecayThroughHoney (B channel)
            _ => 0.91f, // LightDecayThroughAir
        };

        // Calculate steps for two-pass blur: intensity * decay^(2r) = threshold
        int steps = (int) Math.Ceiling(Math.Log(minIntensityThreshold / maxIntensity) / Math.Log(decayRate));
        int radius = steps / 2; // Two passes, so radius is half the total steps

        return Math.Min(Math.Max(0, radius), maxRadius);
    }

    public static float CalculateIntensityForRadius(float desiredRadius, LightMaskMode medium = LightMaskMode.None,
        float edgeIntensity = 0.5f)
    {
        float decayRate = medium switch
        {
            LightMaskMode.None => 0.91f,
            LightMaskMode.Solid => 0.56f,
            LightMaskMode.Water => 0.93f,
            LightMaskMode.Honey => 0.66f,
            _ => 0.91f
        };

        return edgeIntensity / (float) Math.Pow(decayRate, desiredRadius);
    }

    extension(int id)
    {
        public string GetTerrariaItem() => "Terraria/Images/Item_" + id;
        public string GetTerrariaProj() => "Terraria/Images/Projectile_" + id;
        public string GetTerrariaNPC() => "Terraria/Images/NPC_" + id;
    }

    extension(short id)
    {
        public string GetTerrariaItem() => "Terraria/Images/Item_" + id;
        public string GetTerrariaProj() => "Terraria/Images/Projectile_" + id;
        public string GetTerrariaNPC() => "Terraria/Images/NPC_" + id;
    }

    public static T GetEnumValue<T>(int index) where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T) values.GetValue(index - 1);
    }

    public static T GetLastEnumValue<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T) values.GetValue(values.Length - 1);
    }

    extension(SoundStyle style)
    {
        public SlotId Play(Vector2 position, float volume = 1f, float pitch = 0f,
            float pitchVariance = 0f, int maxInstances = 1, string identifier = null,
            PauseBehavior behavior = PauseBehavior.KeepPlaying)
        {
            SoundStyle sound = style;
            sound.Volume = volume;
            sound.Pitch = pitch;
            sound.PitchVariance = pitchVariance;
            sound.MaxInstances = maxInstances;
            sound.Identifier = identifier;
            sound.PauseBehavior = behavior;

            return SoundEngine.PlaySound(sound, position);
        }

        public SlotId PlayVariance(Vector2 position, float volume, float pitch,
            float pitchVariance, (float, float) pitchRange, int maxInstances = 1, string identifier = null,
            PauseBehavior behavior = PauseBehavior.KeepPlaying)
        {
            SoundStyle sound = style;
            sound.Volume = volume;
            sound.Pitch = pitch;
            sound.PitchVariance = pitchVariance;
            sound.PitchRange = pitchRange;
            sound.MaxInstances = maxInstances;
            sound.Identifier = identifier;
            sound.PauseBehavior = behavior;

            return SoundEngine.PlaySound(sound, position);
        }
    }

    public static SlotId Play(this Dictionary<SoundStyle, float> styles, Vector2 position, float volume = 1f,
        float pitch = 0f,
        float pitchVariance = 0f, (float, float)? pitchRange = null, int maxInstances = 1, string identifier = null,
        PauseBehavior behavior = PauseBehavior.KeepPlaying)
    {
        SoundStyle sound = new WeightedDict<SoundStyle>(styles).GetRandom();
        sound.Volume = volume;
        sound.Pitch = pitch;
        if (pitchRange != null)
            sound.PitchRange = (pitchRange.Value.Item1, pitchRange.Value.Item2);
        sound.PitchVariance = pitchVariance;
        sound.MaxInstances = maxInstances;
        sound.Identifier = identifier;
        sound.PauseBehavior = behavior;
        return SoundEngine.PlaySound(sound, position);
    }

    public static string ColoredText(this string text, Color color)
        => $"[c/{color.ToHexRGB()}:{text}]";

    extension(Keys key)
    {
        public bool Current()
            => !PlayerInput.WritingText && Main.hasFocus && Main.keyState.IsKeyDown(key);

        public bool JustPressed()
            => !PlayerInput.WritingText && Main.hasFocus && Main.keyState.IsKeyDown(key) &&
               !Main.oldKeyState.IsKeyDown(key);

        public bool JustReleased()
            => !PlayerInput.WritingText && Main.hasFocus && !Main.keyState.IsKeyDown(key) &&
               Main.oldKeyState.IsKeyDown(key);
    }

    public static ILCursor HijackIncomingLabels(this ILCursor cursor)
    {
        ILLabel[] array = [.. cursor.IncomingLabels];
        cursor.Emit(OpCodes.Nop);
        for (int i = 0; i < array.Length; i++)
            array[i].Target = cursor.Prev;
        return cursor;
    }

    public static void Log(this string message) => AdditionsMain.Instance?.Logger.Info(" " + message);
    public static void Warn(this string message) => AdditionsMain.Instance?.Logger.Warn(" " + message);

    public static void ServerLog(this string message)
    {
        DateTime time = DateTime.Now;
        Console.WriteLine(
            $"[TEA] [{time.Hour}.{time.Minute}.{time.Second}.{time.Millisecond}.{time.Microsecond}]: {message}");
    }

    public static NetworkText GetNetworkText(string key, params object[] substitutions) =>
        NetworkText.FromKey("Mods.TheExtraordinaryAdditions." + key, substitutions);

    public static LocalizedText GetText(string key) =>
        Language.GetOrRegister("Mods.TheExtraordinaryAdditions." + key);

    public static string GetTextValue(string key) =>
        Language.GetTextValue("Mods.TheExtraordinaryAdditions." + key);

    public static void DirectlyDisplayText(string text, Color? color = null)
    {
        Color col = color ?? Color.White;
        Main.chatMonitor.NewText(text, col.R, col.G, col.B);
    }

    public static void DisplayText(string text, Color? color = null)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.NewText(text, color ?? Color.White);
        else if (Main.dedServ)
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color ?? Color.White);
    }

    public static void StartRain()
    {
        const int ticks = 86400;
        const int rand = ticks / 24;
        Main.rainTime = Main.rand.Next(rand * 8, ticks);
        if (Main.rand.NextBool(3))
            Main.rainTime += Main.rand.Next(0, rand);
        if (Main.rand.NextBool(4))
            Main.rainTime += Main.rand.Next(0, rand * 2);
        if (Main.rand.NextBool(5))
            Main.rainTime += Main.rand.Next(0, rand * 2);
        if (Main.rand.NextBool(6))
            Main.rainTime += Main.rand.Next(0, rand * 3);
        if (Main.rand.NextBool(7))
            Main.rainTime += Main.rand.Next(0, rand * 4);
        if (Main.rand.NextBool(8))
            Main.rainTime += Main.rand.Next(0, rand * 5);

        float mult = 1f;
        if (Main.rand.NextBool(2))
            mult += 0.05f;
        if (Main.rand.NextBool(3))
            mult += 0.1f;
        if (Main.rand.NextBool(4))
            mult += 0.15f;
        if (Main.rand.NextBool(5))
            mult += 0.2f;

        Main.rainTime = (int) (Main.rainTime * mult);
        Main.raining = true;
        AdditionsNetcode.SyncWorld();
    }
    
    public static readonly BindingFlags UniversalBindingFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<Type> GetEveryTypeDerivedFrom(Type baseType, Assembly assemblyToSearch)
    {
        foreach (Type type in AssemblyManager.GetLoadableTypes(assemblyToSearch))
        {
            if (!type.IsSubclassOf(baseType) || type.IsAbstract)
                continue;

            yield return type;
        }
    }

    public static IEnumerable<Type> GetEveryTypeDerivedFrom<T>(Assembly assemblyToSearch)
    {
        foreach (Type type in AssemblyManager.GetLoadableTypes(assemblyToSearch))
        {
            if (typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                yield return type;
        }
    }

    public static Delegate ConvertToDelegate(this MethodInfo method, object instance)
    {
        List<Type> paramTypes =
        [
            .. method.GetParameters().Select(parameter => parameter.ParameterType),
            method.ReturnType
        ];

        Type delegateType = Expression.GetDelegateType([.. paramTypes]);
        return Delegate.CreateDelegate(delegateType, instance, method);
    }

    #region Color Utils

    extension(Color color)
    {
        public string ToHexRGB() =>
            BitConverter.ToString([color.R, color.G, color.B]).Replace("-", "");

        public string ToHexRGBA() =>
            BitConverter.ToString([color.R, color.G, color.B, color.A]).Replace("-", "");
    }

    public static string ColorMessage(string msg, Color color)
    {
        StringBuilder sb;
        if (!msg.Contains('\n'))
        {
            sb = new StringBuilder(msg.Length + 12);
            sb.Append("[c/").Append(color.Hex3()).Append(':')
                .Append(msg)
                .Append(']');
        }
        else
        {
            sb = new StringBuilder();
            string[] array = msg.Split('\n');
            foreach (string newlineSlice in array)
            {
                sb.Append("[c/").Append(color.Hex3()).Append(':')
                    .Append(newlineSlice)
                    .Append(']')
                    .Append('\n');
            }
        }

        return sb.ToString();
    }

    public static Color MulticolorLerp(float increment, params Color[] colors)
    {
        if (colors.Length <= 1)
            return colors[0];

        increment = MathHelper.Clamp(increment, 0f, 1f);

        float segmentLength = 1f / (colors.Length - 1);
        int segmentIndex = (int) (increment / segmentLength);

        if (segmentIndex >= colors.Length - 1)
            return colors[^1];

        // Calculate the blend factor for the current segment
        float segmentT = (increment - (segmentIndex * segmentLength)) / segmentLength;

        // Get the two colors to interpolate between
        Color start = colors[segmentIndex];
        Color end = colors[segmentIndex + 1];

        // Perform the interpolation for each color channel
        byte r = (byte) (start.R + (end.R - start.R) * segmentT);
        byte g = (byte) (start.G + (end.G - start.G) * segmentT);
        byte b = (byte) (start.B + (end.B - start.B) * segmentT);
        byte a = (byte) (start.A + (end.A - start.A) * segmentT);

        return new Color(r, g, b, a);
    }

    public static Color Lerp(this Color color, Color color2, float amount) => Color.Lerp(color, color2, amount);

    public static Color ColorSwap(Color firstColor, Color secondColor, float seconds)
    {
        float colorMe =
            (float) ((Math.Sin((double) (MathHelper.Pi * 2f / seconds) * Main.GlobalTimeWrappedHourly) + 1.0) * 0.5);
        return Color.Lerp(firstColor, secondColor, colorMe);
    }

    public delegate void ChromaAberrationDelegate(Vector2 offset, Color colorMult);

    public static void DrawChromaticAberration(Vector2 direction, float strength, ChromaAberrationDelegate drawCall)
    {
        for (int i = -1; i <= 1; i++)
        {
            Color aberrationColor = i switch
            {
                -1 => new Color(255, 0, 0, 0),
                0 => new Color(0, 255, 0, 0),
                1 => new Color(0, 0, 255, 0),
                _ => Color.White
            };
            Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * i;
            offset *= strength;
            drawCall(offset, aberrationColor);
        }
    }

    #endregion Color Utils
}
