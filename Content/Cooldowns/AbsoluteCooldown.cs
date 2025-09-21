using CalamityMod;
using CalamityMod.Cooldowns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;

namespace TheExtraordinaryAdditions.Content.Cooldowns;

public class AbsoluteCooldown : CooldownHandler
{
    public static new string ID => "Absolute";
    public override bool ShouldDisplay => true;
    public override LocalizedText DisplayName => GetText($"Cooldowns.{ID}");
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CooldownAbsolute);
    public override Color OutlineColor => Color.DarkGray;
    public override Color CooldownStartColor => Color.White;
    public override Color CooldownEndColor => Color.White;
}
