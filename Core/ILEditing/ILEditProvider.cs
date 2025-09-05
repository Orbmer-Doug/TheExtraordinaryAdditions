using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.ILEditing;

public abstract class ILEditProvider : ModType
{
    public sealed override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            new ManagedILEdit(Name, Subscribe, Unsubscribe, PerformEdit).Apply();
        });
    }

    public sealed override void Register() => ModTypeLookup<ILEditProvider>.Register(this);

    public sealed override void SetupContent() => SetStaticDefaults();

    /// <summary>
    /// Subscribe <see cref="ManagedILEdit.SubscriptionWrapper"/> to your IL event here.
    /// </summary>
    public abstract void Subscribe(ManagedILEdit edit);

    /// <summary>
    /// Unsubscribe <see cref="ManagedILEdit.SubscriptionWrapper"/> to your IL event here.
    /// </summary>
    public abstract void Unsubscribe(ManagedILEdit edit);

    /// <summary>
    /// Perform the actual IL edit here. Use the provided ManagedILEdit's log method if something goes wrong.
    /// </summary>
    public abstract void PerformEdit(ILContext il, ManagedILEdit edit);
}
