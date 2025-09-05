using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TheExtraordinaryAdditions.Core.Config;

[BackgroundColor(13, 97, 66, 250)]
public class AdditionsConfigServer : ModConfig
{
    public static AdditionsConfigServer Instance;
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Gameplay")]

    [BackgroundColor(80, 250, 187, 192)]
    [SliderColor(42, 222, 162)]
    [DefaultValue(true)]
    [ReloadRequired]
    public bool UseCustomAI { get; set; }

    [BackgroundColor(80, 250, 187, 192)]
    [SliderColor(42, 222, 162)]
    [DefaultValue(true)]
    [ReloadRequired]
    public bool ToolOverhaul { get; set; }
}
