using Microsoft.CodeAnalysis.Differencing;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.ILEditing;

public class AdditionsHooks
{
    // why clamp
    public class AllowLouderSounds : ModSystem
    {
        private ILHook _volumeSetterHook;

        public override void Load()
        {
            MethodBase setVolumeMethod = typeof(SoundStyle).GetMethod("set_Volume", BindingFlags.Public | BindingFlags.Instance);

            if (setVolumeMethod is null)
            {
                AdditionsMain.Instance.Logger.Error("Could not find SoundStyle.set_Volume method!");
                return;
            }

            _volumeSetterHook = new ILHook(
                setVolumeMethod,
                Edit
            );
        }

        public override void Unload()
        {
            _volumeSetterHook?.Dispose();
            _volumeSetterHook = null;
        }

        private static void Edit(ILContext il)
        {
            ILCursor cursor = new(il);

            // Try to find the ldc.r4 1 instruction
            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.OpCode == OpCodes.Ldc_R4 && i.Operand is float f && f == 1f))
            {
                AdditionsMain.Instance.Logger.Error("Could not find ldc.r4 1 instruction in SoundStyle.set_Volume");
                return;
            }

            // Replace ldc.r4 1 with ldc.r4 2
            cursor.Next.Operand = 2f;
        }
    }
}