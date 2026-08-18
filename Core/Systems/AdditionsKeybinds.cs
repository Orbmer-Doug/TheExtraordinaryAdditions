using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Systems;

public class AdditionsKeybinds : ModSystem
{
    public static ModKeybind SetBonusHotKey { get; private set; }
    public static ModKeybind ShieldParry { get; private set; }
    public static ModKeybind OpenCrossDiscUI { get; private set; }

    public override void Load()
    {
        SetBonusHotKey = KeybindLoader.RegisterKeybind(Mod, "SetBonus", "V");
        ShieldParry = KeybindLoader.RegisterKeybind(Mod, "Shield Parry", "Q");
        OpenCrossDiscUI = KeybindLoader.RegisterKeybind(Mod, "Open Cross Disc Elements", "G");
    }

    public override void Unload()
    {
        SetBonusHotKey = null;
        ShieldParry = null;
        OpenCrossDiscUI = null;
    }
}
