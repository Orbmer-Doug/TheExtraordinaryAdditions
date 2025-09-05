using System.ComponentModel;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Core.Config;

[BackgroundColor(13, 97, 66, 250)]
public class AdditionsConfigClient : ModConfig
{
    /// <summary>
    /// Refer to the following in ModContent.Load:
    /// <code>
    /// LoadModContent(token, mod => {
    ///     ContentInstance.Register(mod);
	///		mod.loading = true;
	///		mod.AutoloadConfig();
	///		mod.PrepareAssets();
	///		mod.Autoload();
	///		mod.Load();
	///		SystemLoader.OnModLoad(mod);
	///		mod.loading = false;
	///	});
    /// </code>
    /// I suppose we must wait on the PR to merge
    /// </summary>
    public override void OnLoaded()
    {
        AdditionsMain.SetLoadingText("Loading assets...");
        AssetRegistry.InitializeAssetDictionaries(Mod);
    }

    public static AdditionsConfigClient Instance = ModContent.GetInstance<AdditionsConfigClient>();
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Graphical")]

    [BackgroundColor(80, 250, 187, 192)]
    [SliderColor(42, 222, 162)]
    [DefaultValue(0.85f)]
    [Increment(.05f)]
    [Range(0f, 1f)]
    public float VisualIntensity { get; set; }

    [BackgroundColor(80, 250, 187, 192)]
    [SliderColor(42, 222, 162)]
    [DefaultValue(1f)]
    [Increment(.05f)]
    [Range(0f, 1f)]
    public float ScreenshakePower { get; set; }
}