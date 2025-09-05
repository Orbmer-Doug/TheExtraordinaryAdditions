using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class RarityGlobalItem : GlobalItem
{
    public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
    {
        // If the item is of the rarity, and the line is the item name.
        if (line.Mod == "Terraria" && line.Name == "ItemName")
        {
            if (item.rare == ModContent.RarityType<LegendaryRarity>())
            {
                LegendaryRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<UniqueRarity>())
            {
                UniqueRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<LaserClassRarity>())
            {
                LaserClassRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<CrosscodeRarity>())
            {
                CrosscodeRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<BloodWroughtRarity>())
            {
                BloodWroughtRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<PrimordialRarity>())
            {
                PrimordialRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<ShadowRarity>())
            {
                ShadowRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<BrackishRarity>())
            {
                BrackishRarity.DrawCustomTooltipLine(line);
                return false;
            }
            else if (item.rare == ModContent.RarityType<CyberneticRarity>())
            {
                CyberneticRarity.DrawCustomTooltipLine(line);
                return false;
            }
        }
        return true;
    }
}
