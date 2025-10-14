using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Wings)]
public class GodGauntlet : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GodGauntlet);
    public override void SetStaticDefaults()
    {
        ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(400, 15f, 5f, false, -1f, 1f);

        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 125;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var line = new TooltipLine(Mod, "GodGauntlet", "\'You know what silas its not too bad of a idea\'\n" +
            "Counts as wings\n" +
            "Hiding this accessory causes you to be immortal, give infinite flight time, and have 30+ minion slots\n" +
            "Press the throw button to remove every entity in the world...\n" +
            "Press quick mana to get the coords of your mouse!\n" +
            "-Developer Tool-".ColoredText(Color.Pink))
        {
            OverrideColor = Color.Lerp(Color.Ivory, Color.AntiqueWhite, .5f)
        };
        tooltips.Add(line);
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
        Item.accessory = true;
        Item.defense = 0;
        Item.rare = -1;
        Item.value = 1;
        Item.maxStack = 1;
    }

    public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
    {
        ascentWhenFalling = 1f;
        ascentWhenRising = 0.175f;
        maxCanAscendMultiplier = 1.25f;
        maxAscentMultiplier = 3.9f;
        constantAscend = 0.15f;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (PlayerInput.Triggers.Current.Throw)
        {
            foreach (NPC n in Main.npc)
                n.active = false;
            foreach (Projectile p in Main.projectile)
                p.active = false;
            foreach (Item item in Main.item)
                item.active = false;
            foreach (Dust dust in Main.dust)
                dust.active = false;
            foreach (Gore gore in Main.gore)
                gore.active = false;

            player.itemTime = player.itemAnimation = 0;
        }

        if (PlayerInput.Triggers.Current.QuickMana && Main.GameUpdateCount % 20f == 19f)
        {
            DisplayText(Main.MouseWorld.ToString(), Color.Azure);
        }

        if (hideVisual)
        {
            player.manaCost *= 0f;
            player.wingTimeMax = 200;
            player.wingTime = player.wingTimeMax / 2;

            player.GiveIFrames(10);
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;

            player.maxMinions += 30;
            player.maxTurrets += 30;
        }
        else
        {
            player.wingTimeMax = 200;
        }

        player.accRunSpeed = 12f;
        player.moveSpeed += 0.7f;
        player.iceSkate = true;
        player.waterWalk = true;
        player.fireWalk = true;
        player.lavaImmune = true;
        player.buffImmune[BuffID.OnFire] = true;
        player.noFallDmg = true;
    }
}