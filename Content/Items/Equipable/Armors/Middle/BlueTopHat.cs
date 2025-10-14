using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Head)]
public class BlueTopHat : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BlueTopHat);

    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = true; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Lime;
        Item.defense = 20;
    }

    public override void UpdateArmorSet(Player player)
    {
        player.setBonus = this.GetLocalization("SetBonus").Format(AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString());
        player.moveSpeed += 0.25f;
        player.statLifeMax2 += 40;

        if (AdditionsKeybinds.SetBonusHotKey.JustPressed && !CalUtils.HasCooldown(player, MyceliumiteCooldown.ID) && player.whoAmI == Main.myPlayer)
        {
            bool onGround = player.CheckSolidGround();
            int amt = onGround ? 30 : 10;
            for (int i = 0; i < amt; i++)
            {
                int num = 0;
                if (player.gravDir == -1f)
                {
                    num -= player.height;
                }
                float away = onGround ? 150f : 4f;
                Vector2 vel = -Vector2.UnitY.RotatedByRandom(.33f) * Main.rand.NextFloat(9f, 16f);
                Vector2 position = new Vector2(player.position.X - away, player.position.Y + player.height + num) + Vector2.UnitY.RotatedBy(player.fullRotation);
                Rectangle pos = new((int)position.X, (int)position.Y, 300, 4);
                Vector2 randPos = Main.rand.NextVector2FromRectangle(pos);

                Vector2 shroomPos = onGround ? randPos : position;
                player.NewPlayerProj(shroomPos, vel, ModContent.ProjectileType<Myceliumite>(), 100, 1f, player.whoAmI);

                for (int j = 0; j < 5; j++)
                {
                    if (j % 2f == 1f)
                    {
                        ParticleRegistry.SpawnMistParticle(shroomPos, RandomVelocity(2f, 3f, 4f), Main.rand.NextFloat(.5f, .8f), new(95, 110, 225), new(31, 32, 110), Main.rand.NextByte(120, 200), .05f);
                    }

                    Dust.NewDustPerfect(shroomPos, DustID.GlowingMushroom, vel.RotatedByRandom(.1f) * Main.rand.NextFloat(.2f, .4f), 0, default, Main.rand.NextFloat(.8f, 1.4f));
                }
            }

            SoundEngine.PlaySound(SoundID.Item167 with { Pitch = -.2f, Volume = 1.4f }, player.Center);
            CalUtils.AddCooldown(player, MyceliumiteCooldown.ID, SecondsToFrames(15));
        }
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<BlueTuxedo>())
        {
            return legs.type == ModContent.ItemType<BlueLeggings>();
        }
        return false;
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Melee) += 0.25f;
        player.buffImmune[BuffID.Chilled] = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShroomiteBar, 13);
        recipe.AddIngredient(ItemID.TopHat, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}