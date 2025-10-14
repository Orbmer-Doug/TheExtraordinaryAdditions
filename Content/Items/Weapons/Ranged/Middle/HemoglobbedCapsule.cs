using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class HemoglobbedCapsule : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HemoglobbedCapsule);

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 13));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = 700;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.useAnimation = 50;
        Item.useTime = 50;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 9f;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<HemoglobbedCapsuleThrown>();
        Item.shootSpeed = 20f;
        Item.DamageType = DamageClass.Ranged;

        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override bool CanUseItem(Player player)
    {
        if (Utility.FindProjectile(out Projectile p, Item.shoot, player.whoAmI))
        {
            if (p.ai[0] == (int)HemoglobbedCapsuleThrown.BehaviorState.Aim)
                return false;
        }
        return true;
    }
}
