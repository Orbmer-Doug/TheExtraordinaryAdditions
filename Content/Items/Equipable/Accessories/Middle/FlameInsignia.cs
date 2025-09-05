using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

public class FlameInsignia : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlameInsignia);
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 46;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.accessory = true;
        Item.rare = ItemRarityID.LightPurple;
    }
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.OrangeRed.ToVector3() * .7f);
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.Additions().flameInsignia = true;
        Lighting.AddLight(player.Center, Color.OrangeRed.ToVector3() * .7f);
        player.GetCritChance(DamageClass.Generic) += 10f;
    }
}
