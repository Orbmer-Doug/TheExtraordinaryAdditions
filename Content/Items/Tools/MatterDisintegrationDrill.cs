using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class MatterDisintegrationDrill : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MatterDisintegrationCannon);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(227, 170, 36));
    }

    public override void SetDefaults()
    {
        Item.damage = 500;
        Item.knockBack = 0f;
        Item.useTime = 1;
        Item.useAnimation = 25;
        Item.pick = 1000;
        Item.DamageType = DamageClass.Melee;
        Item.width = 90;
        Item.height = 26;
        Item.channel = Item.noUseGraphic = Item.noMelee = Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
        Item.UseSound = SoundID.Item23;
        Item.shoot = ModContent.ProjectileType<CannonHoldout>();
        Item.shootSpeed = 40f;
        Item.tileBoost = 56;
    }

    public override void HoldItem(Player player)
    {
        player.Additions().SyncMouse = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();

        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("ShadowspecBar", out ModItem ShadowspecBar) && calamityMod.TryFind("MarniteObliterator", out ModItem MarniteObliterator))
        {
            recipe.AddIngredient(ShadowspecBar, 5);
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddIngredient(MarniteObliterator, 1);
            recipe.AddTile(TileID.HeavyWorkBench);
            recipe.AddTile(TileID.MythrilAnvil);
        }
        else
        {
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddRecipeGroup("AnyCopperBar", 15);
            recipe.AddIngredient(ItemID.LunarBar, 20);
            recipe.AddTile(TileID.HeavyWorkBench);
            recipe.AddTile(TileID.MythrilAnvil);
        }
        recipe.Register();
    }
}