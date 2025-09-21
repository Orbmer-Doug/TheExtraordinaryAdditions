using CalamityMod.Cooldowns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class MyceliumiteCooldown : CooldownHandler
{
    public static new string ID => "Fungus";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownMycelium);
    public override Color OutlineColor => new(22, 22, 22);
    public override Color CooldownStartColor => new(30, 100, 172);
    public override Color CooldownEndColor => new(95, 110, 255);
}
