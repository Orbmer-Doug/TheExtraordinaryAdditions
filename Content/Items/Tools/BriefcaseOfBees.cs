using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class BriefcaseOfBees : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BriefcaseOfBees);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.scale = 1;
        Item.DamageType = DamageClass.Generic;
        Item.width = 10;
        Item.height = 36;
        Item.useTime = Item.useAnimation = 120;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = false;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.shootSpeed = 1f;
        Item.crit = 0;
    }

    public override bool? UseItem(Player player)
    {
        for (int i = 0; i < 50; i++)
        {
            int THEBEESHAVEMANIFESTED = NPC.NewNPC(Item.GetSource_FromAI(), (int)player.Center.X, (int)player.Center.Y, NPCID.Bee);
            Main.npc[THEBEESHAVEMANIFESTED].velocity = Main.rand.NextVector2CircularEdge(18f, 18f) * Main.rand.NextFloat(0f, 1f);
            Main.npc[THEBEESHAVEMANIFESTED].npcSlots = .1f;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, THEBEESHAVEMANIFESTED);
        }
        return true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BeeHive, 100);
        recipe.AddIngredient(ItemID.Leather, 15);
        recipe.AddTile(TileID.Hive);
        recipe.Register();
    }
}