using CalamityMod.Cooldowns;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class CyberneticParryCooldown : CooldownHandler
{
    public static new string ID => "CyberParry";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownCyberneticParry);
    public override Color OutlineColor => Color.White;
    public override Color CooldownStartColor => Color.DarkCyan;
    public override Color CooldownEndColor => Color.SkyBlue;
}
