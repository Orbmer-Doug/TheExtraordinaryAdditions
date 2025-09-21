using Microsoft.Xna.Framework.Input;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Systems;

public class AdditionsKeybinds : ModSystem
{
    public static ModKeybind TeleportHotKey { get; private set; }
    public static ModKeybind SetBonusHotKey { get; private set; }
    public static ModKeybind MiscHotKey { get; private set; }
    public static ModKeybind ShieldParry { get; private set; }
    public static ModKeybind OpenCrossDiscUI { get; private set; }

    public override void Load()
    {
        TeleportHotKey = KeybindLoader.RegisterKeybind(Mod, "Teleport", "Z");
        SetBonusHotKey = KeybindLoader.RegisterKeybind(Mod, "SetBonus", "V");
        MiscHotKey = KeybindLoader.RegisterKeybind(Mod, "Misc", "B");
        ShieldParry = KeybindLoader.RegisterKeybind(Mod, "Shield Parry", "Q");
        OpenCrossDiscUI = KeybindLoader.RegisterKeybind(Mod, "Open Cross Disc Elements", "G");
    }

    public override void Unload()
    {
        TeleportHotKey = null;
        SetBonusHotKey = null;
        MiscHotKey = null;
        ShieldParry = null;
        OpenCrossDiscUI = null;
    }
}
