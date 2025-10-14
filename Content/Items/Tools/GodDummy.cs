using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.UI.GodDummyUI;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class GodDummy : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GodDummy);

    public const int MaxDefense = 200;
    public const int LifeAmount = 20000;
    public const int MaxLifeAmount = 200000;
    public const float MaxScale = 5.5f;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.width = 22;
        Item.height = 36;
        Item.useTime =
        Item.useAnimation = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTurn = true;
        Item.value = 0;
        Item.rare = ItemRarityID.Green;
        Item.autoReuse = true;
    }

    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (DummyUI.visible == false)
                {
                    Main.playerInventory = true;
                    DummyUI.visible = true;
                }
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
                DeleteDummies();
            else
            {
                ModPacket netMessage = Mod.GetPacket();
                netMessage.Write((byte)AdditionsModMessageType.DeleteGodDummy);
                netMessage.Send();
            }
        }
        else
        {
            if (NPC.CountNPCS(ModContent.NPCType<GodDummyNPC>()) < 25)
                AdditionsNetcode.SpawnGodDummy(player.Additions().mouseWorld);
        }

        return true;
    }

    public override bool AltFunctionUse(Player player) => true;

    public static void DeleteDummies()
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type == ModContent.NPCType<GodDummyNPC>())
            {
                npc.life = 0;
                npc.active = false;

                if (Main.dedServ)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.TargetDummy, 3);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}