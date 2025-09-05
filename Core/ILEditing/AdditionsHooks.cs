using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using Terraria.Audio;

namespace TheExtraordinaryAdditions.Core.ILEditing;

public class AdditionsHooks
{
    // why clamp
    // you can make ear blasters by simply setting max instances to 0
    public class AllowLouderSoundsEdit : ILEditProvider
    {
        public override void Subscribe(ManagedILEdit edit)
        {
            // Get the set_Volume method
            MethodBase methodToModify = typeof(SoundStyle).GetMethod("set_Volume", BindingFlags.Public | BindingFlags.Instance);
            if (methodToModify == null)
            {
                edit.LogFailure("Could not find SoundStyle.set_Volume method");
                return;
            }

            // Apply the IL edit using HookHelper
            HookHelper.ModifyMethodWithIL(methodToModify, (il) => PerformEdit(il, edit));
        }

        public override void Unsubscribe(ManagedILEdit edit)
        {
            // The ILHook is automatically undone by HookHelper.UnloadHooks
        }

        public override void PerformEdit(ILContext il, ManagedILEdit edit)
        {
            ILCursor cursor = new(il);

            // Try to find the ldc.r4 1 instruction
            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.OpCode == OpCodes.Ldc_R4 && i.Operand is float f && f == 1f))
            {
                edit.LogFailure("Could not find ldc.r4 1 instruction in SoundStyle.set_Volume");
                return;
            }

            // Replace ldc.r4 1 with ldc.r4 10
            cursor.Next.Operand = 10f;
        }
    }
}