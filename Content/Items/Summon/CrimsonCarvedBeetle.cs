using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Terraria.ID.ContentSamples.CreativeHelper;

namespace TheExtraordinaryAdditions.Content.Items.Summon;
public class CrimsonCarvedBeetle : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimsonCarvedBeetle);
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(15, 5, false));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
        ItemID.Sets.SortingPriorityBossSpawns[Type] = 10;
    }

    public override void SetDefaults()
    {
        Item.width = 50;
        Item.height = 66;
        Item.rare = ItemRarityID.LightPurple;
        Item.useAnimation = 10;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.shoot = ModContent.ProjectileType<BeetleHoldout>();
        Item.shootSpeed = 1f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.consumable = false;
    }

    public override void ModifyResearchSorting(ref ItemGroup itemGroup)
    {
        itemGroup = ItemGroup.BossItem;
    }

    public override bool CanUseItem(Player player)
    {
        if (!Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<StygainHeart>()))
            return player.ownedProjectileCounts[Item.shoot] <= 0;
        return false;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI, 0f, (player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed).ToInt());
        return false;
    }

    public override bool AltFunctionUse(Player player)
    {
        return BossDownedSaveSystem.HasDefeated<StygainHeart>();
    }

    public override void ModifyTooltips(List<TooltipLine> list)
    {
        if (BossDownedSaveSystem.HasDefeated<StygainHeart>())
            list.ModifyTooltip([new(Mod, "CrimsonCarvedBeetle", this.GetLocalizedValue("Tooltip2"))]);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BeetleHusk, 8);
        recipe.AddIngredient(ItemID.BloodMoonStarter, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}