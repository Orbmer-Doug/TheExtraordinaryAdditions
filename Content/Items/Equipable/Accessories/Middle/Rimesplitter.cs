using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

public class Rimesplitter : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Rimesplitter);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.IntegrateHotkey(AdditionsKeybinds.ShieldParry, "[KEY]");
        tooltips.ColorLocalization(new Color(52, 183, 235));
    }

    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 48;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.defense = 7;
        Item.accessory = true;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.Additions().Auroric = true;
        player.noKnockback = true;
        player.buffImmune[BuffID.Chilled & BuffID.Frostburn2 & BuffID.Frostburn & BuffID.Frozen] = true;

        if (AdditionsKeybinds.ShieldParry.JustPressed && AdditionsKeybinds.ShieldParry != null)
        {
            int type = ModContent.ProjectileType<AuroricShield>();
            if (!Main.projectile.Any((n) => n.active && n.owner == player.whoAmI && n.type == type) && player.ownedProjectileCounts[type] <= 0 && !player.HasBuff(ModContent.BuffType<AuroricCooldown>()))
            {
                int dmg = (int)player.GetTotalDamage(DamageClass.Generic).ApplyTo(500f);
                Projectile p = Main.projectile[Projectile.NewProjectile(player.GetSource_ItemUse(Item, null), player.Center, Vector2.One, type, dmg, 10f, player.whoAmI, 0f, 0f, 0f)];
                player.AddBuff(ModContent.BuffType<AuroricCooldown>(), SecondsToFrames(5));
            }
        }
    }
}
