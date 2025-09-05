using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class CosmicImplosion : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CosmicImplosion);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(ColorSwap(Color.Fuchsia, Color.Cyan, 6f));
    }

    public override void SetDefaults()
    {
        Item.damage = 915;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 164;
        Item.height = 68;
        Item.useTime = Item.useAnimation = 2;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.knockBack = 4f;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CosmicImplosionHoldout>();
        Item.shootSpeed = 24f;
        Item.rare = ItemRarityID.Purple;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.UseSound = SoundID.Item1;
    }

    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("EternalBlizzard", out ModItem EternalBlizzard) && calamityMod.TryFind("DarkPlasma", out ModItem DarkPlasma) && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity) && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence) && calamityMod.TryFind("GalacticaSingularity", out ModItem GalacticaSingularity))
        {
            recipe.AddIngredient(EternalBlizzard.Type, 1);
            recipe.AddIngredient(GalacticaSingularity, 10);
            recipe.AddIngredient(DarkPlasma, 8);
            recipe.AddTile(TileID.VoidMonolith);
        }
        else
        {
            recipe.AddIngredient(ItemID.DD2BallistraTowerT3Popper, 1);
            recipe.AddIngredient(ItemID.SoulofFlight, 120);
            recipe.AddIngredient(ItemID.LunarOre, 120);
            recipe.AddTile(TileID.VoidMonolith);
        }
        recipe.Register();
    }
}