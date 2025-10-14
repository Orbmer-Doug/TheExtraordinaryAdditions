using CalamityMod.Cooldowns;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class PumpkinDashCooldown : CooldownHandler
{
    public static new string ID => "Pumpkin";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownPumpkinDash);
    public override Color OutlineColor => new(66, 24, 0);
    public override Color CooldownStartColor => new(207, 118, 17);
    public override Color CooldownEndColor => new(252, 204, 30);
}
