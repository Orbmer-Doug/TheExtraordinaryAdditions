using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class CometStorm : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CometStorm);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }
    public override void SetDefaults()
    {
        Item.damage = 260;
        Item.DamageType = DamageClass.Magic;
        Item.width = 116;
        Item.height = 184;
        Item.useTime =
        Item.useAnimation = 8;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CometStormHoldout>();
        Item.crit = 0;
        Item.mana = 3;
        Item.shootSpeed = 11f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.None;
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("RuinousSoul", out ModItem RuinousSoul) && calamityMod.TryFind("Lumenyl", out ModItem Lumenyl) && calamityMod.TryFind("DarkPlasma", out ModItem DarkPlasma))
        {
            recipe.AddIngredient(ItemID.MeteorStaff, 1);
            recipe.AddIngredient(Lumenyl.Type, 10);
            recipe.AddIngredient(RuinousSoul.Type, 5);
            recipe.AddIngredient(DarkPlasma.Type, 6);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        else
        {
            recipe.AddIngredient(ItemID.MeteorStaff, 1);
            recipe.AddIngredient(ItemID.LunarBar, 15);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}