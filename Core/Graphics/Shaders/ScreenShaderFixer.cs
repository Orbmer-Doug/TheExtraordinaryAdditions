using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using TheExtraordinaryAdditions.Core.ILEditing;

namespace TheExtraordinaryAdditions.Core.Graphics.Shaders;

/// <summary>
/// In short, terraria really only knows best of basic screen tint shaders so this fixes it to work across all lightings
/// </summary>
public sealed class ScreenShaderFixer : ILEditProvider
{
    public override void PerformEdit(ILContext il, ManagedILEdit edit)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Lighting>("get_NotRetro")))
        {
            edit.LogFailure("The Lighting.NotRetro property could not be found.");
            return;
        }

        // Emit OR 1 on the "can screen shaders be drawn" bool to make it always true, regardless of lighting mode.
        cursor.Emit(OpCodes.Ldc_I4_1);
        cursor.Emit(OpCodes.Or);
    }

    public override void Subscribe(ManagedILEdit edit) => IL_Main.DoDraw += edit.SubscriptionWrapper;

    public override void Unsubscribe(ManagedILEdit edit) => IL_Main.DoDraw -= edit.SubscriptionWrapper;
}