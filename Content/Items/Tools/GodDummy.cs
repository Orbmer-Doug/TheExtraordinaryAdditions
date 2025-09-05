using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.UI.GodDummyUI;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class GodDummy : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GodDummy);
    public const int MaxDefense = 200;
    public const int LifeAmount = 20000;
    public const int MaxLifeAmount = 200000;
    public const float MaxScale = 5.5f;
    public const float MaxSpeed = 32f;
    public static Player Owner => Main.LocalPlayer;
    public static GlobalPlayer ModdedPlayer => Owner.Additions();
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var line = new TooltipLine(Mod, "GodDummy", $"Summons a dummy that can be removed and customized by right-clicking while held\n" +
            $"If Maximum Life is at 200,000 the dummy becomes invincible\n" +
            $"A max of 50 dummies can exist at once\n" +
            $"If any other boss than it is alive it cant be struck\n" +
            $"\"Now this is testing!\"");
        var warn = new TooltipLine(Mod, "GodDummy", "Using this in a server may have unintentional consequences!")
        { OverrideColor = Color.Orange };
        tooltips.Add(line);
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            tooltips.Add(warn);
        }
    }

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

    /*
    public override bool? UseItem(Player player)
    {
        int dummy = ModContent.NPCType<GodDummyNPC>();

        bool right = PlayerInput.Triggers.JustPressed.MouseRight;
        bool left = PlayerInput.Triggers.JustPressed.MouseLeft;

        if (player.altFunctionUse == 2)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                bool trigger = PlayerInput.Triggers.JustPressed.QuickMana;
                bool isBeingHeld = player.HeldItem.ModItem is GodDummy;//player.HeldItem.type == Type;
                if (GodDummyUI.CurrentlyViewing == false)
                {
                    Main.playerInventory = true;
                    GodDummyUI.CurrentlyViewing = true;
                }

                // Delete dummy as normal
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    DeleteDummies();
                }
                // Tell server whats going on
                // why no work
                else
                {
                    var netMessage = Mod.GetPacket();
                    netMessage.Write((byte)AdditionsNetcodeMessageType.DeleteGodDummy);
                    netMessage.Send();
                }
            }
        }
        else if (player.whoAmI == Main.myPlayer)
        {
            int x = (int)Main.MouseWorld.X - 9;
            int y = (int)Main.MouseWorld.Y - 20;

            // Summon the dummy as normal
            if (Main.netMode == NetmodeID.SinglePlayer && SeekingSystem.CountNPCs(ModContent.NPCType<GodDummyNPC>()) < 50)
            {
                Vector2 pos = Main.MouseWorld;
                int n = NPC.NewNPC(new EntitySource_ItemUse(player, Item, null), x, y, dummy, 0, 0f, 0f, 0f, 0f, 255);
                NPC npc = Main.npc[n];
                if (n.WithinBounds(Main.maxNPCs))
                {
                    NPC.AdditionsInfo().ExtraAI[0] = ModdedPlayer.DummyMoving.ToDirectionInt();
                    NPC.AdditionsInfo().ExtraAI[1] = ModdedPlayer.DummyMovingSpeed;
                    NPC.AdditionsInfo().ExtraAI[2] = player.whoAmI;
                    npc.boss = ModdedPlayer.DummyCountsAsBoss;
                    npc.life = ModdedPlayer.DummyMaximumLife;
                    npc.lifeMax = ModdedPlayer.DummyMaximumLife;
                    npc.defense = ModdedPlayer.DummyDefense;
                    npc.scale = ModdedPlayer.DummyScale;
                    npc.direction = (player.Center.X < npc.Center.X).ToDirectionInt();
                }
            }
            // Let the server in on it
            else
            {
                var netMessage = Mod.GetPacket();
                netMessage.Write((byte)AdditionsNetcodeMessageType.SpawnGodDummy);
                netMessage.Write(x);
                netMessage.Write(y);
                netMessage.Send();
            }
        }
        return true;
    }
    */

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

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<GodDummyNPC>())
                        {
                            NPC npc = Main.npc[i];
                            npc.life = 0;
                            npc.HitEffect(0, 10.0, null);
                            npc.SimpleStrikeNPC(int.MaxValue, 0, false, 0f, null, false, 0f, true);
                            SoundEngine.PlaySound(SoundID.Dig, (Vector2?)npc.position, null);
                        }
                    }
                }
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (Main.npc[j].active && Main.npc[j].type == ModContent.NPCType<GodDummyNPC>())
                        {
                            SoundEngine.PlaySound(SoundID.Dig, (Vector2?)Main.npc[j].position, null);
                        }
                    }
                    ModPacket packet = Mod.GetPacket(256);
                    ((BinaryWriter)(object)packet).Write((byte)1);
                    packet.Send(-1, -1);
                }
            }
        }
        else if (NPC.CountNPCS(ModContent.NPCType<GodDummyNPC>()) < 50)
        {
            Vector2 pos = default(Vector2);
            pos = new((int)Main.MouseWorld.X - 9, (int)Main.MouseWorld.Y - 20);
            Projectile.NewProjectile(player.GetSource_ItemUse(Item, null), pos, Vector2.Zero, ModContent.ProjectileType<GodDummyProjectile>(), 0, 0f, player.whoAmI, ModContent.NPCType<GodDummyNPC>(), 0f, 0f);
        }
        return true;
    }
    public override bool AltFunctionUse(Player player)
    {
        return true;
    }
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